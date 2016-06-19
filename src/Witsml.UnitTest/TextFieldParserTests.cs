//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml
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
       
        [TestMethod]
        public void TextFieldParser_Performance_Compared_To_Split()
        {
            string[] logFiles = Directory.GetFiles(DataDir, "LargeLog_append.xml");
            Assert.IsTrue(logFiles.Length > 0);

            int textFieldParserLineCount = 0;
            long textFieldParserElapseTime;
            int splitLineCount = 0;
            long splitElapseTime;

            // Check performance of TextFieldParser
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
                        }
                    }

                    Stopwatch stopwatch = Stopwatch.StartNew();
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
                    stopwatch.Stop();
                    textFieldParserElapseTime = stopwatch.ElapsedMilliseconds;                   
                }
            }

            Console.WriteLine("TextFieldParser elapse time = {0}", textFieldParserElapseTime);
            Console.WriteLine("Number of lines processed by TextFieldParse={0}", textFieldParserLineCount);

            // Check Performance of Split
            using (StreamReader reader = new StreamReader(logFiles[0]))
            {
                char[] delimiters = new char[] { ',' };
                bool startTimer = false;
                while (!startTimer)
                {
                    string line = reader.ReadLine();
                    if (line.Trim().StartsWith("<logData>"))
                        startTimer = true;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
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
                stopwatch.Stop();
                splitElapseTime = stopwatch.ElapsedMilliseconds;                
            }
          
            Console.WriteLine("Split elapse time = {0}", splitElapseTime);
            Console.WriteLine("Number of lines processed by Split ={0}", splitLineCount);
        }
    }
}
