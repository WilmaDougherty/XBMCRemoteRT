﻿using XBMCRemoteRT.Common;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using XBMCRemoteRT.Helpers;
using XBMCRemoteRT.Models.Video;
using XBMCRemoteRT.RPCWrappers;
using System.Threading.Tasks;
using Windows.UI.Popups;
using XBMCRemoteRT.Models.Common;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace XBMCRemoteRT.Pages.Entry
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TVShowThinPivot : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public TVShowThinPivot()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;            
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            Frame.BackStack.Clear();
            Init(e.Parameter.ToString());
        }       

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
            ConnectionManager.ManageSystemTray(false);
        }

        #endregion

        private async void Init(string navigationArgs)
        {
            ConnectionManager.ManageSystemTray(true);
            bool isConnected = await LoadAndConnnect();
            if (!isConnected)
                return;

            await PopulatePage(navigationArgs);
            TileHelper.UpdateAllTiles();
            ConnectionManager.ManageSystemTray(false);
        }

        private async Task PopulatePage(string navigationArgs)
        {
            string showName = navigationArgs.Split("_".ToArray())[1];
            ShowNameTextBlock.Text = showName;

            var shows = await VideoLibrary.GetTVShows();
            int showId = shows.Where(show => show.Title == showName).FirstOrDefault().TvShowId;

            if (showId == null)
                return;

            Filter newFilter = new Filter { Field = "playcount", Operator = "is", value = "0" };

            Sort sort = new Sort { Method = "label", IgnoreArticle = true, Order = "ascending" };

            var newEpisodes = new EpisodesCollection(newFilter, sort, showId);
            NewEpisodesListView.ItemsSource = newEpisodes;

            Filter watchedFilter = new Filter { Field = "playcount", Operator = "greaterthan", value = "0" };

            var watchedEpisodes = new EpisodesCollection(watchedFilter, sort, showId);
            WatchedEpisodesListView.ItemsSource = watchedEpisodes;
            
            if (await VideoLibrary.GetEpisodesCount(tvShowID: showId) == 0)
            {
                string message = String.Format("No episodes were found in {0}. We will take you to the library now.", showName);
                string messageHeader = "Nothing Here!";
                MessageDialog msg = new MessageDialog(message, messageHeader);
                await msg.ShowAsync();
                Frame.Navigate(typeof(CoverPage));
            }
        }

        private async Task<bool> LoadAndConnnect()
        {
            await App.ConnectionsVM.ReloadConnections();
            string ip = (string)SettingsHelper.GetValue("RecentServerIP");
            if (ip != null)
            {
                var connectionItem = App.ConnectionsVM.ConnectionItems.FirstOrDefault(item => item.IpAddress == ip);
                if (connectionItem != null)
                {
                    if (await JSONRPC.Ping(connectionItem))
                    {
                        ConnectionManager.CurrentConnection = connectionItem;
                        return true;
                    }
                }
            }
            string message = "Could not connect to a server. Please check the connection on next screen.";
            string messageHeader = "Connection Unsuccessful";
            MessageDialog msg = new MessageDialog(message, messageHeader);
            await msg.ShowAsync();
            Frame.Navigate(typeof(MainPage), false);
            return false;
        }

        private void EpisodeWrapper_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var tappedEpisode = (sender as StackPanel).DataContext as Episode;
            Player.PlayEpisode(tappedEpisode);
            Frame.Navigate(typeof(CoverPage));
        }
    }
}
