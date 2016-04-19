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

using Avro.Specific;

namespace Energistics.Common
{
    /// <summary>
    /// Represents the method that will handle a protocol handler event.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ProtocolEventArgs{T}"/> instance containing the event data.</param>
    public delegate void ProtocolEventHandler<T>(object sender, ProtocolEventArgs<T> e) where T : ISpecificRecord;

    /// <summary>
    /// Represents the method that will handle a protocol handler event.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ProtocolEventArgs{T, V}"/> instance containing the event data.</param>
    public delegate void ProtocolEventHandler<T, TContext>(object sender, ProtocolEventArgs<T, TContext> e) where T : ISpecificRecord;
}
