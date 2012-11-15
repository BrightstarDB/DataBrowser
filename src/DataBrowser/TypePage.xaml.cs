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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DataBrowser.Providers;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace DataBrowser
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class TypePage : DataBrowser.Common.LayoutAwarePage
    {
        public TypePage()
        {
            this.InitializeComponent();
        }

        private ResourceType _resourceType;

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
            _resourceType = navigationParameter as ResourceType;

            if (_resourceType == null)
            {
                var param = navigationParameter as List<object>;

                _resourceType = new ResourceType(param[2] as IProvider);
                _resourceType.Title = param[0] as string;
                DefaultViewModel["ResourceTypeTitle"] = _resourceType.Title;
                DefaultViewModel["Instances"] = new ObservableCollection<Resource>();
                DefaultViewModel["FilteredInstances"] = new ObservableCollection<Resource>();
                LoadQueriedState(new Uri(param[1] as string), _resourceType);
            }
            else
            {
                // load instances
                DefaultViewModel["ResourceTypeTitle"] = _resourceType.Title;
                DefaultViewModel["Instances"] = new ObservableCollection<Resource>();
                DefaultViewModel["FilteredInstances"] = new ObservableCollection<Resource>();
                var ctx = new object[] { DefaultViewModel["Instances"], DefaultViewModel["FilteredInstances"] };

                var t = new Task((c) =>
                {
                    var context = c as object[];
                    var a = _resourceType.DataProvider.GetResources(_resourceType);
                    Task.WaitAll(a);
                    var b = a.Result;
                    HomePage.UiThreadDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LoadingMessageTextBlock.Visibility = Visibility.Collapsed;
                        foreach (var rt in a.Result)
                        {
                            (context[0] as ObservableCollection<Resource>).Add(rt);
                            (context[1] as ObservableCollection<Resource>).Add(rt);
                        }
                    });

                }, ctx);
                t.Start();

                LoadingMessageTextBlock.Visibility = Visibility.Visible;
                LoadingMessageTextBlock.Text = "Loading...";
            }
        }

        private void itemGridView_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            var currentFrame = Window.Current.Content as Frame;

            // check if they clicked the next resource
            var resource = e.ClickedItem as Resource;
            if (resource.Type != null && resource.Type.Identity != null && resource.Type.Identity.AbsoluteUri.Equals("http://www.brightstardb.com/databrowser/core/next"))
            {                
                LoadQueriedState(resource.Identity, _resourceType);
            }
            else
            {
                currentFrame.Navigate(typeof(ResourcePage), e.ClickedItem);
            }
        }

        private void LoadQueriedState(Uri query, ResourceType type)
        {
            ((ObservableCollection<Resource>)DefaultViewModel["Instances"]).Clear();
            ((ObservableCollection<Resource>)DefaultViewModel["FilteredInstances"]).Clear();

            var ctx = new object[] { DefaultViewModel["Instances"], DefaultViewModel["FilteredInstances"] };

            var t = new Task((c) =>
            {
                var context = c as object[];
                var a = _resourceType.DataProvider.GetResources(query, _resourceType);
                Task.WaitAll(a);
                var b = a.Result;
                HomePage.UiThreadDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    LoadingMessageTextBlock.Visibility = Visibility.Collapsed;
                    foreach (var rt in a.Result)
                    {
                        (context[0] as ObservableCollection<Resource>).Add(rt);
                        (context[1] as ObservableCollection<Resource>).Add(rt);
                    }
                });

            }, ctx);
            t.Start();

            LoadingMessageTextBlock.Visibility = Visibility.Visible;
            LoadingMessageTextBlock.Text = "Loading...";
        }

        private void SearchBox_KeyUp_1(object sender, KeyRoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ObservableCollection<Resource> resources = DefaultViewModel["Instances"] as ObservableCollection<Resource>;
                ObservableCollection<Resource> filteredResources = DefaultViewModel["FilteredInstances"] as ObservableCollection<Resource>;
                filteredResources.Clear();
                var filter = tb.Text.ToLower();
                foreach (var r in resources)
                {
                    if (r.Title.ToLower().Contains(filter))
                    {
                        filteredResources.Add(r);
                    }
                }
            }
        }
    }
}
