using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Lokad.Cloud.AppHost.Framework;

namespace MySampleProject
{
	public class Class1 : IApplicationEntryPoint
    {
		public void Run(XElement settings, IDeploymentReader deploymentReader, IApplicationEnvironment environment,
			CancellationToken cancellationToken)
		{
			while (true)
			{
				if (cancellationToken.IsCancellationRequested)
					return;
				Console.WriteLine("I am running and and getting sleepy");
				Thread.Sleep(1000);
				Console.WriteLine("Oh, must be a second wind");

			}
		}

		public void OnSettingsChanged(XElement settings)
		{
			throw new NotImplementedException();
		}
    }
}
