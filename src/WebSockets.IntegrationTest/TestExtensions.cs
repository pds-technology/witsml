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
using System.Threading;
using System.Threading.Tasks;

namespace Energistics
{
    public static class TestExtensions
    {
        public static async Task<bool> OpenAsync(this EtpClient client)
        {
            var task = new Task<bool>(() => true);

            client.SocketOpened += (s, e) =>
            {
                task.Start();
            };

            client.Open();

            return await task.WaitAsync();
        }

        public static async Task<TResult> WaitAsync<TResult>(this Task<TResult> task, int milliseconds = 5000)
        {
            return await task.WaitAsync(TimeSpan.FromMilliseconds(milliseconds));
        }

        public static async Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var tokenSource = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, tokenSource.Token));

            if (completedTask == task)
            {
                tokenSource.Cancel();
                return await task;
            }

            throw new TimeoutException("The operation has timed out.");
        }
    }
}
