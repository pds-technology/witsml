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
        public void OnViewReady()
        {
            if (!Items.Any())
                LoadWells();
        }

        private void LoadWells()
        {
            Task.Run(async () =>
            {
                var wells = Context.GetAllWells();
                await LoadDataItems(wells, Items, LoadWellbores);
            });
        }

        private void LoadWellbores(ResourceViewModel parent, string uri)
        {
            Task.Run(async () =>
            {
                var wellbores = Context.GetWellbores(uri);
                await LoadDataItems(wellbores, parent.Children, LoadWellboreFolders);
            });
        }

        private void LoadWellboreFolders(ResourceViewModel parent, string uri)
        {
        }

        private async Task LoadDataItems<T>(
            IEnumerable<T> dataObjects,
            BindableCollection<ResourceViewModel> items,
            Action<ResourceViewModel, string> action,
            int children = -1)
            where T : IDataObject
        {
            await Runtime.InvokeAsync(() =>
            {
                dataObjects
                    .Select(x => ToResourceViewModel(x, action, children))
                    .ForEach(items.Add);
            });
        }

        private ResourceViewModel ToResourceViewModel<T>(T dataObject, Action<ResourceViewModel, string> action, int children = -1) where T : IDataObject
        {
            var uri = dataObject.GetUri();

            var resource = new Resource()
            {
                Uri = uri,
                Name = dataObject.Name,
                ContentType = uri.ContentType,
                HasChildren = children
            };

            var viewModel = new ResourceViewModel(resource);
            viewModel.LoadChildren = x => action(viewModel, x);

            return viewModel;
        }
    }
}
