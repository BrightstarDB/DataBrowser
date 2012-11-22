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
using Windows.UI.Core;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace DataBrowser
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class HomePage : DataBrowser.Common.LayoutAwarePage
    {
        public HomePage()
        {
            this.InitializeComponent();
        }


        public static CoreDispatcher UiThreadDispatcher;

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
            UiThreadDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            DefaultViewModel["Items"] = Context.Contexts;
        }

        private void Context_ItemClick(object sender, ItemClickEventArgs e)
        {
            // navigate to the context page
            var currentFrame = Window.Current.Content as Frame;
            currentFrame.Navigate(typeof(ContextPage), e.ClickedItem);
        }

        private void AddNewContextClick(object sender, RoutedEventArgs e)
        {
            var currentFrame = Window.Current.Content as Frame;
            currentFrame.Navigate(typeof(CreateContextPage));
        }
    }
}
