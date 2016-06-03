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

    /// <summary>
    /// Manages the execution and cancellation of asynchronous tasks.
    /// </summary>
    public class TaskRunner
    {
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;


        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRunner"/> class.
        /// </summary>
        /// <param name="interval">The interval.</param>
        public TaskRunner(int interval = 1000)
        {
            Interval = interval;
            OnExecute = Stop;
        }


        /// <summary>
        /// Gets the interval.
        /// </summary>
        /// <value>
        /// The interval.
        /// </value>
        public int Interval { get; private set; }


        /// <summary>
        /// Gets or sets the Action to perform on execute.
        /// </summary>
        /// <value>
        /// The on execute.
        /// </value>
        public Action OnExecute { get; set; }


        /// <summary>
        /// Gets or sets the Action to perform on error.
        /// </summary>
        /// <value>
        /// The on error.
        /// </value>
        public Action<Exception> OnError { get; set; }


        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning
        {
            get { return _tokenSource != null && !_token.IsCancellationRequested; }
        }


        /// <summary>
        /// Starts an asynchronous Task
        /// </summary>
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


        /// <summary>
        /// Stops this Task instance.
        /// </summary>
        public void Stop()
        {
            if (_tokenSource != null)
                _tokenSource.Cancel();
        }


        /// <summary>
        /// Starts the Task with the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="interval">The interval.</param>
        /// <returns></returns>
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
