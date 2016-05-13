//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
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
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Web.Services.Protocols;
using System.Windows;
using PDS.Framework;

namespace PDS.Witsml.Studio.Core.Providers
{
    public class LoggingSoapExtension : SoapExtension
    {
        private Stream _oldStream;
        private MemoryStream _newStream;

        [ImportMany]
        public List<ISoapMessageHandler> Handlers { get; set; }

        public override Stream ChainStream(Stream stream)
        {
            _oldStream = stream;
            _newStream = new MemoryStream();
            return _newStream;
        }

        public override object GetInitializer(Type webServiceType)
        {
            return Application.Current.Resources["bootstrapper"];
        }

        public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)
        {
            return Application.Current.Resources["bootstrapper"];
        }

        public override void Initialize(object initializer)
        {
            GetContainer(initializer).BuildUp(this);
        }

        public override void ProcessMessage(SoapMessage message)
        {
            switch (message.Stage)
            {
                case SoapMessageStage.BeforeSerialize:
                    break;
                case SoapMessageStage.AfterSerialize:
                    WriteOutput(message);
                    break;
                case SoapMessageStage.BeforeDeserialize:
                    WriteInput(message);
                    break;
                case SoapMessageStage.AfterDeserialize:
                    break;
                default:
                    throw new InvalidOperationException("Invalid SOAP message stage: " + message.Stage);
            }
        }

        private void WriteOutput(SoapMessage message)
        {
            var xml = Encoding.UTF8.GetString(_newStream.GetBuffer());
            var action = message.Action;

            _newStream.Position = 0;
            Copy(_newStream, _oldStream);

            Handlers.ForEach(x => x.LogRequest(action, xml));
        }

        private void WriteInput(SoapMessage message)
        {
            Copy(_oldStream, _newStream);
            _newStream.Position = 0;

            var xml = Encoding.UTF8.GetString(_newStream.GetBuffer());
            var action = message.Action;

            Handlers.ForEach(x => x.LogResponse(action, xml));
        }

        private void Copy(Stream from, Stream to)
        {
            var reader = new StreamReader(from);
            var writer = new StreamWriter(to);
            writer.WriteLine(reader.ReadToEnd());
            writer.Flush();
        }

        private IContainer GetContainer(dynamic bootstrapper)
        {
            return bootstrapper.Container;
        }
    }
}
