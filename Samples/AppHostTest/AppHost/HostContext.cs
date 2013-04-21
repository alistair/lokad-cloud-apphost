using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.AppHost.Framework.Instrumentation;

namespace Source
{
    public class HostContext : IHostContext
    {
        public HostContext(IHostObserver hostObserver, IDeploymentReader deploymentReader)
        {
            Observer = hostObserver;
            DeploymentReader = deploymentReader;

            Identity = new HostLifeIdentity("test", Guid.NewGuid().ToString());
        }

        public HostLifeIdentity Identity { get; private set; }
        public CellLifeIdentity GetNewCellLifeIdentity(string solutionName, string cellName, SolutionHead deployment)
        {
            return new CellLifeIdentity(Identity, solutionName, cellName, string.Concat(solutionName, "_", cellName));
        }

        public string GetSettingValue(CellLifeIdentity cell, string settingName)
        {
            throw new NotImplementedException();
        }

        public X509Certificate2 GetCertificate(CellLifeIdentity cell, string thumbprint)
        {
            throw new NotImplementedException();
        }

        public string GetLocalResourcePath(CellLifeIdentity cell, string resourceName)
        {
            return Path.Combine(Path.GetTempPath(), cell.Host.WorkerName, cell.SolutionName, cell.UniqueCellInstanceName);
        }

        public IPEndPoint GetEndpoint(CellLifeIdentity cell, string endpointName)
        {
            throw new NotImplementedException();
        }

        public int CurrentWorkerInstanceCount
        {
            get { return 1; }
        }

        public IDeploymentReader DeploymentReader { get; private set; }

        /// <remarks>Can be <c>null</c>.</remarks>
        public IHostObserver Observer { get; private set; }

        public string GetSettingValue(string settingName)
        {
            throw new NotImplementedException();
        }

        public X509Certificate2 GetCertificate(string thumbprint)
        {
            throw new NotImplementedException();
        }

        public void ProvisionWorkerInstances(int numberOfInstances)
        {
            throw new NotImplementedException();
        }

        public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
        {
            throw new NotImplementedException();
        }
    }
}
