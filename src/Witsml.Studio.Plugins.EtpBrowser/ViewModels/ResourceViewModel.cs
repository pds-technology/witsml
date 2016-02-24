using System;
using Caliburn.Micro;
using Energistics.Datatypes.Object;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Represents the user interface elements that will be displayed in the tree view.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.PropertyChangedBase" />
    public class ResourceViewModel : PropertyChangedBase
    {
        private static readonly ResourceViewModel Placeholder = new ResourceViewModel(new Resource()
        {
            Name = "loading..."
        });

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceViewModel"/> class.
        /// </summary>
        /// <param name="resource">The resource.</param>
        public ResourceViewModel(Resource resource)
        {
            Resource = resource;
            Children = new BindableCollection<ResourceViewModel>();

            if (resource.HasChildren != 0)
            {
                Children.Add(Placeholder);
            }
        }

        /// <summary>
        /// Gets the resource.
        /// </summary>
        /// <value>The resource.</value>
        public Resource Resource { get; private set; }

        /// <summary>
        /// Gets the collection of child resources.
        /// </summary>
        /// <value>The children.</value>
        public BindableCollection<ResourceViewModel> Children { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has a placeholder element.
        /// </summary>
        /// <value><c>true</c> if this instance has placeholder; otherwise, <c>false</c>.</value>
        public bool HasPlaceholder
        {
            get { return Children.Count == 1 && Children[0] == Placeholder; }
        }

        /// <summary>
        /// Gets or sets the action method used to load child resources.
        /// </summary>
        /// <value>The load children.</value>
        public Action<string> LoadChildren { get; set; }

        private bool _isExpanded;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is expanded.
        /// </summary>
        /// <value><c>true</c> if this instance is expanded; otherwise, <c>false</c>.</value>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyOfPropertyChange(() => IsExpanded);
                }

                if (HasPlaceholder && value)
                {
                    Children.Clear();
                    LoadChildren(Resource.Uri);
                }
            }
        }
    }
}
