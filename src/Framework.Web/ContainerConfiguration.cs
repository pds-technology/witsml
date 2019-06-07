//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
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
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace PDS.WITSMLstudio.Framework.Web
{
    /// <summary>
    /// Provides helper methods for configuring dependency injection for web applications.
    /// </summary>
    public static class ContainerConfiguration
    {
        private static string _workingDirectory;

        /// <summary>
        /// Registers a dependency resolver using the specified assembly path.
        /// </summary>
        /// <param name="assemblyPath">The assembly path.</param>
        public static void Register(string assemblyPath)
        {
            _workingDirectory = string.IsNullOrWhiteSpace(assemblyPath) || assemblyPath == "."
                ? Environment.CurrentDirectory
                : assemblyPath;

            var container = ContainerFactory.Create(assemblyPath);
            var resolver = new DependencyResolver(container);

            // Install dependency resolver for MVC
            System.Web.Mvc.DependencyResolver.SetResolver(resolver);

            // Install dependency resolver for Web API
            GlobalConfiguration.Configuration.DependencyResolver = resolver;

            // Install custom Web API controller factory
            ControllerBuilder.Current.SetControllerFactory(new ControllerFactory(container));
        }

        /// <summary>
        /// Maps the specified path relative to the current working directory.
        /// </summary>
        /// <param name="path">The relative path.</param>
        /// <returns>The absolute path.</returns>
        public static string MapWorkingDirectory(string path)
        {
            if (HttpContext.Current == null)
                return Path.Combine(_workingDirectory, path);

            return HttpContext.Current.Server.MapPath(Path.Combine("~/bin", path));
        }
    }
}
