using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;

namespace PDS.Witsml.Studio.Behaviors
{
    public class DropDownButtonBehavior : Behavior<Button>
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DropDownButtonBehavior));

        private long _attachedCount;
        private bool _isContextMenuOpen;

        protected override void OnAttached()
        {
            _log.DebugFormat("Attached to '{0}' button.", AssociatedObject.Name);

            base.OnAttached();
            AssociatedObject.AddHandler(Button.ClickEvent, new RoutedEventHandler(AssociatedObject_Click), true);
        }

        void AssociatedObject_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button source = sender as Button;
            if (source != null && source.ContextMenu != null)
            {
                // Only open the ContextMenu when it is not already open. If it is already open,
                // when the button is pressed the ContextMenu will lose focus and automatically close.
                if (!_isContextMenuOpen)
                {
                    source.ContextMenu.AddHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed), true);
                    Interlocked.Increment(ref _attachedCount);
                    // If there is a drop-down assigned to this button, then position and display it 
                    source.ContextMenu.PlacementTarget = source;
                    source.ContextMenu.Placement = PlacementMode.Bottom;
                    source.ContextMenu.IsOpen = true;
                    _isContextMenuOpen = true;
                }
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(Button.ClickEvent, new RoutedEventHandler(AssociatedObject_Click));
        }

        void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            _isContextMenuOpen = false;
            var contextMenu = sender as ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.RemoveHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed));
                Interlocked.Decrement(ref _attachedCount);
            }
        }
    }
}