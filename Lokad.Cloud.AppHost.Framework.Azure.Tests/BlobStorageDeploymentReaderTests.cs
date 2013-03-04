using Lokad.Cloud.AppHost.Framework.Definition;
using NUnit.Framework;

namespace Lokad.Cloud.AppHost.Framework.Azure.Tests
{
	public class BlobStorageDeploymentReaderTests
	{
		private const string CONNECTION_STRING = "UseDevelopmentStorage=true;";

 		[Test]
 		public void BlahTest()
 		{
 			var reader = new BlobStorageDeploymentReader(CONNECTION_STRING);


 			string newEtag = string.Empty;
 			var solutionHead = reader.GetDeploymentIfModified(newEtag, out newEtag);

 			var solutionDefinition = reader.GetSolution(solutionHead);

 			reader.GetAssembliesAndSymbols(solutionDefinition.Cells[0].Assemblies);
 		}
	}
}