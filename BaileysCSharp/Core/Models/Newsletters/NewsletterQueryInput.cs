using BaileysCSharp.Core.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BaileysCSharp.Core.Models.Newsletters
{
    public interface INewsletterInputParams
    {

    }
    public interface INewsletterVariable
    {

    }
    public class WMexQuery
    {
        [JsonPropertyName("variables")]
        public INewsletterVariable Variables { get; set; }
    }

    public class NewsletterQueryInput : INewsletterInputParams
    {

        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("view_role")]
        public string ViewRole { get; set; }
    }

    public class CreateNewsletterInput : INewsletterInputParams
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class NewsletterRecommendedInput : INewsletterInputParams
    {
        //For Query
        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("country_codes")]
        public List<string> CountryCodes { get; set; }
    }



    public class NewsletterUpdateParamType
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }


        [JsonPropertyName("settings")]
        public NewsletterSettings Setttings { get; set; }
    }

    public class NewsletterSettings
    {
        [JsonPropertyName("reaction_codes")]
        public NewsletterReactionMode ReactionMode { get; set; }
    }


    public class NewsletterVariable : INewsletterVariable
    {
        [JsonPropertyName("input")]
        public INewsletterInputParams Input { get; set; }

        [JsonPropertyName("updates")]
        public NewsletterUpdateParamType Updates { get; set; }

        [JsonPropertyName("newsletter_id")]
        public string NewsletterID { get; set; }
    }

    public class NewsletterMetadataVariable: INewsletterVariable
    {
        [JsonPropertyName("input")]
        public INewsletterInputParams Input { get; set; }

        [JsonPropertyName("fetch_viewer_metadata")]
        public bool FetchViewerMetadata { get; set; }

        [JsonPropertyName("fetch_full_image")]
        public bool FetchFullImage { get; set; }

        [JsonPropertyName("fetch_creation_time")]
        public bool FetchCreationTime { get; set; }
    }

    public class QueryChannellsParams
    {
        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("country_codes")]
        public List<string> CountryCodes { get; set; }
    }


    public class NewsletterActionResult
    {
        [JsonPropertyName("data")]
        public CreateNewsletterData Data { get; set; }
    }

    public class CreateNewsletterData
    {
        [JsonPropertyName("xwa2_newsletter_create")]
        public NewsletterMetaData Xwa2NewsletterCreate { get; set; }
    }

    public class QueryNewsletterData
    {
        [JsonPropertyName("xwa2_newsletter")]
        public NewsletterMetaData Xwa2Newsletter { get; set; }
    }


    public class QueryNewsletterResult
    {
        [JsonPropertyName("data")]
        public QueryNewsletterData Data { get; set; }
    }

    public class NewsletterMetaData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("state")]
        public StateMetaData State { get; set; }

        [JsonPropertyName("thread_metadata")]
        public ThreadMetadata Thread { get; set; }

        [JsonPropertyName("viewer_metadata")]
        public ViewerMetadata Viewer { get; set; }


        public class Description
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }

            [JsonPropertyName("update_time")]
            public string UpdateTime { get; set; }
        }

        public class Name
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }

            [JsonPropertyName("update_time")]
            public string UpdateTime { get; set; }
        }

        public class Picture
        {
            [JsonPropertyName("direct_path")]
            public string DirectPath { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }
        }

        public class Preview
        {
            [JsonPropertyName("direct_path")]
            public string DirectPath { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }
        }

        public class StateMetaData
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }
        }

        public class ThreadMetadata
        {
            [JsonPropertyName("creation_time")]
            public string CreationTime { get; set; }

            [JsonPropertyName("description")]
            public Description Description { get; set; }

            //[JsonPropertyName("handle")]
            //public object Handle { get; set; }

            [JsonPropertyName("invite")]
            public string Invite { get; set; }

            [JsonPropertyName("name")]
            public Name Name { get; set; }

            [JsonPropertyName("picture")]
            public Picture Picture { get; set; }

            [JsonPropertyName("preview")]
            public Preview Preview { get; set; }

            [JsonPropertyName("subscribers_count")]
            public string SubscribersCount { get; set; }

            [JsonPropertyName("verification")]
            public string Verification { get; set; }
        }

        public class ViewerMetadata
        {
            [JsonPropertyName("mute")]
            public string Mute { get; set; }

            [JsonPropertyName("role")]
            public string Role { get; set; }
        }

        public class QueryNewsletterRecommendedResult
        {
            [JsonPropertyName("data")]
            public QueryNewsletterRecommendedResultData Data { get; set; }
        }

        public class QueryNewsletterRecommendedResultData
        {
            [JsonPropertyName("xwa2_newsletters_recommended")]
            public Xwa2NewslettersRecommended Xwa2NewslettersRecommended { get; set; }
        }


        public class Xwa2NewslettersRecommended
        {
            [JsonPropertyName("page_info")]
            public PageInfo PageInfo { get; set; }

            [JsonPropertyName("result")]
            public List<NewsletterMetaData> Result { get; set; }
        }

        public class PageInfo
        {
            [JsonPropertyName("endCursor")]
            public string EndCursor { get; set; }

            [JsonPropertyName("hasNextPage")]
            public bool HasNextPage { get; set; }

            [JsonPropertyName("hasPreviousPage")]
            public bool HasPreviousPage { get; set; }

            [JsonPropertyName("startCursor")]
            public object StartCursor { get; set; }
        }
    }



    public class NewsletterAdminCountData
    {
        [JsonPropertyName("xwa2_newsletter_admin")]
        public Xwa2NewsletterAdmin Xwa2NewsletterAdmin { get; set; }
    }

    public class NewsletterAdminCountResult
    {
        [JsonPropertyName("data")]
        public NewsletterAdminCountData Data { get; set; }
    }

    public class Xwa2NewsletterAdmin
    {
        [JsonPropertyName("admin_count")]
        public int AdminCount { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public enum NewsletterMetaDataType
    {
        INVITE,
        JID
    }

}
