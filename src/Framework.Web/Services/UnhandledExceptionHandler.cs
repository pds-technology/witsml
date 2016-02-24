using System.Net;
using System.Net.Http;
using System.Security;
using System.Web.Http.Filters;
using log4net;

namespace PDS.Framework.Web.Services
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
