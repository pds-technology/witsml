namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defines a method that can be used to validate WITSML data objects.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    public interface IDataObjectValidator<T>
    {
        /// <summary>
        /// Gets the data object being validated.
        /// </summary>
        /// <value>The data object.</value>
        T DataObject { get; }

        /// <summary>
        /// Gets the WITSML API method being executed.
        /// </summary>
        /// <value>The method being executed.</value>
        Functions Function { get; }

        /// <summary>
        /// Validates the specified data object while executing a WITSML API method.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="dataObject">The data object.</param>
        void Validate(Functions function, T dataObject);
    }
}
