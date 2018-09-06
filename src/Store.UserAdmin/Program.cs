//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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
using System.IO;
using CommandLine;
using PDS.WITSMLstudio.Framework.Web;

namespace PDS.WITSMLstudio.Store.UserAdmin
{
    public static class Program
    {
        private const int Success = 0;
        private const int Error = 1;

        public static void Main(string[] args)
        {
            ContainerConfiguration.Register(".");
            Console.WriteLine();

            var writer = new StringWriter();
            new Parser(with =>
                {
                    with.EnableDashDash = true;
                    with.HelpWriter = writer;
                })
                .ParseArguments<AddOptions, RemoveOptions>(args)
                .MapResult(
                    (AddOptions opts) => AddUser(opts),
                    (RemoveOptions opts) => RemoveUser(opts),
                    errors =>
                    {
                        Console.WriteLine(writer);
                        return Error;
                    });
        }

        public static int AddUser(AddOptions opts)
        {
            return string.IsNullOrEmpty(UserAdmin.AddUser(opts)) ? Success : Error;
        }

        public static int RemoveUser(RemoveOptions opts)
        {
            return string.IsNullOrEmpty(UserAdmin.RemoveUser(opts)) ? Success : Error;
        }
    }
}
