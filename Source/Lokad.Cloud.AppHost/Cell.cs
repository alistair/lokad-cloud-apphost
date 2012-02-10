#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Threading;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.AppHost.Framework.Instrumentation.Events;
using Lokad.Cloud.AppHost.Util;

namespace Lokad.Cloud.AppHost
{
    internal sealed class Cell
    {
        private static readonly TimeSpan FloodFrequencyThreshold = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan DelayWhenFlooding = TimeSpan.FromMinutes(5);

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly string _solutionName;
        private readonly string _cellName;

        private readonly IHostContext _hostContext;
        private readonly Action<IHostCommand> _sendCommand;

        private volatile Thread _thread;
        private volatile CellAppDomainEntryPoint _entryPoint;
        private volatile CellDefinition _cellDefinition;
        private volatile SolutionHead _deployment;

        private Cell(IHostContext hostContext, Action<IHostCommand> sendCommand, CellDefinition cellDefinition, SolutionHead deployment, string solutionName, CancellationToken cancellationToken)
        {
            _hostContext = hostContext;
            _sendCommand = sendCommand;
            _solutionName = solutionName;
            _cellName = cellDefinition.CellName;
            _cellDefinition = cellDefinition;
            _deployment = deployment;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public static Cell Run(
            IHostContext hostContext,
            Action<IHostCommand> sendCommand,
            CellDefinition cellDefinition,
            SolutionHead deployment,
            string solutionName,
            CancellationToken cancellationToken)
        {
            var process = new Cell(hostContext, sendCommand, cellDefinition, deployment, solutionName, cancellationToken);
            process.Run();
            return process;
        }

        /// <summary>
        /// Shutdown just this cell. Use the Task property to wait for the shutdown to complete if needed.
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Ensure this cell is either still alive, or cancelled.
        /// </summary>
        public void EnsureIsRunningUnlessCancelled()
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                return;
            }

            var thread = _thread;
            if (thread == null || !thread.IsAlive)
            {
                _hostContext.Observer.TryNotify(() => new CellDeadRestartedEvent(_hostContext.Identity, _cellName, _solutionName));
                Run();
            }
        }

        void Run()
        {
            var cancellationToken = _cancellationTokenSource.Token;
            _thread = new Thread(() =>
                {
                    var currentRoundStartTime = DateTimeOffset.UtcNow - FloodFrequencyThreshold;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var observer = _hostContext.Observer;
                        var lastRoundStartTime = currentRoundStartTime;
                        currentRoundStartTime = DateTimeOffset.UtcNow;

                        var identity = _hostContext.GetNewCellLifeIdentity(_solutionName, _cellName, _deployment);

                        AppDomain domain = AppDomain.CreateDomain("LokadCloudServiceRuntimeCell_" + identity.UniqueCellInstanceName, null, AppDomain.CurrentDomain.SetupInformation);
                        try
                        {
                            try
                            {
                                _entryPoint = (CellAppDomainEntryPoint)domain.CreateInstanceAndUnwrap(
                                    Assembly.GetExecutingAssembly().FullName,
                                    typeof(CellAppDomainEntryPoint).FullName);
                            }
                            catch (Exception exception)
                            {
                                // Fatal Error
                                observer.TryNotify(() => new CellFatalErrorRestartedEvent(identity, exception));
                                cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                                continue;
                            }

                            // Forward cancellation token to AppDomain-internal cancellation token source
                            var registration = cancellationToken.Register(_entryPoint.Cancel);
                            var environment = new ApplicationEnvironment(_hostContext, identity, _deployment, _cellDefinition.Assemblies, _sendCommand);
                            try
                            {
                                observer.TryNotify(() => new CellStartedEvent(identity));
                                _entryPoint.Run(_cellDefinition, _hostContext.DeploymentReader, environment);
                            }
                            catch (ThreadAbortException exception)
                            {
                                Thread.ResetAbort();
                                observer.TryNotify(() => new CellAbortedEvent(identity, exception));
                            }
                            catch (Exception exception)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    observer.TryNotify(() => new CellAbortedEvent(identity, exception));
                                }
                                else
                                {
                                    _entryPoint = null;
                                    if ((DateTimeOffset.UtcNow - lastRoundStartTime) < FloodFrequencyThreshold)
                                    {
                                        observer.TryNotify(() => new CellExceptionRestartedEvent(identity, exception, true));
                                        cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                                    }
                                    else
                                    {
                                        observer.TryNotify(() => new CellExceptionRestartedEvent(identity, exception, false));
                                    }
                                }
                            }
                            finally
                            {
                                RemotingServices.Disconnect(environment);
                                _entryPoint = null;
                                observer.TryNotify(() => new CellStoppedEvent(identity));
                                registration.Dispose();
                            }
                        }
                        catch (Exception exception)
                        {
                            // Fatal Error
                            observer.TryNotify(() => new CellFatalErrorRestartedEvent(identity, exception));
                            cancellationToken.WaitHandle.WaitOne(DelayWhenFlooding);
                            continue;
                        }
                        finally
                        {
                            AppDomain.Unload(domain);
                        }
                    }
                });

            _thread.Name = "Lokad.Cloud AppHost Cell (" + _cellName + ")";
            _thread.Start();
        }

        public void OnCellDefinitionChanged(CellDefinition newCellDefinition, SolutionHead newDeployment)
        {
            var oldCellDefinition = _cellDefinition;
            var newAssemblies = newCellDefinition.Assemblies;
            var newEntryPointTypeName = newCellDefinition.EntryPointTypeName;

            _cellDefinition = newCellDefinition;
            _deployment = newDeployment;

            var entryPoint = _entryPoint;
            if (entryPoint == null)
            {
                return;
            }

            if (!oldCellDefinition.Assemblies.Equals(newAssemblies)
                || !StringComparer.Ordinal.Equals(oldCellDefinition.EntryPointTypeName, newEntryPointTypeName))
            {
                // cancel will stop the cell and unload the AppDomain, but then automatically
                // start again with the new assemblies and entry point
                entryPoint.Cancel();
                return;
            }

            entryPoint.AppplyChangedSettings(newCellDefinition.SettingsXml);
        }
    }
}
