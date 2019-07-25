//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avro.Specific;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Protocol.Core;
using Energistics.Etp.Native;
using Energistics.Etp.v11;
using Energistics.Etp.v11.Datatypes.Object;
using Energistics.Etp.v11.Protocol.ChannelStreaming;
using Energistics.Etp.v11.Protocol.Core;
using Energistics.Etp.v11.Protocol.Discovery;
using Energistics.Etp.v11.Protocol.Store;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store
{
    /// <summary>
    /// Common base class for all integration tests.
    /// </summary>
    public abstract class IntegrationTestBase
    {
        private IContainer _container;
        protected IEtpSelfHostedWebServer _server;
        protected IEtpClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTestBase"/> class.
        /// </summary>
        protected IntegrationTestBase()
        {
            Logger = LogManager.GetLogger(GetType());
        }

        /// <summary>
        /// Gets or sets the test context.
        /// </summary>
        /// <value>The test context.</value>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Gets the logger associated with the current type.
        /// </summary>
        /// <value>The logger.</value>
        protected ILog Logger { get; }

        /// <summary>
        /// Initializes common resources.
        /// </summary>
        /// <param name="container">The composition container.</param>
        protected void EtpSetUp(IContainer container)
        {
            _container = container;

            // Clean up any remaining resources
            _client?.Dispose();
            _server?.Dispose();

            // Get next available port number
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            // Update EtpServerUrl setting
            var uri = new Uri(TestSettings.EtpServerUrl);
            var url = TestSettings.EtpServerUrl.Replace($":{uri.Port}", $":{port}");

            // Create server and client instances
            _server = CreateServer(port);
            _client = InitClient(CreateClient(url));

            // Resolve dependencies early to avoid object disposed
            var streaming = container.Resolve<IChannelStreamingProducer>();
            var discovery = container.Resolve<IDiscoveryStore>();
            var store = container.Resolve<IStoreStore>();

            // Register server handlers
            _server.Register(() => streaming);
            _server.Register(() => discovery);
            _server.Register(() => store);
        }

        /// <summary>
        /// Disposes common resources.
        /// </summary>
        protected void EtpCleanUp()
        {
            _client?.Dispose();
            _server?.Dispose();
            _client = null;
            _server = null;

            TestSettings.Reset();
        }

        /// <summary>
        /// Creates an <see cref="IEtpSelfHostedWebServer"/> instance.
        /// </summary>
        /// <param name="port">The port number.</param>
        /// <returns>A new <see cref="IEtpSelfHostedWebServer"/> instance.</returns>
        protected IEtpSelfHostedWebServer CreateServer(int port)
        {
            var version = GetType().Assembly.GetName().Version.ToString();
            var server = EtpFactory.CreateSelfHostedWebServer(port, GetType().AssemblyQualifiedName, version);
            
            return server;
        }

        /// <summary>
        /// Creates an <see cref="IEtpClient"/> instance configurated with the
        /// current connection and authorization parameters.
        /// </summary>
        /// <param name="url">The WebSocket URL.</param>
        /// <returns>A new <see cref="IEtpClient"/> instance.</returns>
        protected IEtpClient CreateClient(string url)
        {
            var version = GetType().Assembly.GetName().Version.ToString();
            var headers = Energistics.Etp.Security.Authorization.Basic(TestSettings.Username, TestSettings.Password);
            var etpSubProtocol = EtpSettings.Etp11SubProtocol;

            var client = EtpFactory.CreateClient(url ?? TestSettings.EtpServerUrl, GetType().AssemblyQualifiedName, version, etpSubProtocol, headers);
            
            return client;
        }

        /// <summary>
        /// Initializes the client.
        /// </summary>
        /// <param name="client">The ETP client.</param>
        /// <returns>The ETP client.</returns>
        protected IEtpClient InitClient(IEtpClient client)
        {
            // Register client handlers
            client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
            client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
            client.Register<IStoreCustomer, StoreCustomerHandler>();

            return client;
        }

        /// <summary>
        /// Handles an event asynchronously and waits for it to complete.
        /// </summary>
        /// <typeparam name="T">The type of ETP message.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="milliseconds">The timeout in milliseconds.</param>
        /// <returns>An awaitable task.</returns>
        protected async Task<ProtocolEventArgs<T>> HandleAsync<T>(
            Action<ProtocolEventHandler<T>> action,
            int? milliseconds = null)
            where T : ISpecificRecord
        {
            ProtocolEventArgs<T> args = null;
            var task = new Task<ProtocolEventArgs<T>>(() => args);

            action((s, e) =>
            {
                args = e;

                if (task.Status == TaskStatus.Created)
                    task.Start();
            });

            return await task.WaitAsync(milliseconds);
        }

        /// <summary>
        /// Handles an event asynchronously and waits for it to complete.
        /// </summary>
        /// <typeparam name="T">The type of ETP message.</typeparam>
        /// <typeparam name="TContext">The type of the context.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="milliseconds">The timeout in milliseconds.</param>
        /// <returns>An awaitable task.</returns>
        protected async Task<ProtocolEventArgs<T, TContext>> HandleAsync<T, TContext>(
            Action<ProtocolEventHandler<T, TContext>> action,
            int? milliseconds = null)
            where T : ISpecificRecord
        {
            ProtocolEventArgs<T, TContext> args = null;
            var task = new Task<ProtocolEventArgs<T, TContext>>(() => args);

            action((s, e) =>
            {
                args = e;

                if (task.Status == TaskStatus.Created)
                    task.Start();
            });

            return await task.WaitAsync(milliseconds);
        }

        /// <summary>
        /// Handles a multi-part event asynchronously and waits for it to complete.
        /// </summary>
        /// <typeparam name="T">The type of ETP message.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="maxMultiPartsToReturn">The maximum count of multi-part messages to return.</param>
        /// <param name="milliseconds">The timeout in milliseconds.</param>
        /// <returns>An awaitable task.</returns>
        protected async Task<List<ProtocolEventArgs<T>>> HandleMultiPartAsync<T>(
            Action<ProtocolEventHandler<T>> action,
            int maxMultiPartsToReturn = 0,
            int? milliseconds = null)
            where T : ISpecificRecord
        {
            var args = new List<ProtocolEventArgs<T>>();
            var task = new Task<List<ProtocolEventArgs<T>>>(() => args);

            action((s, e) =>
            {
                args.Add(e);

                if (task.Status == TaskStatus.Created &&
                    (e.Header.IsFinalResponse() ||
                    (maxMultiPartsToReturn > 0 && args.Count >= maxMultiPartsToReturn)))
                    task.Start();
            });

            return await task.WaitAsync(milliseconds ?? TestSettings.DefaultTimeoutInMilliseconds * 5);
        }

        /// <summary>
        /// Handles a multi-part event asynchronously and waits for it to complete.
        /// </summary>
        /// <typeparam name="T">The type of ETP message.</typeparam>
        /// <typeparam name="TContext">The type of the context.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="maxMultiPartsToReturn">The maximum count of multi-part messages to return.</param>
        /// <param name="milliseconds">The timeout in milliseconds.</param>
        /// <returns>An awaitable task.</returns>
        protected async Task<List<ProtocolEventArgs<T, TContext>>> HandleMultiPartAsync<T, TContext>(
            Action<ProtocolEventHandler<T, TContext>> action,
            int maxMultiPartsToReturn = 0,
            int? milliseconds = null)
            where T : ISpecificRecord
        {
            var args = new List<ProtocolEventArgs<T, TContext>>();
            var task = new Task<List<ProtocolEventArgs<T, TContext>>>(() => args);

            action((s, e) =>
            {
                args.Add(e);

                if (task.Status == TaskStatus.Created &&
                    (e.Header.IsFinalResponse() ||
                    (maxMultiPartsToReturn > 0 && args.Count >= maxMultiPartsToReturn)))
                    task.Start();
            });

            return await task.WaitAsync(milliseconds ?? TestSettings.DefaultTimeoutInMilliseconds * 5);
        }

        protected DataObject CreateDataObject<TList, TObject>(EtpUri uri, TObject instance)
            where TList : IEnergisticsCollection
            where TObject : IDataObject
        {
            var dataObject = new DataObject()
            {
                Resource = new Resource()
                {
                    Uri = uri,
                    ContentType = uri.ContentType,
                    Name = instance.Name,
                    ResourceType = ResourceTypes.DataObject.ToString(),
                    CustomData = new Dictionary<string, string>(),
                    HasChildren = -1
                }
            };

            var list = ObjectTypes.GetObjectTypeListPropertyInfo(uri.ObjectType, uri.Family, uri.Version);
            var collection = Activator.CreateInstance<TList>();

            list.SetValue(collection, new List<TObject> { instance });
            dataObject.SetString(EnergisticsConverter.ObjectToXml(collection));

            return dataObject;
        }

        protected DataObject CreateDataObject<TObject>(EtpUri uri, TObject instance) where TObject : AbstractObject
        {
            var dataObject = new DataObject()
            {
                Resource = new Resource()
                {
                    Uri = uri,
                    ContentType = uri.ContentType,
                    Name = $"{instance.Citation?.Title}",
                    ResourceType = ResourceTypes.DataObject.ToString(),
                    CustomData = new Dictionary<string, string>(),
                    HasChildren = -1
                }
            };

            dataObject.SetString(EnergisticsConverter.ObjectToXml(instance));

            return dataObject;
        }

        protected TObject Parse<TList, TObject>(string xml) where TList : IEnergisticsCollection
        {
            var collection = EnergisticsConverter.XmlToObject<TList>(xml);
            Assert.IsNotNull(collection);

            var dataObject = collection.Items.Cast<TObject>().FirstOrDefault();
            Assert.IsNotNull(dataObject);

            return dataObject;
        }

        protected TObject Parse<TObject>(string xml) where TObject : AbstractObject
        {
            var dataObject = EnergisticsConverter.XmlToObject<TObject>(xml);
            Assert.IsNotNull(dataObject);
            return dataObject;
        }

        /// <summary>
        /// Requests a new session and asserts.
        /// </summary>
        /// <param name="retries">The number of retries.</param>
        /// <returns>The <see cref="OpenSession" /> message args.</returns>
        protected async Task<ProtocolEventArgs<OpenSession>> RequestSessionAndAssert(int retries = 10)
        {
            try
            {
                var client = _client;

                // Register event handler for OpenSession response
                var onOpenSession = HandleAsync<OpenSession>(
                    x => client.Handler<ICoreClient>().OnOpenSession += x);

                // Wait for Open connection
                var isOpen = await _client.OpenAsyncWithTimeout();
                Assert.IsTrue(isOpen);

                // Wait for OpenSession
                var openArgs = await onOpenSession;

                // Verify OpenSession and Supported Protocols
                VerifySessionWithProtcols(openArgs, Protocols.ChannelStreaming, Protocols.Discovery, Protocols.Store);

                return openArgs;
            }
            catch (TimeoutException)
            {
                if (retries < 1) throw;

                await Task.Delay(TestSettings.DefaultTimeoutInMilliseconds);
                Logger.Warn("Retrying connection attempt after timeout");

                if (retries == 1)
                {
                    _client?.Dispose();
                    _client = InitClient(CreateClient(TestSettings.FallbackServerUrl));
                }
                else
                {
                    EtpSetUp(_container);
                    _server.Start();
                }

                return await RequestSessionAndAssert(retries - 1);
            }
        }

        /// <summary>
        /// Gets the resources and asserts.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="exist">if set to <c>true</c> resources exist; otherwise, <c>false</c>.</param>
        /// <returns>A collection of resources.</returns>
        protected async Task<List<ProtocolEventArgs<GetResourcesResponse, string>>> GetResourcesAndAssert(EtpUri uri, EtpErrorCodes? errorCode = null, bool exist = true)
        {
            var handler = _client.Handler<IDiscoveryCustomer>();

            // Register event handler for GetResourcesResponse
            var onGetResourcesResponse = HandleMultiPartAsync<GetResourcesResponse, string>(
                x => handler.OnGetResourcesResponse += x);

            // Register exception hanlder
            var onProtocolException = HandleAsync<IProtocolException>(x => handler.OnProtocolException += x);

            // Send GetResources message for specified URI
            var messageId = handler.GetResources(uri);
            List<ProtocolEventArgs<GetResourcesResponse, string>> args = null;

            var tokenSource = new CancellationTokenSource();

            var taskList = new List<Task>()
            {
                WaitFor(onGetResourcesResponse, tokenSource.Token),
                WaitFor(onProtocolException, tokenSource.Token)
            };

            // Start each event
            taskList.ForEach(task => task.Start());

            // Wait for a task to finish
            await Task.WhenAny(taskList);

            // Cancel the rest of the task
            tokenSource.Cancel();

            // Wait for the rest to be finished
            await Task.WhenAll(taskList);

            if (onGetResourcesResponse.Status == TaskStatus.RanToCompletion)
            {
                // Wait for GetResourcesResponse
                args = await onGetResourcesResponse;
                Assert.IsNotNull(args);
                Assert.AreEqual(exist, args.Any());

                var folder = ResourceTypes.Folder.ToString();

                // Check Resource URIs
                foreach (var arg in args)
                {
                    VerifyCorrelationId(arg, messageId);
                    Assert.IsNotNull(arg?.Message?.Resource?.Uri);

                    var resourceUri = new EtpUri(arg.Message.Resource.Uri);

                    if (uri == EtpUri.RootUri)
                    {
                        Assert.IsTrue(uri.IsBaseUri);
                    }
                    else if (!folder.EqualsIgnoreCase(arg.Message.Resource.ResourceType))
                    {
                        Assert.AreEqual(uri.Family, resourceUri.Family);
                        Assert.AreEqual(uri.Version, resourceUri.Version);
                    }
                }
            }
            if (onProtocolException.Status == TaskStatus.RanToCompletion)
            {
                var exceptionArgs = onProtocolException.Result;

                // Assert exception details
                Assert.IsNotNull(errorCode);
                Assert.IsNotNull(exceptionArgs?.Message);
                Assert.AreEqual((int)errorCode, exceptionArgs.Message.ErrorCode);
            }

            return args;
        }

        /// <summary>
        /// Gets the dataObject from the URI and asserts.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns>The ProtocolEventArgs.</returns>
        protected async Task<ProtocolEventArgs<Energistics.Etp.v11.Protocol.Store.Object>> GetAndAssert(IStoreCustomer handler, EtpUri uri, EtpErrorCodes? errorCode = null)
        {
            // Register event handler for root URI
            var onObject = HandleAsync<Energistics.Etp.v11.Protocol.Store.Object>(x => handler.OnObject += x);

            // Register exception hanlder
            var onProtocolException = HandleAsync<IProtocolException>(x => handler.OnProtocolException += x);

            // Send GetObject for non-existant URI
            handler.GetObject(uri);
            ProtocolEventArgs<Energistics.Etp.v11.Protocol.Store.Object> args = null;

            var tokenSource = new CancellationTokenSource();

            var taskList = new List<Task>()
            {
                WaitFor(onObject, tokenSource.Token),
                WaitFor(onProtocolException, tokenSource.Token)
            };

            // Start each event
            taskList.ForEach(task => task.Start());

            // Wait for a task to finish
            await Task.WhenAny(taskList);

            // Cancel the rest of the task
            tokenSource.Cancel();

            // Wait for the rest to be finished
            await Task.WhenAll(taskList);

            if (onObject.Status == TaskStatus.RanToCompletion)
            {
                args = onObject.Result;

                // Check for DataObject
                Assert.IsNotNull(args?.Message.DataObject);
            }
            if (onProtocolException.Status == TaskStatus.RanToCompletion)
            {
                var exceptionArgs = onProtocolException.Result;

                // Assert exception details
                Assert.IsNotNull(errorCode);
                Assert.IsNotNull(exceptionArgs?.Message);
                Assert.AreEqual((int)errorCode, exceptionArgs.Message.ErrorCode);
            }

            return args;
        }

        /// <summary>
        /// Puts the dataObject and asserts.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="dataObject">The data object.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns>The task.</returns>
        protected async Task PutAndAssert(IStoreCustomer handler, DataObject dataObject, EtpErrorCodes? errorCode = null)
        {
            // Register event handler for Acknowledge response
            var onAcknowledge = HandleAsync<IAcknowledge>(x => handler.OnAcknowledge += x);

            // Register exception hanlder
            var onProtocolException = HandleAsync<IProtocolException>(x => handler.OnProtocolException += x);

            // Send PutObject message for new data object
            var messageId = handler.PutObject(dataObject);

            var tokenSource = new CancellationTokenSource();

            var taskList = new List<Task>
            {
                WaitFor(onAcknowledge, tokenSource.Token),
                WaitFor(onProtocolException, tokenSource.Token)
            };

            // Start each event
            taskList.ForEach(task => task.Start());

            // Wait for a task to finish
            await Task.WhenAny(taskList);

            // Cancel the rest of the task
            tokenSource.Cancel();

            // Wait for the rest to be finished
            await Task.WhenAll(taskList);

            // Check error code
            if (onProtocolException.Status == TaskStatus.RanToCompletion)
            {
                var exceptionArgs = onProtocolException.Result;

                // Assert exception details
                Assert.IsNotNull(errorCode);
                Assert.IsNotNull(exceptionArgs?.Message);
                Assert.AreEqual((int)errorCode, exceptionArgs.Message.ErrorCode);
            }
            // Check for valid acknowledgement
            else
            {
                var acknowledge = onAcknowledge.Result;

                // Assert acknowledgement and messageId
                VerifyCorrelationId(acknowledge, messageId);
            }
        }

        /// <summary>
        /// Deletes the dataObject from the URI and asserts.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns>The ProtocolEventArgs.</returns>
        protected async Task DeleteAndAssert(IStoreCustomer handler, EtpUri uri, EtpErrorCodes? errorCode = null)
        {
            // Register event handler for Acknowledge response
            var onAcknowledge = HandleAsync<IAcknowledge>(x => handler.OnAcknowledge += x);

            // Register exception hanlder
            var onProtocolException = HandleAsync<IProtocolException>(x => handler.OnProtocolException += x);

            // Send GetObject for non-existant URI
            var messageId = handler.DeleteObject(uri);

            var tokenSource = new CancellationTokenSource();

            var taskList = new List<Task>
            {
                WaitFor(onAcknowledge, tokenSource.Token),
                WaitFor(onProtocolException, tokenSource.Token)
            };

            // Start each event
            taskList.ForEach(task => task.Start());

            // Wait for a task to finish
            await Task.WhenAny(taskList);

            // Cancel the rest of the task
            tokenSource.Cancel();

            // Wait for the rest to be finished
            await Task.WhenAll(taskList);

            if (onAcknowledge.Status == TaskStatus.RanToCompletion)
            {
                var acknowledge = onAcknowledge.Result;

                // Assert acknowledgement and messageId
                VerifyCorrelationId(acknowledge, messageId);
            }
            if (onProtocolException.Status == TaskStatus.RanToCompletion)
            {
                var exceptionArgs = onProtocolException.Result;

                // Assert exception details
                Assert.IsNotNull(errorCode);
                Assert.IsNotNull(exceptionArgs?.Message);
                Assert.AreEqual((int)errorCode, exceptionArgs.Message.ErrorCode);
            }
        }

        protected Task WaitFor(Task task, CancellationToken token)
        {
            return new Task(() =>
            {
                try
                {
                    task.Wait(token);
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }

        protected void VerifyCorrelationId<TMessage>(ProtocolEventArgs<TMessage> args, long messageId) where TMessage : ISpecificRecord
        {
            // Verify Correlation ID
            Assert.IsNotNull(args?.Header);
            Assert.AreEqual(messageId, args.Header.CorrelationId);
        }

        protected void VerifyProtocolException(ProtocolEventArgs<ProtocolException> args, long messageId, EtpErrorCodes errorCode)
        {
            VerifyCorrelationId(args, messageId);

            // Verify Error Code
            Assert.IsNotNull(args?.Message);
            Assert.AreEqual((int)errorCode, args.Message.ErrorCode);
        }

        protected void VerifySessionWithProtcols(ProtocolEventArgs<OpenSession> args, params Protocols[] requestedProtocols)
        {
            // Verify OpenSession response
            Assert.IsNotNull(args?.Message?.SessionId);
            var message = args.Message;

            // Verify Session ID
            Guid sessionId;
            Assert.IsNotNull(message.SessionId);
            Assert.IsTrue(Guid.TryParse(message.SessionId, out sessionId));
            Assert.IsNotNull(sessionId);

            // Verify count of Supported Protocols
            var supportedProtocols = message.SupportedProtocols;
            Assert.IsTrue(supportedProtocols.Count <= requestedProtocols.Length);

            // Verify Supported Protocols
            foreach (var protocol in supportedProtocols)
            {
                var version = protocol.ProtocolVersion;

                // Verify against requested protocols
                Assert.IsTrue(requestedProtocols.Any(x => protocol.Protocol == (int)x));

                // Verify protocol versions
                Assert.AreEqual(TestSettings.EtpVersion, $"{version.Major}.{version.Minor}");
            }
        }
    }
}
