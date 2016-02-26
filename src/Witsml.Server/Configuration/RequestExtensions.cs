namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Provides extension methods that can be used to process WITSML Store API method input paramters.
    /// </summary>
    public static class RequestExtensions
    {
        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The GetFromStore request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_GetFromStoreRequest request)
        {
            return new RequestContext(
                function: Functions.GetFromStore,
                objectType: request.WMLtypeIn,
                xml: request.QueryIn,
                options: request.OptionsIn,
                capabilities: request.CapabilitiesIn);
        }

        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The AddToStore request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_AddToStoreRequest request)
        {
            return new RequestContext(
                function: Functions.AddToStore,
                objectType: request.WMLtypeIn,
                xml: request.XMLin,
                options: request.OptionsIn,
                capabilities: request.CapabilitiesIn);
        }

        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The UpdateInStore request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_UpdateInStoreRequest request)
        {
            return new RequestContext(
                function: Functions.UpdateInStore,
                objectType: request.WMLtypeIn,
                xml: request.XMLin,
                options: request.OptionsIn,
                capabilities: request.CapabilitiesIn);
        }

        /// <summary>
        /// Converts a specific request object into a common structure.
        /// </summary>
        /// <param name="request">The DeleteFromStore request object.</param>
        /// <returns>The request context instance.</returns>
        public static RequestContext ToContext(this WMLS_DeleteFromStoreRequest request)
        {
            return new RequestContext(
                function: Functions.DeleteFromStore,
                objectType: request.WMLtypeIn,
                xml: request.QueryIn,
                options: request.OptionsIn,
                capabilities: request.CapabilitiesIn);
        }
    }
}
