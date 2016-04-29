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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Energistics.DataAccess;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using PDS.Framework;
using PDS.Witsml.Linq;
using PDS.Witsml.Studio.Core.Connections;
using PDS.Witsml.Studio.Core.Runtime;

namespace PDS.Witsml.Studio.Core.ViewModels
{
    /// <summary>
    /// Manages the display and interaction of the WITSML hierarchy view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Screen" />
    public class WitsmlTreeViewModel : Screen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlTreeViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        public WitsmlTreeViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            Items = new BindableCollection<ResourceViewModel>();
            DataObjects = new BindableCollection<string>();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; }

        /// <summary>
        /// Gets the TreeView items.
        /// </summary>
        /// <value>The TreeView items.</value>
        public BindableCollection<ResourceViewModel> Items { get; }

        /// <summary>
        /// Gets the collection of supported data objects.
        /// </summary>
        /// <value>The data objects.</value>
        public BindableCollection<string> DataObjects { get; } 

        private IWitsmlContext _context;

        /// <summary>
        /// Gets or sets the WITSML context.
        /// </summary>
        /// <value>The WITSML context.</value>
        public IWitsmlContext Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(() => Context);
                }
            }
        }

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectIds
        {
            get
            {
                var resource = Items.FindSelected();

                return resource != null &&
                    !string.IsNullOrWhiteSpace(new EtpUri(resource.Resource.Uri).ObjectId);
            }
        }

        /// <summary>
        /// Gets the selected item's IDs using a GetFromStore request.
        /// </summary>
        public void GetObjectIds()
        {
            var resource = Items.FindSelected();
            var uri = new EtpUri(resource.Resource.Uri);
            Context.GetObjectIdOnly(uri.ObjectType, uri);
        }

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectHeader
        {
            get
            {
                var resource = Items.FindSelected();
                if (resource == null) return false;

                var uri = new EtpUri(resource.Resource.Uri);

                return !string.IsNullOrWhiteSpace(uri.ObjectId)
                    && ObjectTypes.IsGrowingDataObject(uri.ObjectType);
            }
        }

        /// <summary>
        /// Gets the selected item's details using a GetFromStore request.
        /// </summary>
        public void GetObjectHeader()
        {
            var resource = Items.FindSelected();
            var uri = new EtpUri(resource.Resource.Uri);
            Context.GetGrowingObjectHeaderOnly(uri.ObjectType, uri);
        }

        /// <summary>
        /// Determines whether a GetFromStore request can be sent for the selected item.
        /// </summary>
        /// <returns><c>true</c> if the selected item is not a folder; otherwise, <c>false</c>.</returns>
        public bool CanGetObjectDetails
        {
            get
            {
                var resource = Items.FindSelected();

                return resource != null &&
                    !string.IsNullOrWhiteSpace(new EtpUri(resource.Resource.Uri).ObjectId);
            }
        }

        /// <summary>
        /// Gets the selected item's details using a GetFromStore request.
        /// </summary>
        public void GetObjectDetails()
        {
            var resource = Items.FindSelected();
            var uri = new EtpUri(resource.Resource.Uri);
            Context.GetObjectDetails(uri.ObjectType, uri);
        }

        /// <summary>
        /// Creates a WITSML proxy for the specified version.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="version">The WITSML version.</param>
        public void CreateContext(Connection connection, WMLSVersion version)
        {
            if (Context != null)
            {
                Context.LogQuery = null;
                Context.LogResponse = null;
            }

            Context = version == WMLSVersion.WITSML131
                ? (IWitsmlContext) new Witsml131Context(connection.Uri, connection.Username, connection.SecurePassword)
                : new Witsml141Context(connection.Uri, connection.Username, connection.SecurePassword);

            Items.Clear();
        }

        /// <summary>
        /// Called when the parent view is ready.
        /// </summary>
        public void OnViewReady(IEnumerable<string> dataObjects)
        {
            DataObjects.Clear();
            DataObjects.AddRange(dataObjects);

            if (!Items.Any())
                LoadWells();
        }

        /// <summary>
        /// Refreshes the hierarchy.
        /// </summary>
        public void RefreshHierarchy()
        {
            Items.Clear();
            LoadWells();
        }

        /// <summary>
        /// Refreshes the context menu.
        /// </summary>
        public void RefreshContextMenu()
        {
            NotifyOfPropertyChange(() => CanGetObjectIds);
            NotifyOfPropertyChange(() => CanGetObjectHeader);
            NotifyOfPropertyChange(() => CanGetObjectDetails);
            //NotifyOfPropertyChange(() => CanDeleteObject);
        }

        private void LoadWells()
        {
            Runtime.ShowBusy();

            Task.Run(async () =>
            {
                var wells = Context.GetAllWells();
                await LoadDataItems(wells, Items, LoadWellbores, x => x.GetUri());
                Runtime.ShowBusy(false);
            });
        }

        private void LoadWellbores(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(async () =>
            {
                var wellbores = Context.GetWellbores(new EtpUri(uri));
                await LoadDataItems(wellbores, parent.Children, LoadWellboreFolders, x => x.GetUri());
                Runtime.ShowBusy(false);
            });
        }

        private void LoadWellboreFolders(ResourceViewModel parent, string uri)
        {
            var etpUri = new EtpUri(uri);

            DataObjects
                .Select(x => ToResourceViewModel(etpUri.Append(x), x, LoadWellboreObjects))
                .ForEach(parent.Children.Add);
        }

        private void LoadWellboreObjects(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(async () =>
            {
                var etpUri = new EtpUri(uri);
                var dataObjects = Context.GetWellboreObjects(etpUri.ObjectType, etpUri);

                await LoadDataItems(dataObjects, parent.Children, LoadGrowingObjectChildren, x => x.GetUri(),
                    ObjectTypes.IsGrowingDataObject(etpUri.ObjectType) ? -1 : 0);

                Runtime.ShowBusy(false);
            });
        }

        private void LoadGrowingObjectChildren(ResourceViewModel parent, string uri)
        {
            Runtime.ShowBusy();

            Task.Run(async () =>
            {
                var etpUri = new EtpUri(uri);
                var dataObject = Context.GetGrowingObjectHeaderOnly(etpUri.ObjectType, etpUri);

                if (ObjectTypes.Log.EqualsIgnoreCase(etpUri.ObjectType))
                    LoadLogCurveInfo(parent.Children, dataObject);

                await Task.Yield();
                Runtime.ShowBusy(false);
            });
        }

        private void LoadLogCurveInfo(IList<ResourceViewModel> items, IWellboreObject dataObject)
        {
            var log131 = dataObject as Witsml131.Log;
            var log141 = dataObject as Witsml141.Log;

            log131?.LogCurveInfo
                .Select(x => ToResourceViewModel(x.GetUri(log131), x.Mnemonic, null, 0))
                .ForEach(items.Add);

            log141?.LogCurveInfo
                .Select(x => ToResourceViewModel(x.GetUri(log141), x.Mnemonic.Value, null, 0))
                .ForEach(items.Add);
        }

        private async Task LoadDataItems<T>(
            IEnumerable<T> dataObjects,
            IList<ResourceViewModel> items,
            Action<ResourceViewModel, string> action,
            Func<T, EtpUri> getUri,
            int children = -1)
            where T : IDataObject
        {
            await Runtime.InvokeAsync(() =>
            {
                dataObjects
                    .Select(x => ToResourceViewModel(x, action, getUri, children))
                    .ForEach(items.Add);
            });
        }

        private ResourceViewModel ToResourceViewModel<T>(T dataObject, Action<ResourceViewModel, string> action, Func<T, EtpUri> getUri, int children = -1) where T : IDataObject
        {
            return ToResourceViewModel(getUri(dataObject), dataObject.Name, action, children);
        }

        private ResourceViewModel ToResourceViewModel(EtpUri uri, string name, Action<ResourceViewModel, string> action, int children = -1)
        {
            var resource = new Resource()
            {
                Uri = uri,
                Name = name,
                ContentType = uri.ContentType,
                HasChildren = children
            };

            var viewModel = new ResourceViewModel(resource);

            if (children != 0 && action != null)
                viewModel.LoadChildren = x => action(viewModel, x);

            return viewModel;
        }
    }
}
