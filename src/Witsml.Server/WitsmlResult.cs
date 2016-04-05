//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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

namespace PDS.Witsml.Server
{
    public class WitsmlResult
    {
        public WitsmlResult(ErrorCodes code) : this(code, string.Empty)
        {
        }

        public WitsmlResult(ErrorCodes code, string message)
        {
            Code = code;
            Message = message;
        }

        public ErrorCodes Code { get; private set; }

        public string Message { get; private set; }
    }

    public class WitsmlResult<T> : WitsmlResult
    {
        public WitsmlResult(ErrorCodes errorCode, T results) : this(errorCode, string.Empty, results)
        {
        }

        public WitsmlResult(ErrorCodes errorCode, string message, T results) : base(errorCode, message)
        {
            Results = results;
        }

        public T Results { get; private set; }
    }
}
