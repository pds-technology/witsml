using System;
using Caliburn.Micro;
using Energistics.Datatypes.Object;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    public class ResourceViewModel : PropertyChangedBase
    {
        private static readonly ResourceViewModel Placeholder = new ResourceViewModel(new Resource()
        {
            Name = "loading..."
        });

        public ResourceViewModel(Resource resource)
        {
            Resource = resource;
            Children = new BindableCollection<ResourceViewModel>();

            if (resource.HasChildren != 0)
            {
                Children.Add(Placeholder);
            }
        }

        public Resource Resource { get; private set; }

        public BindableCollection<ResourceViewModel> Children { get; private set; }

        public bool HasPlaceholder
        {
            get { return Children.Count == 1 && Children[0] == Placeholder; }
        }

        public Action<string> LoadChildren { get; set; }

        private bool _isExpanded;
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
