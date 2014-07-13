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

// 基本ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234237 を参照してください

namespace MetroTwitter_Rev1
{
    /// <summary>
    /// 多くのアプリケーションに共通の特性を指定する基本ページ。
    /// </summary>
    public sealed partial class Activation : MetroTwitter_Rev1.Common.LayoutAwarePage
    {
        const string CONSUMER_KEY = "9joiIRntsMA5i09DZNcw";
        const string CONSUMER_SECRET = "xhTnZmu3BGb8RYkjRvcfgL9B071V4caaLEl1ZEYoRWU";
        public Twitter twitter = new Twitter(CONSUMER_KEY, CONSUMER_SECRET);

        public Activation()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// このページには、移動中に渡されるコンテンツを設定します。前のセッションからページを
        /// 再作成する場合は、保存状態も指定されます。
        /// </summary>
        /// <param name="navigationParameter">このページが最初に要求されたときに
        /// <see cref="Frame.Navigate(Type, Object)"/> に渡されたパラメーター値。
        /// </param>
        /// <param name="pageState">前のセッションでこのページによって保存された状態の
        /// ディクショナリ。ページに初めてアクセスするとき、状態は null になります。</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Uri activateUri = new Uri(await twitter.GetAuthorizeUrl());
            ActivateView.Navigate(activateUri);
        }


        private async void Activate_Button_Click(object sender, RoutedEventArgs e)
        {
            var userData = Windows.Storage.ApplicationData.Current.LocalSettings;

            ActivateStatus_Label.Text = "Activating...";

            try
            {
                await twitter.GetAccessToken(PINCode_Box.Text.ToString().Trim());
            }
            catch(Exception)
            {
                ActivateStatus_Label.Text = "Activate Failed.";
                return;
            }

            ActivateStatus_Label.Text = "Activate Success!";

            userData.Values["AccessToken"] = Twitter.AccessToken;
            userData.Values["AccessTokenSecret"] = Twitter.AccessTokenSecret;
            userData.Values["UserID"] = Twitter.UserId;
            userData.Values["ScreenName"] = Twitter.ScreenName;

            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
