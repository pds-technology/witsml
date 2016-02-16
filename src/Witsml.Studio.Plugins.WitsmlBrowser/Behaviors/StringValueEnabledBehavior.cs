using System.Windows;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.Behaviors
{
    public class StringValueEnabledBehavior
    {
        public static readonly DependencyProperty StringValueProperty =
            DependencyProperty.RegisterAttached(
                "StringValue", 
                typeof(string), 
                typeof(StringValueEnabledBehavior), 
                new PropertyMetadata(string.Empty, OnStringValueChanged));

        public static string GetStringValue(DependencyObject obj)
        {
            return (string)obj.GetValue(StringValueProperty);
        }

        public static void SetStringValue(DependencyObject obj, string value)
        {
            obj.SetValue(StringValueProperty, value);
        }

        private static void OnStringValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as UIElement;

            if (control != null)
            {
                control.IsEnabled = e != null && !string.IsNullOrEmpty((e.NewValue ?? string.Empty) as string);
            }
        }
    }
}
