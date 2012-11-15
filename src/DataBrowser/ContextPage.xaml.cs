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
using System.Collections.ObjectModel;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace DataBrowser
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class ContextPage : DataBrowser.Common.LayoutAwarePage
    {

        private Context _context;

        public string ContextTitle
        {
            get
            {
                return _context.Title;
            }
        }

        public ContextPage()
        {
            this.InitializeComponent();
        }

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
            _context = navigationParameter as Context;
            DefaultViewModel["ContextTitle"] = _context.Title;
            DefaultViewModel["Types"] = new ObservableCollection<ResourceType>();
            var collection = DefaultViewModel["Types"] as ObservableCollection<ResourceType>;
            var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

            var t = new Task((c) => {
                var a = _context.DataProvider.GetResourceTypes();
                if (a != null)
                {
                    Task.WaitAll(a);
                    var b = a.Result;
                    var coll = c as ObservableCollection<ResourceType>;
                    foreach (var rt in a.Result)
                    {
                        dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            coll.Add(rt);
                        });
                    }
                }
            }, collection);
            t.Start();

            // var resourceTypes = await _context.GetTypesAsync();
        }

        private void ResourceType_Click(object sender, ItemClickEventArgs e)
        {
            var currentFrame = Window.Current.Content as Frame;
            currentFrame.Navigate(typeof(TypePage), e.ClickedItem);
        }
    }
}
