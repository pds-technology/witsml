//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Avro.Specific;
using Energistics.Datatypes;

namespace Energistics.Common
{
    /// <summary>
    /// Defines the properties and methods needed to manage an ETP session.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IEtpSession : IDisposable
    {
        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        /// <value>The name of the application.</value>
        string ApplicationName { get; }

        /// <summary>
        /// Gets the application version.
        /// </summary>
        /// <value>The application version.</value>
        string ApplicationVersion { get; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        /// <value>The session identifier.</value>
        string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the list of supported objects.
        /// </summary>
        /// <value>The supported objects.</value>
        IList<string> SupportedObjects { get; set; }

        /// <summary>
        /// Gets or sets a delegate to process logging messages.
        /// </summary>
        /// <value>The output delegate.</value>
        Action<string> Output { get; set; }

        /// <summary>
        /// Formats the specified message for output.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The formatted message.</returns>
        string Format(string message);

        /// <summary>
        /// Formats the specified message for output.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The formatted message.</returns>
        string Format(string message, params object[] args);

        /// <summary>
        /// Called when the ETP session is opened.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        void OnSessionOpened(IList<SupportedProtocol> supportedProtocols);

        /// <summary>
        /// Called when WebSocket data is received.
        /// </summary>
        /// <param name="data">The data.</param>
        void OnDataReceived(byte[] data);

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="header">The header.</param>
        /// <param name="body">The body.</param>
        void SendMessage<T>(MessageHeader header, T body) where T : ISpecificRecord;

        /// <summary>
        /// Gets the supported protocols.
        /// </summary>
        /// <param name="isSender">if set to <c>true</c> the current session is the sender.</param>
        /// <returns>A list of supported protocols.</returns>
        IList<SupportedProtocol> GetSupportedProtocols(bool isSender = false);

        /// <summary>
        /// Gets the registered protocol handler for the specified ETP interface.
        /// </summary>
        /// <typeparam name="T">The protocol handler interface.</typeparam>
        /// <returns>The registered protocol handler instance.</returns>
        T Handler<T>() where T : IProtocolHandler;

        /// <summary>
        /// Determines whether this instance can handle the specified protocol.
        /// </summary>
        /// <typeparam name="T">The protocol handler interface.</typeparam>
        /// <returns><c>true</c> if the specified protocol handler has been registered; otherwise, <c>false</c>.</returns>
        bool CanHandle<T>() where T : IProtocolHandler;

        /// <summary>
        /// Generates a new unique message identifier for the current session.
        /// </summary>
        /// <returns>The message identifier.</returns>
        long NewMessageId();

        /// <summary>
        /// Closes the WebSocket connection for the specified reason.
        /// </summary>
        /// <param name="reason">The reason.</param>
        void Close(string reason);
    }
}
