using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using Twitter;

namespace MetroTwitter_Rev1
{
    public class Twitter
    {
        // トークン類取得URL
        const string REQUEST_TOKEN_URL = "https://twitter.com/oauth/request_token";
        const string ACCESS_TOKEN_URL = "https://twitter.com/oauth/access_token";
        const string AUTHORIZE_URL = "https://twitter.com/oauth/authorize";

        // 各種APIのURL
        const string MENTIONS_URL = "https://api.twitter.com/1.1/statuses/mentions_timeline.json";
        const string USER_TIMELINE_URL = "https://api.twitter.com/1.1/statuses/user_timeline.json";
        const string HOME_TIMELINE_URL = "https://api.twitter.com/1.1/statuses/home_timeline.json";
        const string DIRECT_MESSAGE_URL = "https://api.twitter.com/1.1/direct_messages.json";
        const string STATUS_UPDATE_URL = "https://api.twitter.com/1.1/statuses/update.json";
        const string USER_STREAM_URL = "https://userstream.twitter.com/1.1/user.json";

        public static string ConsumerKey { get; set; }
        public static string ConsumerSecret { get; set; }
        public static string RequestToken { get; set; }
        public static string RequestTokenSecret { get; set; }
        public static string AccessToken { get; set; }
        public static string AccessTokenSecret { get; set; }
        public static string UserId { get; set; }
        public static string ScreenName { get; set; }

        // タイムラインのデータを格納するObservable Collection
        public static ObservableCollection<Status> homeTimelineStatusList { get; set; }
        public static ObservableCollection<Status> mentionsStatusList { get; set; }
        public static ObservableCollection<DirectMessage> directMessageList { get; set; }

        public static bool isStreamingConnecting { get; set; }

        public Twitter(string consumerKey, string consumerSecret)
        {
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;

            homeTimelineStatusList = new ObservableCollection<Status>();
            mentionsStatusList = new ObservableCollection<Status>();
        }


        public Twitter(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, string userId, string screenName)
        {
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            AccessToken = accessToken;
            AccessTokenSecret = accessTokenSecret;
            UserId = userId;
            ScreenName = screenName;
        }


        /// <summary>
        /// 非同期でリクエストトークンを取得します。
        /// </summary>
        public async Task GetRequestToken()
        {
            SortedDictionary<string, string> parameters = GenerateParameters("");
            string signature = GenerateSignature("", "GET", REQUEST_TOKEN_URL, parameters);
            parameters.Add("oauth_signature", Uri.EscapeDataString(signature));
            string response = await HttpGet(REQUEST_TOKEN_URL, parameters);
            Dictionary<string, string> dic = ParseResponse(response);
            RequestToken = dic["oauth_token"];
            RequestTokenSecret = dic["oauth_token_secret"];
        }


        /// <summary>
        /// 認証用URLを返します。
        /// 通常、認証用URL+リクエストトークンになります。
        /// リクエストトークンが取得されていない場合は、その取得も同時に行います。
        /// </summary>
        public async Task<string> GetAuthorizeUrl()
        {
            if (RequestToken == null)
            {
                await GetRequestToken();
            }
            return AUTHORIZE_URL + "?oauth_token=" + RequestToken;
        }


        /// <summary>
        /// アクセストークンを取得します。
        /// あらかじめPINをTwitterから取得する必要があります。
        /// </summary>
        /// <param name="pin">Twitterから発行された7桁のPINコード</param>
        public async Task<string> GetAccessToken(string pin)
        {
            SortedDictionary<string, string> parameters = GenerateParameters(RequestToken);
            parameters.Add("oauth_verifier", pin);
            string signature = GenerateSignature(RequestTokenSecret, "GET", ACCESS_TOKEN_URL, parameters);
            parameters.Add("oauth_signature", Uri.EscapeDataString(signature));
            string response = await HttpGet(ACCESS_TOKEN_URL, parameters);
            Dictionary<string, string> dic = ParseResponse(response);
            AccessToken = dic["oauth_token"];
            AccessTokenSecret = dic["oauth_token_secret"];
            UserId = dic["user_id"];
            ScreenName = dic["screen_name"];
            return AccessToken;
        }


