//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
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
using System.Data;
using Caliburn.Micro;
using Energistics.Datatypes;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Studio.Core.Runtime;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Studio.Core.ViewModels
{
    /// <summary>
    /// Manages the loading of data displayed in the data grid control.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class DataGridViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DataGridViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public DataGridViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DataTable = new DataTable();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime ervice.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the data table.
        /// </summary>
        /// <value>The data table.</value>
        public DataTable DataTable { get; }

        /// <summary>
        /// Gets the URI for the current data obejct.
        /// </summary>
        /// <value>The URI.</value>
        public EtpUri Uri { get; private set; }

        /// <summary>
        /// Sets the current object.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <param name="dataObject">The data object.</param>
        /// <param name="retrievePartialResults">True if to automatically request partial results.</param>
        public void SetCurrentObject(string objectType, object dataObject, bool retrievePartialResults)
        {
            if (!ObjectTypes.IsGrowingDataObject(objectType)) return;

            try
            {
                var log131 = dataObject as Witsml131.Log;
                if (log131 != null) SetLogData(log131, retrievePartialResults);

                var log141 = dataObject as Witsml141.Log;
                if (log141 != null) SetLogData(log141, retrievePartialResults);
            }
            catch (Exception ex)
            {
                _log.WarnFormat("Error displaying growing object data: {0}", ex);
            }
        }

        /// <summary>
        /// Clears the data table if the URI has changed.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="retrievePartialResults">True if to automatically request partial results.</param>
        private void ClearDataTable(EtpUri uri, bool retrievePartialResults)
        {
            if (uri == Uri && retrievePartialResults)
                return;

            Uri = uri;
            DataTable.BeginLoadData();
            DataTable.PrimaryKey = new DataColumn[0];
            DataTable.Clear();
            DataTable.Rows.Clear();
            DataTable.Columns.Clear();
            DataTable.AcceptChanges();
            DataTable.EndLoadData();
        }

        /// <summary>
        /// Sets the log data.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="retrievePartialResults">True if to automatically request partial results.</param>
        private void SetLogData(Witsml131.Log log, bool retrievePartialResults)
        {
            ClearDataTable(log.GetUri(), retrievePartialResults);
            Runtime.InvokeAsync(() => SetChannelData(log.GetReader()));
        }

        /// <summary>
        /// Sets the log data.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="retrievePartialResults">True if to automatically request partial results.</param>
        private void SetLogData(Witsml141.Log log, bool retrievePartialResults)
        {
            ClearDataTable(log.GetUri(), retrievePartialResults);
            Runtime.InvokeAsync(() => log.GetReaders().ForEach(SetChannelData));
        }

        /// <summary>
        /// Sets the channel data.
        /// </summary>
        /// <param name="reader">The reader.</param>
        private void SetChannelData(ChannelDataReader reader)
        {
            reader.IncludeUnitWithName = true;
            DataTable.BeginLoadData();
            DataTable.Load(reader, LoadOption.Upsert);
            DataTable.PrimaryKey = new[] { DataTable.Columns[0] };
            DataTable.AcceptChanges();
            DataTable.EndLoadData();
            NotifyOfPropertyChange(() => DataTable);
        }
    }
}
