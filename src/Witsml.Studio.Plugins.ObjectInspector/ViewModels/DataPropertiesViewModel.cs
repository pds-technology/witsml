using System;
using System.Collections.Generic;
using Caliburn.Micro;
using PDS.Framework;
using PDS.Witsml.Studio.Runtime;
using Witsml.Studio.Plugins.ObjectInspector.Models;

namespace Witsml.Studio.Plugins.ObjectInspector.ViewModels
{
    /// <summary>
    /// Manages the UI behavior for the data properties of an Energistics Data Object.
    /// </summary>
    /// <seealso cref="Screen" />
    public sealed class DataPropertiesViewModel : Screen
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DataPropertiesViewModel));

        private DataObject _dataObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPropertiesViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <exception cref="ArgumentNullException"><paramref name="runtime"/> is null.</exception>
        public DataPropertiesViewModel(IRuntimeService runtime)
        {
            runtime.NotNull(nameof(runtime));

            Log.Debug("Creating view model instance");
            Runtime = runtime;
        }

        /// <summary>
        /// Gets or sets the family version of the objects to display.
        /// </summary>
        public DataObject DataObject
        {
            get {  return _dataObject; }
            set
            {
                if (_dataObject == value) return;

                _dataObject = value;

                Refresh();
            }
        }

        /// <summary>
        /// All (nested) data properties of the Energistics Data Object
        /// </summary>
        public IReadOnlyCollection<DataProperty> DataProperties => DataObject?.NestedDataProperties;
         
        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; }
    }
}