        /// <summary>
        /// TwitterAPIに対するGET要求を行うメソッドです。
        /// 指定URLに対して、シグネチャとパラメータを付加してGET要求を行います。
        /// </summary>
        /// <param name="url">GETを行うURL</param>
        /// <param name="parameters">追加するパラメータ</param>
        public async Task<string> Get(string url, IDictionary<string, string> parameters)
        {
            SortedDictionary<string, string> parameters2 = GenerateParameters(AccessToken);
            foreach (var p in parameters)
                parameters2.Add(p.Key, p.Value);
            string signature = GenerateSignature(AccessTokenSecret, "GET", url, parameters2);
            parameters2.Add("oauth_signature", Uri.EscapeDataString(signature));
            return await HttpGet(url, parameters2);
        }


        /// <summary>
        /// TwitterAPIに対するPOST要求を行うメソッドです。
        /// 指定URLに対して、シグネチャとパラメータを付加してPOST要求を行います。
        /// </summary>
        /// <param name="url">POSTを行うURL</param>
        /// <param name="parameters">追加するパラメータ</param>
        public async Task<string> Post(string url, IDictionary<string, string> parameters)
        {
            SortedDictionary<string, string> parameters2 = GenerateParameters(AccessToken);
            foreach (var p in parameters)
                parameters2.Add(p.Key, p.Value);
            string signature = GenerateSignature(AccessTokenSecret, "POST", url, parameters2);
            parameters2.Add("oauth_signature", Uri.EscapeDataString(signature));
            return await HttpPost(url, parameters2);
        }


        /// <summary>
        /// 非同期で、指定されたURLに対してHTTP-GET要求を行います。
        /// シグネチャ類の生成および付加は行いません。
        /// TwitterAPIへのGET要求はGetメソッドの利用を推薦します。
        /// </summary>
        /// <param name="url">GETを行うURL</param>
        /// <param name="parameters">追加するパラメータ</param>
        /// <returns></returns>
        private async Task<string> HttpGet(string url, IDictionary<string, string> parameters)
        {
            string joinedParameters = JoinParameters(parameters);
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url + '?' + JoinParameters(parameters));
            HttpWebResponse webRes = (HttpWebResponse)await webReq.GetResponseAsync();

            Stream stream = webRes.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            stream.Dispose();
            reader.Dispose();
            return result;
        }


        /// <summary>
        /// 非同期で、指定されたURLに対してHTTP-POST要求を行います。
        /// シグネチャ類の生成および付加は行いません。
        /// TwitterAPIに対するPOST要求はPostメソッドの使用を推薦します。
        /// </summary>
        /// <param name="url">POSTを行うURL</param>
        /// <param name="parameters">追加するパラメータ</param>
        /// <returns></returns>
        async Task<string> HttpPost(string url, IDictionary<string, string> parameters)
        {
            byte[] data = Encoding.UTF8.GetBytes(JoinParameters(parameters));
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            Stream reqStream = await req.GetRequestStreamAsync();
            reqStream.Write(data, 0, data.Length);
            reqStream.Dispose();

            HttpWebResponse res = (HttpWebResponse)await req.GetResponseAsync();
            Stream resStream = res.GetResponseStream();
            StreamReader reader = new StreamReader(resStream, Encoding.UTF8);
            string result = reader.ReadToEnd();
            reader.Dispose();
            resStream.Dispose();
            return result;
        }


