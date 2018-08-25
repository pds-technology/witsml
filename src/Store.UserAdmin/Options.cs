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

using CommandLine;

namespace PDS.WITSMLstudio.Store.UserAdmin
{
    [Verb("add")]
    public class AddOptions
    {
        [Option('u', "user", Required = true, HelpText = "The user name to create.")]
        public string Username { get; set; }

        [Option('p', "password", HelpText = "The optional password.")]
        public string Password { get; set; }

        [Option('e', "email", HelpText = "The optional email address.")]
        public string Email { get; set; }
    }

    [Verb("remove")]
    public class RemoveOptions
    {
        [Option('u', "user", Required = true, HelpText = "The user name to remove.")]
        public string Username { get; set; }
    }
}
