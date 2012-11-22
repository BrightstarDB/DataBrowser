using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DataBrowser.Model;
using System.Threading.Tasks;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace DataBrowser
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ResourcePage : DataBrowser.Common.LayoutAwarePage
    {
        public ResourcePage()
        {
            this.InitializeComponent();
        }

        private Resource _resource;

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            _resource = navigationParameter as Resource;
            DefaultViewModel["ResourceName"] = _resource.Title;

            // PropertyBox.Items.Add("hello there");
            var t = new Task(() =>
            {
                var a = _resource.DataProvider.GetResourceProperties(_resource);
                Task.WaitAll(a);

                HomePage.UiThreadDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, 
                    () => { LoadingMessageTextBlock.Visibility=Visibility.Collapsed;});

                foreach (var rt in a.Result)
                {
                    HomePage.UiThreadDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {                        
                        PropertyBox.Items.Add(rt);
                    });
                }
            });
            t.Start();

            LoadingMessageTextBlock.Visibility = Visibility.Visible;
            LoadingMessageTextBlock.Text = "Loading...";
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {

        }

        private void PropertyBox_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            var p = e.ClickedItem as Property;
            if (!p.IsLiteral)
            {
                // either its a collection link or an entity link
                if (p.IsCollectionProperty)
                {
                    var param = new List<object>();
                    param.Add(p.PropertyValueName);
                    param.Add(p.PropertyValue);
                    param.Add(p.DataProvider);
                    var currentFrame = Window.Current.Content as Frame;
                    currentFrame.Navigate(typeof(TypePage), param);
                }
                else
                {
                    var relatedResource = new Resource(p.DataProvider, null) { Identity = new Uri(p.PropertyValue), Title = p.PropertyValueName };
                    var currentFrame = Window.Current.Content as Frame;
                    currentFrame.Navigate(typeof(ResourcePage), relatedResource);
                }
            }
            else if (p.PropertyValue.StartsWith("http://") && Uri.IsWellFormedUriString(p.PropertyValue, UriKind.Absolute))
            {
                // clickable web resource so open up the browser

            }
        }
    }
}