        /// <summary>
        /// &で区切られた各種パラメータを持った文字列を各パラメータごと分割、Dictionary化して返します。
        /// </summary>
        private Dictionary<string, string> ParseResponse(string response)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string s in response.Split('&'))
            {
                int index = s.IndexOf('=');
                if (index == -1)
                    result.Add(s, "");
                else
                    result.Add(s.Substring(0, index), s.Substring(index + 1));
            }
            return result;
        }


        /// <summary>
        /// Dictionary形式のパラメータをURLに変換および結合します。
        /// </summary>
        private string JoinParameters(IDictionary<string, string> parameters)
        {
            StringBuilder result = new StringBuilder();
            bool first = true;
            foreach (var parameter in parameters)
            {
                if (first)
                    first = false;
                else
                    result.Append('&');
                result.Append(parameter.Key);
                result.Append('=');
                result.Append(parameter.Value);
            }
            return result.ToString();
        }


        /// <summary>
        /// TwitterAPIの利用に必要なシグネチャの生成を行います。
        /// シグネチャの生成にはHMAC-SHA1を使用します。
        /// </summary>
        /// <param name="tokenSecret">リクエストトークンまたはアクセストークンのSecret</param>
        /// <param name="httpMethod">POSTまたはGET</param>
        /// <param name="url">要求を行うURL</param>
        /// <param name="parameters">要求を行う際に付加するパラメータ</param>
        private string GenerateSignature(string tokenSecret, string httpMethod, string url, SortedDictionary<string, string> parameters)
        {
            string signatureBase = GenerateSignatureBase(httpMethod, url, parameters);
            String strAlgName = MacAlgorithmNames.HmacSha1;     // アルゴリズム名、HMACSHA1
            IBuffer buffMsg;                                    // メッセージのバッファ？
            CryptographicKey hmacKey;                           // キーなのだが型が・・・
            IBuffer buffHMAC;                                   // 生成されたHMACのバッファ？

            // MacProvの作成とアルゴリズムの指定
            var objMacProv = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
            string strNameUsed = objMacProv.AlgorithmName;

            // バイナリ変換時のエンコード指定、変換とバッファへの代入
            // UTF-8を利用して文字列をバイナリエンコードします
            BinaryStringEncoding encoding = BinaryStringEncoding.Utf8;
            buffMsg = CryptographicBuffer.ConvertStringToBinary(signatureBase, encoding);

            // メッセージ署名に利用されるKeyの生成
            // "ConsumerSecret"&"tokenSecret"をバイナリ変換したものをベースに鍵を作成します。
            IBuffer buffKeyMaterial = CryptographicBuffer.ConvertStringToBinary(Uri.EscapeDataString(ConsumerSecret) + '&' + Uri.EscapeDataString(tokenSecret), encoding);
            hmacKey = objMacProv.CreateKey(buffKeyMaterial);

            // キーを利用してHMACの生成
            buffHMAC = CryptographicEngine.Sign(hmacKey, buffMsg);

            // HMACをbuffからBase64Stringに変換して返す
            return CryptographicBuffer.EncodeToBase64String(buffHMAC);
        }


        /// <summary>
        /// シグネチャ生成の際に必要となる文字列を生成します。
        /// HTTPメソッド以外はURLエンコードされてから追加されます。
        /// </summary>
        /// <param name="httpMethod">POSTまたはGET</param>
        /// <param name="url">要求を行うURL</param>
        /// <param name="parameters">パラメータ</param>
        private string GenerateSignatureBase(string httpMethod, string url, SortedDictionary<string, string> parameters)
        {
            StringBuilder result = new StringBuilder();
            result.Append(httpMethod);
            result.Append('&');
            result.Append(Uri.EscapeDataString(url));
            result.Append('&');
            result.Append(Uri.EscapeDataString(JoinParameters(parameters)));
            return result.ToString();
        }


        /// <summary>
        /// API利用時に必要となる、
        /// Consumer key,Signature method,Timestamp,nonce,version,および与えられたtoken
        /// をすべて含んだSorted Dictionaryを返します。
        /// </summary>
        /// <param name="token">リクエストトークンもしくはアクセストークン</param>
        private SortedDictionary<string, string> GenerateParameters(string token)
        {
            SortedDictionary<string, string> result = new SortedDictionary<string, string>();
            result.Add("oauth_consumer_key", ConsumerKey);
            result.Add("oauth_signature_method", "HMAC-SHA1");
            result.Add("oauth_timestamp", GenerateTimestamp());
            result.Add("oauth_nonce", GenerateNonce());
            result.Add("oauth_version", "1.0");
            if (!string.IsNullOrEmpty(token))
                result.Add("oauth_token", token);
            return result;
        }


        /// <summary>
        /// 同じリクエストの重複を防ぐためのランダムな文字列を生成します。
        /// </summary>
        private string GenerateNonce()
        {
            Random random = new Random();
            string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder result = new StringBuilder(8);
            for (int i = 0; i < 8; ++i)
                result.Append(letters[random.Next(letters.Length)]);
            return result.ToString();
        }


        /// <summary>
        /// タイムスタンプを生成します。
        /// 1970年1月1日0時0分0秒0からの経過時刻を返します。
        /// </summary>
        private string GenerateTimestamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }


        /// <summary>
        /// つぶやきを投稿します。
        /// </summary>
        /// <param name="userStatus">投稿内容文字列、140文字以内</param>
        /// <param name="options">投稿時オプションディクショナリ</param>
        public async Task Tweet(string userStatus, Dictionary<string, string> options = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Clear();
            parameters.Add("status", Uri.EscapeDataString(userStatus));

            if (options != null)
            {
                foreach (var item in options)
                {
                    parameters.Add(item.Key, item.Value);
                }
            }

            await Post(STATUS_UPDATE_URL, parameters);
        }



        /// <summary>
        /// HomeTimelineから投稿を指定された数だけ取得してリストを更新します。
        /// 古いツイートはすべて削除されます。
        /// 特に指定がない場合は20件取得します。
        /// </summary>
        /// <param name="count">投稿取得件数を示す整数値、最大200</param>
        /// <returns></returns>
        public async Task GetHomeTimelineAsync(int count = 20)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            ObservableCollection<Status> tempStatusList = new ObservableCollection<Status>();
            parameters.Clear();
            parameters.Add("count", count.ToString());
            string Json = await Get(HOME_TIMELINE_URL, parameters);

            homeTimelineStatusList = await JsonConvert.DeserializeObjectAsync<ObservableCollection<Status>>(Json);
            var tempHomeTimelineList = new ObservableCollection<Status>();

            foreach (var item in homeTimelineStatusList)
            {
                Status tempItem;

                if (item.retweeted_status != null)
                {
                    tempItem = ConvertRetweet(item);
                }

                else
                {
                    tempItem = item;
                }

                tempHomeTimelineList.Add(tempItem);
            }

            tempHomeTimelineList.OrderBy(n => n.id);
            homeTimelineStatusList.Clear();
            homeTimelineStatusList = tempHomeTimelineList;
        }


        /// <summary>
        /// 新着投稿を取得します。
        /// </summary>
        /// <returns></returns>
        public async Task GetNewHomeTimelineAsync()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            string latestId = homeTimelineStatusList.First().id_str;
            parameters.Clear();
            parameters.Add("since_id", latestId);
            string Json = await Get(HOME_TIMELINE_URL, parameters);

            var gottenStatusList = (ObservableCollection<Status>)JsonConvert.DeserializeObject(Json, typeof(ObservableCollection<Status>));
            var tempCollection = new ObservableCollection<Status>();

            foreach (var item in gottenStatusList)
            {
                Status tempItem;

                if (item.retweeted_status != null)
                {
                    tempItem = ConvertRetweet(item);
                }

                else
                {
                    tempItem = item;
                }

                homeTimelineStatusList.Add(tempItem);
            }

            homeTimelineStatusList.OrderBy(n => n.id);
        }


        /// <summary>
        /// Mensionsを取得します。
        /// Newtonsoft-Jsonを利用しています。
        /// </summary>
        public async Task GetMentionsAsync()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Clear();
            parameters.Add("count", "20");
            string Json = await Get(MENTIONS_URL, parameters);

            mentionsStatusList = await JsonConvert.DeserializeObjectAsync<ObservableCollection<Status>>(Json);
        }


        /// <summary>
        /// 新着Mensionsを取得します。
        /// Newtonsoft-Jsonを利用しています。
        /// </summary>
        public async Task GetNewMentionsAsync()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            ObservableCollection<Status> tempStatusList = new ObservableCollection<Status>();
            tempStatusList.Clear();

            string latestId = mentionsStatusList.First().id_str;
            parameters.Clear();
            parameters.Add("since_id", latestId);
            string Json = await Get(MENTIONS_URL, parameters);

            tempStatusList = (ObservableCollection<Status>)JsonConvert.DeserializeObject(Json, typeof(ObservableCollection<Status>));
            var reversedTempStatusList = tempStatusList.Reverse();

            foreach (var item in reversedTempStatusList)
            {
                mentionsStatusList.Insert(0, item);
            }
        }


        /// <summary>
        /// Direct Messageを取得します。
        /// </summary>
        public async Task GetDirectMessageAsync()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Clear();
            parameters.Add("count", "20");
            string Json = await Get(DIRECT_MESSAGE_URL, parameters);

            directMessageList = (ObservableCollection<DirectMessage>)JsonConvert.DeserializeObject(Json, typeof(ObservableCollection<DirectMessage>));
        }


        /// <summary>
        /// 新着Direct Messageを取得します。
        /// </summary>
        /// <returns></returns>
        public async Task GetNewDirectMessageAsync()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            string latestID = directMessageList.First().id_str;
            parameters.Clear();
            parameters.Add("since_id", latestID);
            string Json = await Get(DIRECT_MESSAGE_URL, parameters);

            var tempDMList = new ObservableCollection<DirectMessage>();
            tempDMList.Clear();
            tempDMList = (ObservableCollection<DirectMessage>)JsonConvert.DeserializeObject(Json, typeof(ObservableCollection<DirectMessage>));

            if (tempDMList.Count != 0)
            {
                foreach (var item in tempDMList)
                {
                    directMessageList.Add(item);
                }
                
                directMessageList.OrderBy(e => e.id);
            }
        }


        /// <summary>
        /// タイムラインのストリーミング受信を開始します。
        /// </summary>
        public async void GetTimelineStreamAsync()
        {
            var timeoutTimer = new DispatcherTimer();
            timeoutTimer.Interval = TimeSpan.FromSeconds(60);
            timeoutTimer.Tick += timeoutTimer_Tick;

            isStreamingConnecting = true;

            SortedDictionary<string, string> parameters = GenerateParameters(AccessToken);

            string signature = GenerateSignature(AccessTokenSecret, "GET", USER_STREAM_URL, parameters);
            parameters.Add("oauth_signature", Uri.EscapeDataString(signature));

            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(USER_STREAM_URL + '?' + JoinParameters(parameters));
            HttpWebResponse webResStream = (HttpWebResponse)await webReq.GetResponseAsync();
            Stream getTimelineStream = webResStream.GetResponseStream();
            StreamReader reader = new StreamReader(getTimelineStream);


            while (true)
            {
                timeoutTimer.Start();
                string tempString = await reader.ReadLineAsync();
                timeoutTimer.Stop();

                Status tempStatus = await JsonConvert.DeserializeObjectAsync<Status>(tempString);

                if (tempStatus != null)
                {
                    if (tempStatus.direct_message != null)
                    {
                        directMessageList.Insert(0, tempStatus.direct_message);
                    }

                    // ReTweetの検出
                    else if (tempStatus.retweeted_status != null)
                    {
                        homeTimelineStatusList.Insert(0, ConvertRetweet(tempStatus));
                    }

                    // 何らかの発言データであるかどうか
                    else if (tempStatus.id_str != null)
                    {
                        homeTimelineStatusList.Insert(0, tempStatus);

                        // リプライ検出
                        if (tempStatus.entities.user_mentions.Length > 0)
                        {
                            if (tempStatus.entities.user_mentions.Where(e => e.id_str == UserId).Count() == 1)
                            {
                                mentionsStatusList.Insert(0, tempStatus);
                            }
                        }
                    }
                }

                if (!isStreamingConnecting)
                {
                    break;
                }
            }

            webReq.Abort();
            webResStream.Dispose();
            reader.Dispose();
        }


        /// <summary>
        /// ステータスが公式リツイートであるかどうかを判定し、
        /// 公式リツイートだった場合はリスト表示に適した形に書き換えます。
        /// それ以外の場合はそのままの形を返します。
        /// </summary>
        /// <param name="tempStatus">被判定ステータス</param>
        /// <returns></returns>
        private static Status ConvertRetweet(Status tempStatus)
        {
            var retweetedStatus = tempStatus.retweeted_status;
            retweetedStatus.isretweeted = true;
            retweetedStatus.retweeted_by = tempStatus.user.name;

            return retweetedStatus;
        }


        /// <summary>
        /// タイムアウト検出用タイマー用イベントです。
        /// できればもっと簡単にまとめたいけど・・・
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void timeoutTimer_Tick(object sender, object e)
        {
            isStreamingConnecting = false;
        }
    }

}