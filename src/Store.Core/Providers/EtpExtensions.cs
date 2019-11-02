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
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.Object;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Providers.ChannelDataLoad;
using PDS.WITSMLstudio.Store.Providers.ChannelStreaming;
using PDS.WITSMLstudio.Store.Providers.Discovery;
using PDS.WITSMLstudio.Store.Providers.Store;

namespace PDS.WITSMLstudio.Store.Providers
{
    /// <summary>
    /// Defines static helper methods that can be used from any protocol handler.
    /// </summary>
    public static class EtpExtensions
    {
        /// <summary>
        /// Resolves the channel streaming consumer handler.
        /// </summary>
        /// <param name="etpSession">The ETP session.</param>
        /// <param name="container">The composition container.</param>
        /// <param name="contractName">The contract name.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>An <see cref="IStreamingConsumer"/> instance.</returns>
        public static IStreamingConsumer RegisterChannelStreamingConsumer(this IEtpSession etpSession, IContainer container, string contractName = null, Action<IStreamingConsumer> callback = null)
        {
            if (etpSession.SupportedVersion == EtpVersion.v11)
            {
                var handler = container.Resolve<Energistics.Etp.v11.Protocol.ChannelStreaming.IChannelStreamingConsumer>(contractName);
                var consumer = handler as IStreamingConsumer;

                callback?.Invoke(consumer);
                etpSession.Register(() => handler);

                return consumer;
            }
            else
            {
                var handler = container.Resolve<Energistics.Etp.v12.Protocol.ChannelStreaming.IChannelStreamingConsumer>(contractName);
                var consumer = handler as IStreamingConsumer;

                callback?.Invoke(consumer);
                etpSession.Register(() => handler);

                return consumer;
            }
        }

        /// <summary>
        /// Resolves the channel streaming producer handler.
        /// </summary>
        /// <param name="etpSession">The ETP session.</param>
        /// <param name="container">The composition container.</param>
        /// <param name="contractName">The contract name.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>An <see cref="IStreamingProducer"/> instance.</returns>
        public static IStreamingProducer RegisterChannelStreamingProducer(this IEtpSession etpSession, IContainer container, string contractName = null, Action<IStreamingProducer> callback = null)
        {
            if (etpSession.SupportedVersion == EtpVersion.v11)
            {
                var handler = container.Resolve<Energistics.Etp.v11.Protocol.ChannelStreaming.IChannelStreamingProducer>(contractName);
                var producer = handler as IStreamingProducer;

                callback?.Invoke(producer);
                etpSession.Register(() => handler);

                return producer;
            }
            else
            {
                var handler = container.Resolve<Energistics.Etp.v12.Protocol.ChannelStreaming.IChannelStreamingProducer>(contractName);
                var producer = handler as IStreamingProducer;

                callback?.Invoke(producer);
                etpSession.Register(() => handler);

                return producer;
            }
        }

        /// <summary>
        /// Registers the channel data load producer.
        /// </summary>
        /// <param name="etpSession">The ETP session.</param>
        /// <param name="container">The composition container.</param>
        /// <param name="contractName">The contract name.</param>
        /// <param name="callback">The callback.</param>
        /// <returns></returns>
        public static IDataLoadProducer RegisterChannelDataLoadProducer(this IEtpSession etpSession, IContainer container, string contractName = null, Action<IDataLoadProducer> callback = null)
        {
            var handler = container.Resolve<Energistics.Etp.v12.Protocol.ChannelDataLoad.IChannelDataLoadProducer>(contractName);
            var producer = handler as IDataLoadProducer;

            callback?.Invoke(producer);
            etpSession.Register(() => handler);

            return producer;
        }

        /// <summary>
        /// Resolves the discovery store handler.
        /// </summary>
        /// <param name="etpSession">The ETP session.</param>
        /// <param name="container">The composition container.</param>
        /// <param name="contractName">The contract name.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>An <see cref="IProtocolHandler"/> instance.</returns>
        public static IProtocolHandler RegisterDiscoveryStore(this IEtpSession etpSession, IContainer container, string contractName = null, Action<IProtocolHandler> callback = null)
        {
            if (etpSession.SupportedVersion == EtpVersion.v11)
            {
                var handler = container.Resolve<Energistics.Etp.v11.Protocol.Discovery.IDiscoveryStore>(contractName);
                callback?.Invoke(handler);
                etpSession.Register(() => handler);
                return handler;
            }
            else
            {
                var handler = container.Resolve<Energistics.Etp.v12.Protocol.Discovery.IDiscoveryStore>(contractName);
                callback?.Invoke(handler);
                etpSession.Register(() => handler);
                return handler;
            }
        }

        /// <summary>
        /// Registers the store customer.
        /// </summary>
        /// <param name="etpSession">The ETP session.</param>
        /// <param name="container">The container.</param>
        /// <param name="contractName">Name of the contract.</param>
        /// <param name="callback">The callback.</param>
        /// <returns></returns>
        public static IEtpStoreCustomer RegisterStoreCustomer(this IEtpSession etpSession, IContainer container, string contractName = null, Action<IEtpStoreCustomer> callback = null)
        {
            if (etpSession.SupportedVersion == EtpVersion.v11)
            {
                var handler = container.Resolve<Energistics.Etp.v11.Protocol.Store.IStoreCustomer>(contractName);
                var customer = handler as IEtpStoreCustomer;

                callback?.Invoke(customer);
                etpSession.Register(() => handler);
                return customer;
            }
            else
            {
                var handler = container.Resolve<Energistics.Etp.v12.Protocol.Store.IStoreCustomer>(contractName);
                var customer = handler as IEtpStoreCustomer;

                callback?.Invoke(customer);
                etpSession.Register(() => handler);
                return customer;
            }
        }

