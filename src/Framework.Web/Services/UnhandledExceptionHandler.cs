//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2017.2
//
// Copyright 2017 PDS Americas LLC
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

using System.Net;
using System.Net.Http;
using System.Security;
using System.Web.Http.Filters;
using log4net;

namespace PDS.WITSMLstudio.Framework.Web.Services
{
    /// <summary>
    /// Logs unhandled exceptions in Web API services.
    /// </summary>
    /// <seealso cref="System.Web.Http.Filters.ExceptionFilterAttribute" />
    public class UnhandledExceptionHandler : ExceptionFilterAttribute
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UnhandledExceptionHandler));

        /// <summary>
        /// Raises the exception event.
        /// </summary>
        /// <param name="actionExecutedContext">The context for the action.</param>
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception is SecurityException)
            {
                Log.Info("Authorization failed.", actionExecutedContext.Exception);

                actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent(actionExecutedContext.Exception.Message)
                };
            }
            else
            {
                Log.Error("Unhandled exception.", actionExecutedContext.Exception);
            }
        }
    }
}
