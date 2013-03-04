using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Lokad.Cloud.AppHost.Framework.Definition;
using Lokad.Cloud.AppHost.Framework.Instrumentation;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.AppHost.Framework.Azure
{
	public class RoleEnvironmentHostContext : IHostContext
	{
		public RoleEnvironmentHostContext(IDeploymentReader deploymentReader, IHostObserver observer)
		{
			DeploymentReader = deploymentReader;
			Observer = observer;
			Identity = new HostLifeIdentity(RoleEnvironment.CurrentRoleInstance.Role.Name, RoleEnvironment.CurrentRoleInstance.Id);
		}

		public HostLifeIdentity Identity { get; private set; }
		public CellLifeIdentity GetNewCellLifeIdentity(string solutionName, string cellName, SolutionHead deployment)
		{
			// TODO: Replace GUID with global blob counter
			return new CellLifeIdentity(Identity, solutionName, cellName, Guid.NewGuid().ToString("N"));
		}

		public string GetSettingValue(CellLifeIdentity cell, string settingName)
		{
			//Don't use roleEnvironment.GetConfigurationSettingValue here currently.
			//RoleEnvironment.GetConfigurationSettingValue(settingName)
			throw new NotImplementedException();
		}

		//TODO Cache
		public X509Certificate2 GetCertificate(CellLifeIdentity cell, string thumbprint)
		{
			var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			try
			{
				store.Open(OpenFlags.ReadOnly);
				var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
				if (certs.Count != 1)
				{
					return null;
				}

				return certs[0];
			}
			finally
			{
				store.Close();
			}
		}

		public string GetLocalResourcePath(CellLifeIdentity cell, string resourceName)
		{
			return RoleEnvironment.GetLocalResource(resourceName).RootPath;
		}

		public IPEndPoint GetEndpoint(CellLifeIdentity cell, string endpointName)
		{
			return RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[endpointName].IPEndpoint;
		}

		public int CurrentWorkerInstanceCount { get; private set; }
		public void ProvisionWorkerInstances(int numberOfInstances)
		{
			throw new System.NotImplementedException();
		}

		public void ProvisionWorkerInstancesAtLeast(int minNumberOfInstances)
		{
			throw new System.NotImplementedException();
		}

		public IDeploymentReader DeploymentReader { get; private set; }
		public IHostObserver Observer { get; private set; }
	}
}