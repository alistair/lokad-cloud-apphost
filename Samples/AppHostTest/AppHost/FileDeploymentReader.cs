using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Ionic.Zip;
using Lokad.Cloud.AppHost.Framework;
using Lokad.Cloud.AppHost.Framework.Definition;

namespace Source
{
    [Serializable]
    public class FileDeploymentReader : IDeploymentReader
    {
        readonly string _basePath;
        static readonly SHA1 Hash = SHA1.Create();
        readonly string _solutionHeadFileName;

        public FileDeploymentReader(string basePath, string solutionHeadFileName)
        {
            _basePath = basePath;
            _solutionHeadFileName = solutionHeadFileName;
        }

        public SolutionHead GetDeploymentIfModified(string knownETag, out string newETag)
        {
            var path = Path.Combine(_basePath, _solutionHeadFileName);

            if (!File.Exists(path))
            {
                newETag = null;
                return null;
            }

            using (var reader = new StreamReader(path))
            {
                
                Encoding encoding = reader.CurrentEncoding;
                string content = reader.ReadToEnd();

                //Reset to begining on stream
                reader.BaseStream.Position = 0;
                reader.DiscardBufferedData();

                var hash = Hash.ComputeHash(encoding.GetBytes(content));
                newETag = BitConverter.ToString(hash).Replace("-", "").ToLower();

                if (newETag == (knownETag != null ? knownETag.ToLower() : null))
                    return null;

                var serializer = new DataContractSerializer(typeof(SolutionHead));

                //Bloody naughty way to doing it
                return (SolutionHead) serializer.ReadObject(reader.BaseStream);
            }
        }

        public SolutionDefinition GetSolution(SolutionHead deployment)
        {
            var path = Path.Combine(_basePath, deployment.SolutionId);

            if (!File.Exists(path))
            {
                return null;
            }

            using (var stream = File.OpenRead(path))
            {
                var serializer = new DataContractSerializer(typeof(SolutionDefinition));

                //Bloody naughty way to doing it
                return (SolutionDefinition)serializer.ReadObject(stream);
            }
        }

        public IEnumerable<AssemblyData> GetAssembliesAndSymbols(AssembliesHead assemblies)
        {
            var path = Path.Combine(_basePath, assemblies.AssembliesId);

            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    using (var zipStream = new ZipInputStream(stream))
                    {
                        ZipEntry entry;
                        while ((entry = zipStream.GetNextEntry()) != null)
                        {
                            if (entry.UncompressedSize == 0)
                                continue;

                            var extension = Path.GetExtension(entry.FileName).ToLowerInvariant();
                            if (extension != ".dll" && extension != ".pdb")
                                continue;

                            var data = new byte[entry.UncompressedSize];

                            try
                            {
                                zipStream.Read(data, 0, data.Length);
                            }
                            catch (Exception)
                            {
                                continue;
                            }

                            yield return new AssemblyData(entry.FileName, data);
                        }
                    }
                }
            }
        }

        public T GetItem<T>(string itemName) where T : class
        {
            throw new NotImplementedException();
        }
    }
}