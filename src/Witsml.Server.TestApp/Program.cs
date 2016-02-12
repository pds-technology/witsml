using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Xml;
using log4net.Config;
using PDS.Framework;

namespace PDS.Witsml.Server.TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));

            //InitializeProvider();
            InitializeProvider("MongoDb");

            var endpoint = new Uri("http://localhost:5050/WitsmlStore.svc");

            Console.WriteLine("Witsml Service listenting at {0}", endpoint);
            Console.WriteLine("Press X to exit...");
            Console.WriteLine();

            using (var container = ContainerFactory.Create())
            using (var witsml = InitializeWitsmlService(container, endpoint))
            {
                witsml.Open();

                while (true)
                {
                    var info = Console.ReadKey();
                    Console.WriteLine(" - processing...");

                    if (IsKey(info, "X"))
                    {
                        break;
                    }
                }
            }

            //CleanUp();
        }

        private static void InitializeProvider()
        {
            Console.WriteLine("Starting Witsml Service...");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("Select data provider:");
                Console.WriteLine(" C - Cassandra");
                Console.WriteLine(" M - MongoDb");
                Console.WriteLine(" R - RavenDb");
                Console.WriteLine(" S - SQLite");
                Console.WriteLine();

                var info = Console.ReadKey();
                Console.Write(" - ");

                if (IsKey(info, "C"))
                {
                    InitializeProvider("Cassandra");
                }
                else if (IsKey(info, "M"))
                {
                    InitializeProvider("MongoDb");
                }
                else if (IsKey(info, "R"))
                {
                    var path = InitializeProvider("RavenDb");

                    CopyFiles(
                        Directory.GetFiles(path, "*.zip"),
                        Environment.CurrentDirectory);
                }
                else if (IsKey(info, "S"))
                {
                    var path = InitializeProvider("SQLite");

                    CopyFiles(
                        Directory.GetFiles(Path.Combine(path, "x86")),
                        Path.Combine(Environment.CurrentDirectory, "x86"));

                    CopyFiles(
                        Directory.GetFiles(Path.Combine(path, "x64")),
                        Path.Combine(Environment.CurrentDirectory, "x64"));
                }
                else
                {
                    Console.WriteLine("Not supported!");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine();
                break;
            }
        }

        private static string InitializeProvider(string provider)
        {
            Console.WriteLine("Copying {0} libraries...", provider);

            var path = Path.Combine(Environment.CurrentDirectory, String.Format(@"..\..\Witsml.Server.{0}\bin\Debug", provider));
            var files = Directory.GetFiles(path, "*.dll").Union(Directory.GetFiles(path, "*.pdb"));

            CopyFiles(files, Environment.CurrentDirectory);

            return path;
        }

        private static void CopyFiles(IEnumerable<string> files, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in files)
            {
                try
                {
                    File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
                }
                catch (Exception)
                {
                    // ignore...
                }
            }
        }

        private static ServiceHost InitializeWitsmlService(IContainer container, Uri endpoint)
        {
            var serviceHost = new ServiceHost(
                container.Resolve<IWitsmlStore>(),
                endpoint);

            // Enable metadata publishing.
            var smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
            serviceHost.Description.Behaviors.Add(smb);

            // Allow use of singleton instance
            var sba = serviceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            sba.InstanceContextMode = InstanceContextMode.Single;
            sba.IncludeExceptionDetailInFaults = true;

            // Add error handler
            serviceHost.Description.Behaviors.Add(
                new Framework.Web.Services.ServiceErrorBehavior());

            // Set HTTP binding configuration
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None);
            binding.CloseTimeout = new TimeSpan(00, 10, 00);
            binding.OpenTimeout = new TimeSpan(00, 10, 00);
            binding.ReceiveTimeout = new TimeSpan(00, 10, 00);
            binding.SendTimeout = new TimeSpan(00, 10, 00);
            binding.TextEncoding = System.Text.Encoding.UTF8;
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.MaxBufferSize = int.MaxValue;
            binding.MaxBufferPoolSize = int.MaxValue;
            binding.ReaderQuotas = XmlDictionaryReaderQuotas.Max;

            serviceHost.AddServiceEndpoint(typeof(IWitsmlStore), binding, endpoint);

            return serviceHost;
        }

        private static bool IsKey(ConsoleKeyInfo info, string key)
        {
            return info.KeyChar.ToString().ToUpperInvariant() == key.ToUpperInvariant();
        }

        private static void CleanUp()
        {
            var path = Environment.CurrentDirectory;
            var files = Directory.GetFiles(path, "*.dll").Union(Directory.GetFiles(path, "*.pdb"));

            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                    // ignore...
                }
            }
        }
    }
}
