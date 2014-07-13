using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace MetroTwitter_Rev1
{
    class TwitterStream
    {
        public event ReceiveEventHandler Received;
        RequestState state = null;
        public delegate void ReceiveEventHandler(string result);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private async void Execute(object obj)
        {
            // 引数オブジェクトを実際の型にキャスト
            ArgsObject args = (ArgsObject)obj;

            // ワーカースレッドでエラーを処理しないと
            // プログラム自体が落ちてしまうため処理は必須
            try
            {
                // パラメータの作成用StringBuilder作成
                StringBuilder paramBuilder = new StringBuilder();

                // 与えられたパラメータ類をすべて&で結合
                // このへんの処理はOAuthのせいでだいぶ変わる気がする
                foreach (string param in args.Parameters)
                {
                    if (paramBuilder.Length > 0)
                        paramBuilder.Append("&");
                    paramBuilder.Append(param);
                }

                // HttpWebRequestの作成、構造体内のURL利用
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(args.Url);

                // 列挙体から要求メソッドを取り出してる？
                // 今回ならGETのみに置き換えてもいい気がする
                request.Method = Enum.GetName(typeof(HttpMethod), args.Method);

                // コンテンツタイプ指定、HTTPヘッダ回りなので触らない
                request.ContentType = "application/x-www-form-urlencoded";

                // ベーシック認証用らしいのでたぶん要らない
                request.Credentials = new NetworkCredential(args.UserId, args.Password);

                // 非同期動作で使用する状態オブジェクト
                // 独自クラスなので後から詳しく見る必要あり
                // state内にHTTPWebRequestとか持っててややこしい
                state = new RequestState(args.BufferSize);
                state.Request = request;

                
                // GetRequestStreamはAsync化しないといけない
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                if (paramBuilder.Length > 0)
                    writer.Write(paramBuilder.ToString());

                writer.Close();

                // 非同期要求
                IAsyncResult iAasyncResult = (IAsyncResult)request.BeginGetResponse(new AsyncCallback(ResponseCallback), state);
            }

            catch (Exception e)
            {
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void ResponseCallback(IAsyncResult ar)
        {
            try
            {
                // 非同期要求用状態オブジェクト
                RequestState state = (RequestState)ar.AsyncState;
                HttpWebRequest request = state.Request;
                state.Response =
                  (HttpWebResponse)request.EndGetResponse(ar);
                // ストリームから読み込む
                Stream stream = state.Response.GetResponseStream();
                state.Stream = stream;
                // 非同期読み込み開始
                IAsyncResult iAsyncResult =
                  stream.BeginRead(
                    state.Buffer, 0, state.BufferSize,
                      new AsyncCallback(ReadCallBack), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void ReadCallBack(IAsyncResult ar)
        {
            try
            {
                RequestState state = (RequestState)ar.AsyncState;
                // 非同期読み込み(待機)
                Stream stream = state.Stream;
                int read = stream.EndRead(ar);
                if (read > 0)
                {
                    state.StringValue.Append(
                      Encoding.ASCII.GetString(state.Buffer, 0, read));
                    if (state.StringValue.Length > 1)
                    {
                        string stringContent =
                          state.StringValue.ToString();
                        // 受信イベントを
                        OnReciveded(stringContent);
                        state.DataClear();
                    }
                    IAsyncResult iAsyncResult =
                    stream.BeginRead(state.Buffer, 0, state.BufferSize,
                      new AsyncCallback(ReadCallBack), state);
                }
                else
                {
                    if (state.StringValue.Length > 1)
                    {
                        string stringContent = state.StringValue.ToString();
                        OnReciveded(stringContent);
                        state.DataClear();
                    }
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        private void OnReciveded(string content)
        {
            if (Received != null)
                Received(content);
        }


        /// <summary>
        /// 
        /// </summary>
        private void Stop()
        {
            state.Request.Abort();
        }
    }


    public struct ArgsObject
    {
        public string UserId;
        public string Password;
        public HttpMethod Method;
        public string Url;
        public int BufferSize;
        public string[] Parameters;

        public ArgsObject(string userId, string password,
                          string url, HttpMethod method,
        string[] parameters, int bufferSize)
        {
            this.UserId = userId;
            this.Password = password;
            this.Url = url;
            this.Method = method;
            this.Parameters = parameters;
            this.BufferSize = bufferSize;
        }
    }


    public class RequestState
    {
        public int BufferSize = 1024;
        public StringBuilder StringValue = new StringBuilder();
        public byte[] Buffer;
        public HttpWebRequest Request = null;
        public HttpWebResponse Response = null;
        public Stream Stream;

        public RequestState(int bufferSize)
        {
            BufferSize = bufferSize;
            Buffer = new byte[BufferSize];
        }

        public RequestState()
        {
            Buffer = new byte[BufferSize];
        }

        public void DataClear()
        {
            StringValue.Length = 0;
        }
    }

    public enum HttpMethod
    {
        POST,
        GET,
        DELETE
    }

    public enum ResultFormat
    {
        json,
        xml,
        rss,
        atom
    }
}
