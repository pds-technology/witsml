//----------------------------------------------------------------------- 
// PDS.Framework, 2016.1
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
using System.Threading;
using System.Threading.Tasks;

namespace PDS.Framework
{
    public class TaskRunner
    {
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;

        public TaskRunner(int interval = 1000)
        {
            Interval = interval;
            OnExecute = Stop;
        }

        public int Interval { get; private set; }

        public Action OnExecute { get; set; }

        public Action<Exception> OnError { get; set; }

        public bool IsRunning
        {
            get { return _tokenSource != null && !_token.IsCancellationRequested; }
        }

        public void Start()
        {
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;

            Task.Run(async () =>
            {
                using (_tokenSource)
                {
                    try
                    {
                        await Start(_token, Interval);
                    }
                    catch (Exception ex)
                    {
                        if (OnError != null)
                            OnError(ex);
                    }
                    finally
                    {
                        _tokenSource = null;
                    }
                }
            },
            _token);
        }

        public void Stop()
        {
            if (_tokenSource != null)
                _tokenSource.Cancel();
        }

        private async Task Start(CancellationToken token, int interval)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                if (OnExecute != null)
                    OnExecute();

                await Task.Delay(interval);
            }
        }
    }
}
