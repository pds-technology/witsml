namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.Models
{
    /// <summary>
    /// Encapsulates the input and output parameters passed to the WITSML Store API methods.
    /// </summary>
    public struct WitsmlResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlResult" /> struct.
        /// </summary>
        /// <param name="xmlIn">The XML in.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="capClientIn">The cap client in.</param>
        /// <param name="xmlOut">The XML out.</param>
        /// <param name="messageOut">The message out.</param>
        /// <param name="returnCode">The return code.</param>
        public WitsmlResult(string xmlIn, string optionsIn, string capClientIn, string xmlOut, string messageOut, short returnCode)
        {
            XmlIn = xmlIn;
            OptionsIn = optionsIn;
            CapClientIn = capClientIn;
            XmlOut = xmlOut;
            MessageOut = messageOut;
            ReturnCode = returnCode;
        }

        /// <summary>
        /// Gets the XML in.
        /// </summary>
        /// <valueThe XML in.</value>
        public string XmlIn { get; private set; }

        /// <summary>
        /// Gets the options in.
        /// </summary>
        /// <value>The options in.</value>
        public string OptionsIn { get; private set; }

        /// <summary>
        /// Gets the cap client in.
        /// </summary>
        /// <value>The cap client in.</value>
        public string CapClientIn { get; private set; }

        /// <summary>
        /// Gets the XML out.
        /// </summary>
        /// <valueThe XML out.</value>
        public string XmlOut { get; private set; }

        /// <summary>
        /// Gets the message out.
        /// </summary>
        /// <value>The message outvalue>
        public string MessageOut { get; private set; }

        /// <summary>
        /// Gets the return code.
        /// </summary>
        /// <value>The return code.</value>
        public short ReturnCode { get; private set; }
    }
}
