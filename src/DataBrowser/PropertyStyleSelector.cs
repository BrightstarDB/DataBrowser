using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using DataBrowser.Model;
using Windows.UI.Xaml;

namespace DataBrowser
{

    public abstract class TemplateSelector : ContentControl
    {
        public abstract DataTemplate SelectTemplate(object item, DependencyObject container);
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            ContentTemplate = SelectTemplate(newContent, this);
        }
    }

    public class PropertyStyleSelector : TemplateSelector
    {

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var property = item as Property;
            var uiElement = container as UIElement;

            if (property.IsLiteral && property.PropertyValue.StartsWith("http://") && Uri.IsWellFormedUriString(property.PropertyValue, UriKind.Absolute))
            {
                return App.Current.Resources["ClickableLiteralPropertyTemplate"] as DataTemplate;
            }
            else if (property.IsLiteral)
            {
                return App.Current.Resources["LiteralPropertyTemplate"] as DataTemplate;
            }
            else
            {
                return App.Current.Resources["RelatedResourcePropertyTemplate"] as DataTemplate;
            }

        }
    }
}
