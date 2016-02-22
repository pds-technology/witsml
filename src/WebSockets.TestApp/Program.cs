using System;
using System.IO;
using Energistics.Common;
using Energistics.Providers;
using Energistics.Protocol.Discovery;
using log4net.Config;

namespace Energistics
{
    public class Program
    {
        private const string ServerAppName = "org.energistics.server";
        private const string ClientAppName = "org.energistics.client";

        private const string WebSocketUri = "ws://localhost/Witsml.Web/api/etp"; // IIS
        //private const string WebSocketUri = "ws://localhost:9000"; // TestApp
        private const int WebSocketPort = 9000;

        public static void Main(string[] args)
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));
            Start();
        }

        private static void Start()
        {
            Console.Write("Press 'S' to start a web socket server,");
            Console.WriteLine(" or press 'C' to start a client instance...");

            var key = Console.ReadKey();

            Console.WriteLine(" - processing...");
            Console.WriteLine();

            if (IsKey(key, "S"))
            {
                StartServer();
            }
            else if (IsKey(key, "C"))
            {
                StartClient();
            }
            else
            {
                Start();
            }
        }

        private static void StartServer()
        {
            Console.WriteLine("Select from the following options:");
            Console.WriteLine(" S - start / stop");
            Console.WriteLine(" Z - clear");
            Console.WriteLine(" X - exit");
            Console.WriteLine();

            using (var server = new EtpSocketServer(WebSocketPort, ServerAppName))
            {
                //server.Register<ICoreServer, CoreServerHandler>();
                server.Register<IDiscoveryStore, MockResourceProvider>();

                while (true)
                {
                    var info = Console.ReadKey();

                    Console.WriteLine(" - processing...");
                    Console.WriteLine();

                    if (IsKey(info, "S"))
                    {
                        if (server.IsRunning)
                            server.Stop();
                        else
                            server.Start();
                    }
                    else if (IsKey(info, "Z"))
                    {
                        Console.Clear();
                    }
                    else if (IsKey(info, "X"))
                    {
                        break;
                    }
                }
            }
        }

        private static void StartClient()
        {
            Console.WriteLine("Select from the following options:");
            Console.WriteLine(" O - open");
            Console.WriteLine(" C - close");
            Console.WriteLine(" D - discover");
            Console.WriteLine(" Z - clear");
            Console.WriteLine(" X - exit");
            Console.WriteLine();

            using (var client = new EtpClient(WebSocketUri, ClientAppName))
            {
                //client.Register<ICoreClient, CoreClientHandler>();
                client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();

                client.Handler<IDiscoveryCustomer>().OnGetResourcesResponse += OnGetResourcesResponse;

                while (true)
                {
                    var info = Console.ReadKey();

                    Console.WriteLine(" - processing...");
                    Console.WriteLine();

                    if (IsKey(info, "O"))
                    {
                        client.Open();
                    }
                    else if (IsKey(info, "C"))
                    {
                        client.Close();
                    }
                    else if (IsKey(info, "D"))
                    {
                        Console.WriteLine("Enter resource URI:");
                        var uri = Console.ReadLine().Trim();
                        Console.WriteLine();

                        client.Handler<IDiscoveryCustomer>()
                            .GetResources(uri);
                    }
                    else if (IsKey(info, "Z"))
                    {
                        Console.Clear();
                    }
                    else if (IsKey(info, "X"))
                    {
                        break;
                    }
                }
            }
        }

        private static void OnGetResourcesResponse(object sender, ProtocolEventArgs<GetResourcesResponse> e)
        {
            Console.WriteLine(((EtpBase)sender).Serialize(e.Message.Resource, true));
        }

        private static bool IsKey(ConsoleKeyInfo info, string key)
        {
            return info.KeyChar.ToString().ToLower() == key.ToLower();
        }
    }
}
