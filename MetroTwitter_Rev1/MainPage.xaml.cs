using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.System;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Twitter;

// 基本ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234237 を参照してください

namespace MetroTwitter_Rev1
{
    /// <summary>
    /// 多くのアプリケーションに共通の特性を指定する基本ページ。
    /// </summary>
    public sealed partial class MainPage : MetroTwitter_Rev1.Common.LayoutAwarePage
    {
        const string CONSUMER_KEY = "9joiIRntsMA5i09DZNcw";
        const string CONSUMER_SECRET = "xhTnZmu3BGb8RYkjRvcfgL9B071V4caaLEl1ZEYoRWU";

        public Twitter twitter;
        public Dictionary<string, string> commandOptions = new Dictionary<string, string>();
        public List<string> inReplyUsers = new List<string>();

        public bool isCtrlKeyPressed;
        public bool isItemClicked;
        public bool isStatusSubmitting;

        public MainPage()
        {
            this.InitializeComponent();
        }


        void mentionsStatusList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifiToast(Twitter.mentionsStatusList.First());
            //throw new NotImplementedException();
        }


        /// <summary>
        /// このページには、移動中に渡されるコンテンツを設定します。前のセッションからページを
        /// 再作成する場合は、保存状態も指定されます。
        /// </summary>
        /// <param name="navigationParameter">このページが最初に要求されたときに
        /// <see cref="Frame.Navigate(Type, Object)"/> に渡されたパラメーター値。
        /// </param>
        /// <param name="pageState">前のセッションでこのページによって保存された状態の
        /// ディクショナリ。ページに初めてアクセスするとき、状態は null に
        /// なります。</param>
        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            var userData = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (pageState == null)
            {
                twitter = new Twitter(CONSUMER_KEY, CONSUMER_SECRET);

                Twitter.AccessToken = userData.Values["AccessToken"].ToString();
                Twitter.AccessTokenSecret = userData.Values["AccessTokenSecret"].ToString();
                Twitter.UserId = userData.Values["UserID"].ToString();
                Twitter.ScreenName = userData.Values["ScreenName"].ToString();

                GoToHomeTimeline_Button.IsEnabled = false;
                GoToMentions_Button.IsEnabled = false;
                GotoDirectMessage_Button.IsEnabled = false;
                Reply_Button.Visibility = Visibility.Collapsed;
                LoadingProgressBar.Visibility = Visibility.Visible;

                await twitter.GetHomeTimelineAsync();
                await twitter.GetMentionsAsync();
                await twitter.GetDirectMessageAsync();

                HomeTimelineList.ItemsSource = Twitter.homeTimelineStatusList;
                MentionsList.ItemsSource = Twitter.mentionsStatusList;
                DMList.ItemsSource = Twitter.directMessageList;
                TwitterBird.DataContext = twitter;

                LoadingProgressBar.Visibility = Visibility.Collapsed;

                GoToHomeTimeline_Button.IsEnabled = true;
                GoToMentions_Button.IsEnabled = true;
                GotoDirectMessage_Button.IsEnabled = true;   
                
                twitter.GetTimelineStreamAsync();     
                Twitter.mentionsStatusList.CollectionChanged += mentionsStatusList_CollectionChanged;
            }
        }


        /// <summary>
        /// アプリケーションが中断される場合、またはページがナビゲーション キャッシュから破棄される場合、
        /// このページに関連付けられた状態を保存します。値は、
        /// <see cref="SuspensionManager.SessionState"/> のシリアル化の要件に準拠する必要があります。
        /// </summary>
        /// <param name="pageState">シリアル化可能な状態で作成される空のディクショナリ。</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            backButton.IsEnabled = false;
        }


        /// <summary>
        /// つぶやきボタンを押した時の処理
        /// 通常のつぶやきを行うためにポップアップを開きます。
        /// </summary>
        private void Tweet_Button_Click(object sender, RoutedEventArgs e)
        {
            TweetPopup.IsOpen = true;
            Status_TextBox.Focus(Windows.UI.Xaml.FocusState.Programmatic);
            Status_TextBox.Text = "";
            commandOptions.Clear();
        }


        /// <summary>
        /// 返信ボタンを押した時の処理
        /// 選択されたツイートに対してリプライを送信するためのポップアップを開きます。
        /// </summary>
        private void Reply_Button_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder DefaultStrings = new StringBuilder();
            foreach (var RepUser in inReplyUsers)
            {
                DefaultStrings.Append(RepUser + " "); 
            }

            TweetPopup.IsOpen = true;
            Status_TextBox.Focus(Windows.UI.Xaml.FocusState.Programmatic);
            Status_TextBox.Text = DefaultStrings.ToString();
            Status_TextBox.Select(Status_TextBox.Text.Length, 0);
        }


        /// <summary>
        /// 送信ボタンを押した時の処理です。
        /// </summary>
        private async void Submit_Button_Click(object sender, RoutedEventArgs e)
        {
            await Status_Submit();
        }


        /// <summary>
        /// ステータス送信処理を行います。
        /// Ctrl+Enterで投稿するとなぜか403を返します
        /// </summary>
        /// <returns></returns>
        private async Task Status_Submit()
        {
            isStatusSubmitting = true;
            Submitting_Ring.IsActive = true;

            try
            {
                if (commandOptions != null)
                    await twitter.Tweet(Status_TextBox.Text, commandOptions);
                else
                    await twitter.Tweet(Status_TextBox.Text);
            }

            catch (Exception)
            {
                throw;
            }

            Submitting_Ring.IsActive = false;
            TweetPopup.IsOpen = false;
            BottomAppBar.IsOpen = false;
            isStatusSubmitting = false;
            HomeTimelineList.SelectedItem = null;
            MentionsList.SelectedItem = null;
        }


        /// <summary>
        /// 更新ボタンが押された時の処理です。
        /// </summary>
        private async void Refresh_Button_Click(object sender, RoutedEventArgs e)
        {
            Refresh_Button.IsEnabled = false;
            LoadingProgressBar.Visibility = Visibility.Visible;

            await twitter.GetNewHomeTimelineAsync();
            await twitter.GetNewMentionsAsync();
            await twitter.GetNewDirectMessageAsync();

            Refresh_Button.IsEnabled = true;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            BottomAppBar.IsOpen = false;
        }


        /// <summary>
        /// DispatcherTimerによって呼び出されるイベントです。
        /// TLの新着取得を行います。
        /// </summary>
        private async void timer_Tick(object sender, object e)
        {
            /*
            Refresh_Button.IsEnabled = false;
            LoadingProgressBar.Visibility = Visibility.Visible;

            await twitter.GetNewHomeTimelineAsync();

            Refresh_Button.IsEnabled = true;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            */
        }


        /// <summary>
        /// 
        /// </summary>
        private void HomeTimelineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            commandOptions.Clear();
            inReplyUsers.Clear();

            if (HomeTimelineList.SelectedItems.Count > 0)
            {
                if (HomeTimelineList.SelectedItems.Count == 1)
                    Favorite_Button.Visibility = Visibility.Visible;

                else
                    Favorite_Button.Visibility = Visibility.Collapsed;

                ObservableCollection<Status> selectedStatuses = new ObservableCollection<Status>();
                foreach (var item in HomeTimelineList.SelectedItems)
                {
                    selectedStatuses.Add(item as Status);
                }

                Status selectedStatus = HomeTimelineList.SelectedItems.First() as Status;

                if (selectedStatus != null)
                {
                    commandOptions.Add("in_reply_to_status_id", selectedStatus.id_str);
                    //commandOptions.Add("in_reply_to_screenname", selectedStatus.user.screen_name.Remove(0, 1));

                    foreach (var item in selectedStatuses)
                    {
                        if (!inReplyUsers.Contains(item.user.screen_name))
                            inReplyUsers.Add(item.user.screen_name);
                    }
                }

                Tweet_Button.Visibility = Visibility.Collapsed;
                Reply_Button.Visibility = Visibility.Visible;
                BottomTweetBar.IsOpen = true;
            }

            else
            {
                Tweet_Button.Visibility = Visibility.Visible;
                Reply_Button.Visibility = Visibility.Collapsed;
                BottomTweetBar.IsOpen = false;
            }
        }



        private void MentionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            commandOptions.Clear();
            inReplyUsers.Clear();

            if (HomeTimelineList.SelectedItems.Count == 1)
                Favorite_Button.Visibility = Visibility.Visible;

            else
                Favorite_Button.Visibility = Visibility.Collapsed;

            if (MentionsList.SelectedItems.Count > 0)
            {
                Tweet_Button.Visibility = Visibility.Collapsed;
                Reply_Button.Visibility = Visibility.Visible;
                BottomTweetBar.IsOpen = true;

                ObservableCollection<Status> selectedMentions = new ObservableCollection<Status>();
                foreach (var item in MentionsList.SelectedItems)
                {
                    selectedMentions.Add(item as Status);
                }

                Status selectedStatus = MentionsList.SelectedItems.First() as Status;

                if (selectedStatus != null)
                {
                    commandOptions.Add("in_reply_to_status_id", selectedStatus.id_str);
                    //commandOptions.Add("in_reply_to_screenname", selectedStatus.user.screen_name.Remove(0, 1));

                    foreach (var item in selectedMentions)
                    {
                        if (!inReplyUsers.Contains(item.user.screen_name))
                            inReplyUsers.Add(item.user.screen_name);
                    }
                }
            }

            else
            {
                Tweet_Button.Visibility = Visibility.Visible;
                Reply_Button.Visibility = Visibility.Collapsed;
                BottomTweetBar.IsOpen = false;
            }
        }


        private void Popup_Close_Button_Click(object sender, RoutedEventArgs e)
        {
            TweetPopup.IsOpen = false;
            Status_TextBox.Text = "";
        }

        private void DMList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


        private void BottomTweetBar_Closed(object sender, object e)
        {
            TweetPopup.IsOpen = false;
        }


        private async void Status_TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Control)
            {
                isCtrlKeyPressed = true;
                Status_TextBox.AcceptsReturn = false;
            }

            else if (isCtrlKeyPressed)
            {
                if (e.Key == VirtualKey.Enter)
                {
                    if (!isStatusSubmitting)
                    {
                        try
                        {
                            await Status_Submit();
                        }
                        catch (Exception)
                        {                            
                            throw;
                        }                         
                    }
                    Status_TextBox.AcceptsReturn = true;
                }
            }
        }


        private void Status_TextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Control)
            {
                isCtrlKeyPressed = false;
                Status_TextBox.AcceptsReturn = true;
            }
        }


        private void NotifiToast(Status status)
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);

            var pictureNodes = toastXml.GetElementsByTagName("image");
            ((XmlElement)pictureNodes[0]).SetAttribute("src", status.user.profile_image_url);
            ((XmlElement)pictureNodes[0]).SetAttribute("alt", "user profile pict");

            var textNodes = toastXml.GetElementsByTagName("text");
            textNodes[0].AppendChild(toastXml.CreateTextNode(status.user.name));
            textNodes[1].AppendChild(toastXml.CreateTextNode(status.text));

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }


        private void GoToHomeTimeline_Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void Favorite_Button_Click_1(object sender, RoutedEventArgs e)
        {
            Status selectedStatus = HomeTimelineList.SelectedItem as Status;
            Dictionary<string, string> statusID = new Dictionary<string,string>();
            statusID.Add("id", selectedStatus.id_str);

            Favorite_Button.IsEnabled = false;
            await twitter.Post("https://api.twitter.com/1.1/favorites/create.json", statusID);
            Favorite_Button.IsEnabled = true;
            BottomAppBar.IsOpen = false;
        }



    }
}
