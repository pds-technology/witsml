//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using Energistics.DataAccess;
using System.Net;
using Energistics.Etp;
using Energistics.Etp.Common;

namespace PDS.WITSMLstudio.Connections
{
    /// <summary>
    /// Defines static helper methods that can be used to configure WITSML store connections.
    /// </summary>
    public static class ConnectionExtensions
    {
        /// <summary>
        /// Creates a WITSMLWebServiceConnection for the current connection uri and WITSML version.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="version">The WITSML version.</param>
        /// <returns>A <see cref="WITSMLWebServiceConnection"/> instance.</returns>
        public static WITSMLWebServiceConnection CreateProxy(this Connection connection, WMLSVersion version)
        {
            //_log.DebugFormat("A new Proxy is being created with URI: {0}; WitsmlVersion: {1};", connection.Uri, version);
            return connection.UpdateProxy(new WITSMLWebServiceConnection(connection.Uri, version));
        }

        /// <summary>
        /// Updates a WITSMLWebServiceConnection for the current connection settings.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="proxy">The WITSML web service proxy.</param>
        /// <returns>The <see cref="WITSMLWebServiceConnection"/> instance.</returns>
        public static WITSMLWebServiceConnection UpdateProxy(this Connection connection, WITSMLWebServiceConnection proxy)
        {
            proxy.Proxy = connection.CreateWebProxy();
            proxy.Url = connection.Uri;
            proxy.Timeout *= 5;
            proxy.AcceptCompressedResponses = connection.SoapAcceptCompressedResponses;
            proxy.CompressRequests = connection.SoapRequestCompressionMethod == CompressionMethods.Gzip;

            connection.SetServerCertificateValidation();

            if (!string.IsNullOrWhiteSpace(connection.Username))
            {
                if (connection.PreAuthenticate)
                {
                    proxy.Headers = connection.GetAuthorizationHeader();
                    proxy.IsPreAuthenticationEnabled = connection.PreAuthenticate;
                }
                else
                {
                    proxy.Username = connection.Username;
                    proxy.SetSecurePassword(connection.SecurePassword);
                }
            }

            return proxy;
        }

        /// <summary>
        /// Creates a web proxy for the current connection settings.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>A new <see cref="WebProxy"/> instance.</returns>
        public static WebProxy CreateWebProxy(this Connection connection)
        {
            if (string.IsNullOrWhiteSpace(connection.ProxyHost)) return null;

            var proxy = connection.ProxyHost.Contains("://")
                ? new WebProxy(new Uri(connection.ProxyHost))
                : new WebProxy(connection.ProxyHost, connection.ProxyPort);

            if (!string.IsNullOrWhiteSpace(connection.ProxyUsername) &&
                !string.IsNullOrWhiteSpace(connection.ProxyPassword))
            {
                proxy.Credentials = new NetworkCredential(connection.ProxyUsername, connection.SecureProxyPassword);
            }

            proxy.UseDefaultCredentials = connection.ProxyUseDefaultCredentials;

            return proxy;
        }

        /// <summary>
        /// Creates a Json client for the current connection uri.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>An <see cref="Energistics.Etp.JsonClient"/> instance.</returns>
        public static JsonClient CreateJsonClient(this Connection connection)
        {
            return connection.IsAuthenticationBasic
                ? new JsonClient(connection.Username, connection.Password, connection.CreateWebProxy())
                : new JsonClient(connection.JsonWebToken, connection.CreateWebProxy());
        }

        /// <summary>
        /// Creates an ETP client for the current connection
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="applicationVersion">The application version.</param>
        /// <returns>An <see cref="Energistics.Etp.Common.IEtpClient"/> instance.</returns>
        public static IEtpClient CreateEtpClient(this Connection connection, string applicationName, string applicationVersion)
        {
            var headers = connection.GetAuthorizationHeader();

            connection.UpdateEtpSettings(headers);
            connection.SetServerCertificateValidation();

            var client = EtpFactory.CreateClient(connection.WebSocketType, connection.Uri, applicationName, applicationVersion, connection.SubProtocol, headers);

            client.SetSecurityOptions(connection.SecurityProtocol, connection.AcceptInvalidCertificates);

            if (!string.IsNullOrWhiteSpace(connection.ProxyHost))
            {
                client.SetProxy(connection.ProxyHost, connection.ProxyPort,
                                connection.ProxyUsername, connection.ProxyPassword);
            }
            if (!string.IsNullOrWhiteSpace(connection.EtpCompression))
            {
                client.SetSupportedCompression(connection.EtpCompression);
            }

            return client;
        }

        /// <summary>
        /// Gets the authorization header for the current connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>The authorization header, as a dictionary.</returns>
        public static IDictionary<string, string> GetAuthorizationHeader(this Connection connection)
        {
            return connection.IsAuthenticationBasic
                   ? Energistics.Etp.Security.Authorization.Basic(connection.Username, connection.Password)
                   : Energistics.Etp.Security.Authorization.Bearer(connection.JsonWebToken);
        }

        /// <summary>
        /// Sets the server certificate validation.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public static void SetServerCertificateValidation(this Connection connection)
        {
            ServicePointManager.SecurityProtocol = connection.SecurityProtocol;

            if (connection.AcceptInvalidCertificates)
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;
            else
                ServicePointManager.ServerCertificateValidationCallback = null;
        }

        /// <summary>
        /// Gets the ETP server capabilities URL for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="includeGetVersion">if set to <c>true</c> include the GetVersion query parameter.</param>
        /// <returns>The well-known server capabilities URL.</returns>
        public static string GetEtpServerCapabilitiesUrl(this Connection connection, bool includeGetVersion = true)
        {
            if (string.IsNullOrWhiteSpace(connection?.Uri))
                return string.Empty;

            var etpVersion = includeGetVersion
                ? $"?{EtpSettings.GetVersionHeader}={WebUtility.UrlEncode(connection.SubProtocol)}"
                : string.Empty;

            return $"http{connection.Uri.Substring(2)}/.well-known/etp-server-capabilities{etpVersion}";
        }

        /// <summary>
        /// Gets the ETP server capabilities for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>The server capabilities result</returns>
        public static object GetEtpServerCapabilities(this Connection connection) =>
            CreateJsonClient(connection).GetServerCapabilities(GetEtpServerCapabilitiesUrl(connection));

        /// <summary>
        /// Gets the ETP versions for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>The ETP versions</returns>
        public static IList<string> GetEtpVersions(this Connection connection)
        {
            var url = $"{GetEtpServerCapabilitiesUrl(connection, false)}?{EtpSettings.GetVersionsHeader}=true";
            return CreateJsonClient(connection).GetEtpVersions(url);
        }

        /// <summary>
        /// Updates the ETP settings based on the connection settings.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="headers">The headers.</param>
        public static void UpdateEtpSettings(this Connection connection, IDictionary<string, string> headers)
        {
            // Allow settings to be blanked out via Connection dialog
            EtpSettings.EtpSubProtocolName = connection.SubProtocol ?? string.Empty;
            headers[EtpSettings.EtpEncodingHeader] = connection.EtpEncoding ?? string.Empty;
        }
    }
}
