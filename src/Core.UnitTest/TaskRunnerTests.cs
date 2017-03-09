//----------------------------------------------------------------------- 
// PDS.Witsml, 2017.1
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;

namespace PDS.Witsml
{
    /// <summary>
    /// TaskRunner tests.
    /// </summary>
    [TestClass]
    public class TaskRunnerTests
    {
        private const string Message = "Error";
        private int _counter;
        private TaskRunner _taskRunner;

        [TestInitialize]
        public void OnTestSetup()
        {
            _taskRunner = new TaskRunner()
            {
                OnExecute = RunningAction,
                OnError = HandleException
            };
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            _taskRunner.Dispose();
        }

        [TestMethod]
        public void TaskRunner_Can_Start_New_TaskRunner()
        {
            Assert.IsNotNull(_taskRunner);
            Assert.AreEqual(1000, _taskRunner.Interval);

            _taskRunner.Start();
            Assert.IsTrue(_taskRunner.IsRunning);

            // Wait for exception to be thrown
            System.Threading.Thread.Sleep(4000);
        }

        [TestMethod]
        public void TaskRunner_Can_Start_Then_Cancel_And_Dispose_New_TaskRunner()
        {
            _taskRunner = new TaskRunner()
            {
                OnExecute = RunningAction,
                OnError = HandleException
            };
            Assert.IsNotNull(_taskRunner);
            Assert.AreEqual(1000, _taskRunner.Interval);

            _taskRunner.Start();
            _taskRunner.Stop();

            Assert.IsFalse(_taskRunner.IsRunning);
        }

        private void HandleException(Exception obj)
        {
            Assert.IsNotNull(obj);
            Assert.AreEqual(Message, obj.Message);
        }

        private void RunningAction()
        {
            if (_counter > 0)
            {
                throw new Exception(Message);
            }
            _counter++;
        }
    }
}
