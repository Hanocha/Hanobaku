using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Twitter
{
    /// <summary>
    /// Twitterから受信したすべてのデータを格納するクラスです。
    /// </summary>
    public class Status
    {
        public Contributors contributors { get; set; }
        public Coordinates coordinates { get; set; }
        public string created_at { get; set; }
        public Entities entities { get; set; }
        public bool favorited { get; set; }
        public long? id { get; set; }
        public string id_str { get; set; }
        public string in_reply_to_screen_name { get; set; }
        public long? in_reply_to_status_id { get; set; }
        public string in_reply_to_status_id_str { get; set; }
        public long? in_reply_to_user_id { get; set; }
        public string in_reply_to_user_id_str { get; set; }
        public Places place { get; set; }
        public int? retweet_count { get; set; }
        public bool retweeted { get; set; }
        public Status retweeted_status { get; set; }
        public object source { get; set; }
        public string text { get; set; }
        public bool truncated { get; set; }
        public User user { get; set; }

        public bool isretweeted { get; set; }
        public string retweeted_by { get; set; }

        public DirectMessage direct_message { get; set; }
    }


    // 
    public class DirectMessage
    {
        public string created_at { get; set; }
        public long? id { get; set; }
        public string id_str { get; set; }
        public string text { get; set; }
        public User sender { get; set; }
        public long? sender_id { get; set; }
        public string sender_screen_name { get; set; }
        public User recipient { get; set; }
        public long? recipient_id { get; set; }
        public string recipient_screen_name { get; set; }
    }

    public class User
    {
        private string _screen_name;

        public string profile_sidebar_border_color { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public string screen_name { get{ return "@" + _screen_name; } set { _screen_name = value; } }
        public string following { get; set; }
        public bool verified { get; set; }
        public string profile_text_color { get; set; }
        public int? followers_count { get; set; }
        public string profile_background_image_url { get; set; }
        public string created_at { get; set; }
        public string notifications { get; set; }
        public int? friends_count { get; set; }
        public string profile_link_color { get; set; }
        public bool profile_background_tile { get; set; }
        public int? favourites_count { get; set; }
        public string profile_background_color { get; set; }
        public bool Protected { get; set; }
        public string time_zone { get; set; }
        public string location { get; set; }
        public string name { get; set; }
        public string profile_sidebar_fill_color { get; set; }
        public string id { get; set; }
        public int? statuses_count { get; set; }
        public int? utc_offset { get; set; }
        public string profile_image_url { get; set; }
    }


    public class Contributors
    {
        public long? id { get; set; }
        public string id_str { get; set; }
        public string screen_name { get; set; }
    }


    public class Coordinates
    {
        public double[] coordinates { get; set; }
        public string type { get; set; }
    }


    public class Entities
    {
        public Hashtags[] hashtags { get; set; }
        public Media[] media { get; set; }
        public Urls[] urls { get; set; }
        public User_Mention[] user_mentions { get; set; }
    }


    public class Places
    {
        public Object attributes { get; set; }
        public Bounding_box bounding_box { get; set; }
        public string country { get; set; }
        public string country_code { get; set; }
        public string full_name { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string place_type { get; set; }
        public string url { get; set; }
    }


    public class Hashtags
    {
        public int[] indices { get; set; }
        public string text { get; set; }
    }

    public class Media
    {
        public string display_url { get; set; }
        public string expanded_url { get; set; }
        public long? id { get; set; }
        public string id_str { get; set; }
        public int[] indices { get; set; }
        public string media_url { get; set; }
        public string media_url_https { get; set; }
        public object sizes { get; set; }
        public long? source_status_id { get; set; }
        public string source_status_id_str { get; set; }
        public string type { get; set; }
        public string url { get; set; }
    }

    public class Urls
    {
        public string display_url { get; set; }
        public string expanded_url { get; set; }
        public int[] indices { get; set; }
        public string url { get; set; }
    }

    public class User_Mention
    {
        public long? id { get; set; }
        public string id_str { get; set; }
        public int[] indices { get; set; }
        public string name { get; set; }
        public string screen_name { get; set; }
    }

    public class Bounding_box
    {
        public double[][][] coordinates { get; set; }
        public string type { get; set; }
    }
}