        /// <summary>
        /// Creates the server capabilities object for the ETP session.
        /// </summary>
        /// <param name="etpSession">The ETP session.</param>
        /// <param name="supportedObjects">The supported objects.</param>
        /// <param name="supportedEncodings">The supported encodings.</param>
        /// <returns>A new server capabilities instance.</returns>
        public static object CreateServerCapabilities(this IEtpSession etpSession, IList<string> supportedObjects, IList<string> supportedEncodings)
        {
            if (etpSession.SupportedVersion == EtpVersion.v11)
            {
                return new Energistics.Etp.v11.Datatypes.ServerCapabilities
                {
                    ApplicationName = etpSession.ApplicationName,
                    ApplicationVersion = etpSession.ApplicationVersion,
                    SupportedProtocols = etpSession.GetSupportedProtocols()
                        .Cast<Energistics.Etp.v11.Datatypes.SupportedProtocol>()
                        .ToList(),
                    SupportedObjects = supportedObjects,
                    SupportedEncodings = string.Join(";", supportedEncodings),
                    ContactInformation = new Energistics.Etp.v11.Datatypes.Contact
                    {
                        OrganizationName = WitsmlSettings.DefaultVendorName,
                        ContactName = WitsmlSettings.DefaultContactName,
                        ContactEmail = WitsmlSettings.DefaultContactEmail,
                        ContactPhone = WitsmlSettings.DefaultContactPhone
                    }
                };
            }

            return new Energistics.Etp.v12.Datatypes.ServerCapabilities
            {
                ApplicationName = etpSession.ApplicationName,
                ApplicationVersion = etpSession.ApplicationVersion,
                SupportedProtocols = etpSession.GetSupportedProtocols()
                    .Cast<Energistics.Etp.v12.Datatypes.SupportedProtocol>()
                    .ToList(),
                SupportedObjects = supportedObjects,
                SupportedEncodings = string.Join(";", supportedEncodings),
                SupportedCompression = new[] { OptionsIn.CompressionMethod.Gzip.Value },
                ContactInformation = new Energistics.Etp.v12.Datatypes.Contact
                {
                    OrganizationName = WitsmlSettings.DefaultVendorName,
                    ContactName = WitsmlSettings.DefaultContactName,
                    ContactEmail = WitsmlSettings.DefaultContactEmail,
                    ContactPhone = WitsmlSettings.DefaultContactPhone
                }
            };
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IResource" /> using the specified parameters.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uuid">The UUID.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="resourceType">The resource type.</param>
        /// <param name="name">The name.</param>
        /// <param name="count">The count.</param>
        /// <param name="lastChanged">The last changed in microseconds.</param>
        /// <returns>The resource instance.</returns>
        public static IResource CreateResource(this IEtpAdapter etpAdapter, string uuid, EtpUri uri, ResourceTypes resourceType, string name, int count = 0, long lastChanged = 0)
        {
            if (etpAdapter.SupportedVersion == EtpVersion.v11)
                return Discovery11StoreProvider.New(uuid, uri, resourceType, name, count, lastChanged);

            return Discovery12StoreProvider.New(uuid, uri, resourceType, name, count, lastChanged);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IResource" /> using the specified parameters.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="protocolUri">The protocol URI.</param>
        /// <param name="folderName">The folder name.</param>
        /// <param name="count">Elements count</param>
        /// <returns>The resource instance.</returns>
        public static IResource NewProtocol(this IEtpAdapter etpAdapter, EtpUri protocolUri, string folderName, int count = -1)
        {
            if (etpAdapter.SupportedVersion == EtpVersion.v11)
                return Discovery11StoreProvider.NewProtocol(protocolUri, folderName, count);

            return Discovery12StoreProvider.NewProtocol(protocolUri, folderName, count);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IResource" /> using the specified parameters.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="folderName">The folder name.</param>
        /// <param name="childCount">The child count.</param>
        /// <param name="appendFolderName">if set to <c>true</c> append folder name.</param>
        /// <returns>A new <see cref="IResource"/> instance.</returns>
        public static IResource NewFolder(this IEtpAdapter etpAdapter, EtpUri parentUri, EtpContentType contentType, string folderName, int childCount = -1, bool appendFolderName = false)
        {
            if (etpAdapter.SupportedVersion == EtpVersion.v11)
                return Discovery11StoreProvider.NewFolder(parentUri, contentType, folderName, childCount, appendFolderName);

            return Discovery12StoreProvider.NewFolder(parentUri, contentType, folderName, childCount, appendFolderName);
        }

        /// <summary>
        /// Sets the properties of the <see cref="IDataObject" /> instance.
        /// </summary>
        /// <typeparam name="T">The type of entity.</typeparam>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="dataObject">The data object.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="name">The name.</param>
        /// <param name="childCount">The child count.</param>
        /// <param name="lastChanged">The last changed in microseconds.</param>
        /// <param name="compress">if set to <c>true</c> compress the data object.</param>
        public static void SetDataObject<T>(this IEtpAdapter etpAdapter, IDataObject dataObject, T entity, EtpUri uri, string name, int childCount = -1, long lastChanged = 0, bool compress = true)
        {
            // There's nothing to set if the data object is null
            if (dataObject == null) return;

            if (etpAdapter.SupportedVersion == EtpVersion.v11)
            {
                Store11StoreProvider.SetDataObject(dataObject, entity, uri, name, childCount, lastChanged, compress);
            }
            else
            {
                Store12StoreProvider.SetDataObject(dataObject, entity, uri, name, childCount, lastChanged);
            }
        }
    }
}
