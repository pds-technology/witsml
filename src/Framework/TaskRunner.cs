//----------------------------------------------------------------------- 
// PDS.Framework, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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
    public class TaskRunner : IDisposable
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
            _tokenSource?.Cancel();
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

                OnExecute?.Invoke();

                await Task.Delay(interval, token);
            }
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // NOTE: dispose managed state (managed objects).

                    if (_tokenSource != null)
                        _tokenSource.Dispose();
                }

                // NOTE: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // NOTE: set large fields to null.

                _tokenSource = null;
                _disposedValue = true;
            }
        }

        // NOTE: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TaskRunner() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // NOTE: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
