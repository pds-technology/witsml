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
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio
{
    [TestClass]
    public class TextFieldParserTests
    {
        private string DataDir;

        [TestInitialize]
        public void TestSetUp()
        {
            DataDir = new DirectoryInfo(@".\TestData").FullName;
        }

        [TestMethod]
        public void TextFieldParser_Can_Parser_With_Delimiter_Comma()
        {
            string testStr = "1, 2, \"testing\"\"s , comma\", 4, 5";

            using (TextReader sr = new StringReader(testStr))
            {
                using (TextFieldParser parser = new TextFieldParser(sr))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.TrimWhiteSpace = true;

                    string[] cells = parser.ReadFields();
                    Assert.AreEqual(5, cells.Length);
                    Assert.AreEqual("1", cells[0]);
                    Assert.AreEqual("2", cells[1]);
                    Assert.AreEqual("testing\"s , comma", cells[2]);
                    Assert.AreEqual("4", cells[3]);
                    Assert.AreEqual("5", cells[4]);
                }
            }
        }

        [Ignore]       
        [TestMethod]
        public void TextFieldParser_Performance_Compared_To_Split()
        {
            string[] logFiles = Directory.GetFiles(DataDir, "LargeLog_append.xml");
            Assert.IsTrue(logFiles.Length > 0);

            int textFieldParserLineCount = 0;
            long textFieldParserElapseTime;
            
            int numOfLoops = 1000;

            // Check performance of TextFieldParser
            long maxElapseTime = long.MinValue;
            int maxLoopNum = 0;
            long minElapseTime = long.MaxValue;
            int minLoopNum = 0;
            long lastElapseTime = 0;
            long intervalElapseTime = 0;

            Stopwatch stopwatch1 = Stopwatch.StartNew();
            stopwatch1.Stop();
            
            for (int i = 0; i < numOfLoops; i++)
            {
                using (StreamReader reader = new StreamReader(logFiles[0]))
                {
                    using (TextFieldParser parser = new TextFieldParser(reader))
                    {
                        parser.Delimiters = new string[] {","};
                        bool startTimer = false;
                        while (!startTimer)
                        {
                            string[] parts = parser.ReadFields();
                            if (parts[0].Equals("<logData>"))
                            {
                                startTimer = true;
                                stopwatch1.Start();
                            }
                        }
                       
                        while (true)
                        {
                            string[] parts = parser.ReadFields();
                            if (parts == null)
                            {
                                break;
                            }
                            //Console.WriteLine(string.Join(" | ", parts));
                            textFieldParserLineCount++;
                        }
                        stopwatch1.Stop();

                        intervalElapseTime = stopwatch1.ElapsedMilliseconds - lastElapseTime;
                        lastElapseTime = stopwatch1.ElapsedMilliseconds;
                        if (intervalElapseTime > maxElapseTime)
                        {
                            maxElapseTime = intervalElapseTime;
                            maxLoopNum = i;
                        }
                        else if (intervalElapseTime < minElapseTime)
                        {
                            minElapseTime = intervalElapseTime;
                            minLoopNum = i;
                        }
                    }
                }
            }
            textFieldParserElapseTime = stopwatch1.ElapsedMilliseconds;
            Console.WriteLine("TextFieldParser elapse time = {0} milliseconds", textFieldParserElapseTime);
            Console.WriteLine("Number of lines processed by TextFieldParse={0}", textFieldParserLineCount);
            Console.WriteLine("Maximum elapse time = {0} ms at loop no. {1}", maxElapseTime, maxLoopNum);
            Console.WriteLine("Minimum elapse time = {0} ms at loop no. {1}", minElapseTime, minLoopNum);

            // Check Performance of Split
            int splitLineCount = 0;
            long splitElapseTime = 0;

            long maxElapseTime2 = long.MinValue;
            int maxLoopNum2 = 0;
            long minElapseTime2 = long.MaxValue;
            int minLoopNum2 = 0;
            long lastElapseTime2 = 0;
            long intervalElapseTime2 = 0;

            Stopwatch stopwatch2 = Stopwatch.StartNew();
            for (int i = 0; i < numOfLoops; i++)
            {                
                using (StreamReader reader = new StreamReader(logFiles[0]))
                {
                    char[] delimiters = new char[] {','};
                    bool startTimer = false;
                    while (!startTimer)
                    {
                        string line = reader.ReadLine();
                        if (line.Trim().StartsWith("<logData>"))
                        {
                            startTimer = true;
                            stopwatch2.Start();
                        }
                    }
                   
                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        string[] parts = line.Split(delimiters);

                        // Console.WriteLine(string.Join(" | ", parts));
                        splitLineCount++;
                    }
                    stopwatch2.Stop();

                    intervalElapseTime2 = stopwatch2.ElapsedMilliseconds - lastElapseTime2;
                    lastElapseTime2 = stopwatch2.ElapsedMilliseconds;
                    if (intervalElapseTime2 > maxElapseTime2)
                    {
                        maxElapseTime2 = intervalElapseTime2;
                        maxLoopNum2 = i;
                    }
                    else if (intervalElapseTime2 < minElapseTime2)
                    {
                        minElapseTime2 = intervalElapseTime2;
                        minLoopNum2 = i;
                    }
                }
            }
            splitElapseTime = stopwatch2.ElapsedMilliseconds;
            Console.WriteLine("Split elapse time = {0} milliseconds", splitElapseTime);
            Console.WriteLine("Number of lines processed by Split ={0}", splitLineCount);
            Console.WriteLine("Maximum elapse time = {0} ms at loop no. {1}", maxElapseTime2, maxLoopNum2);
            Console.WriteLine("Minimum elapse time = {0} ms at loop no. {1}", minElapseTime2, minLoopNum2);
        }
    }
}
