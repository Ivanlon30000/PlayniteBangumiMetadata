using System.Collections.Generic;
using Bangumi.Models;
using EasyHttp.Http;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using EasyHttp.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;

namespace Bangumi.Services
{
    public class BangumiMetadataService
    {
        private const string BANGUMI_API = "https://api.bgm.tv";

        private ILogger logger;
        private HttpClient client;
        private Bangumi plugin;

        public BangumiMetadataService(Bangumi plugin)
        {
            this.plugin = plugin;
            this.logger = plugin.Logger;
            this.client = new HttpClient(BANGUMI_API)
            {
                Request =
                {
                    Accept = HttpContentTypes.ApplicationJson,
                    UserAgent = $"ivanlon/PlayniteBangumiMetadataProvider/{Bangumi.VERSION} (https://github.com/Ivanlon30000/PlayniteBangumiMetadata)"
                }
            };
        }

        private string DoGet(string uri, object query = null)
        {
            string rawContent = null;
            logger.Debug($"Get {uri}");
            if (!string.IsNullOrEmpty(plugin.Settings.AccessToken))
            {
                client.Request.RawHeaders["Authorization"] = $"Bearer {this.plugin.Settings.AccessToken}";
            }
            else
            {
                logger.Debug("without token");
            }

            try
            {
                HttpResponse response = client.Get(uri, query);
                rawContent = response.RawText;
            }
            catch (Exception e)
            {
                logger.Warn($"invoke `GetMe` network error: {e.GetType()}: {e.Message}");
            }

            return rawContent;
        }

        public Dictionary<string, string> GetMe()
        {
            logger.Debug("invoke GetMe");

            string jsonRaw = DoGet("/v0/me");
            
            if (!String.IsNullOrEmpty(jsonRaw))
            {
                try
                {
                    JObject jObject = JObject.Parse(jsonRaw);
                    Dictionary<string,string> info = new Dictionary<string, string>
                    {
                        { "username", jObject["username"].ToObject<string>() },
                        { "nickname", jObject["nickname"].ToObject<string>() },
                        { "sign", jObject["sign"].ToObject<string>() },
                        { "avatar", jObject["avatar"]["large"].ToObject<string>() },
                        { "id", jObject["id"].ToObject<int>().ToString() }
                    };
                    logger.Debug("GetMe success");
                    return info;
                }
                catch (Exception e)
                {
                    logger.Warn($"invoke parse error: {e.GetType()}: {e.Message}; the access token may be wrong");
                }
            }
            
            return null;
        }

        public List<BangumiSubject> Search(string keyword, string pattern = null)
        {
            logger.Debug($"Search '{keyword}'");

            // 直接解析为bangumi id
            List<BangumiSubject> searchResult = new List<BangumiSubject>();
            uint id = 0;
            try
            {
                id = uint.Parse(keyword);
            }
            catch (FormatException)
            {

            }

            // 从名称中提取bangumi id
            if (id == 0 && !string.IsNullOrEmpty(pattern))
            {
                Match match = Regex.Match(keyword, pattern);
                string idStr = match.Groups["id"].Value;
                if (!string.IsNullOrEmpty(idStr))
                {
                    try
                    {
                        id = uint.Parse(idStr);
                    }
                    catch (FormatException)
                    {

                    }
                }
            }

            if (id > 0)
            {
                searchResult.Add(GetSubjectById(id));
                logger.Debug($"match subject with id {id}");
            }


            // 使用名字搜索
            string rawJson = DoGet($"/search/subject/{Uri.EscapeDataString(keyword.Replace("!", ""))}",
                new { type = 4, responseGroup = "medium" });
            if (!string.IsNullOrEmpty(rawJson))
            {
                try
                {
                    JObject jObject = JsonConvert.DeserializeObject<JObject>(rawJson);
                    JArray jArray = jObject["list"].ToObject<JArray>();
                    searchResult.AddRange(jArray
                        .Where(token => token["id"].ToObject<uint>() != id)
                        .Select(token => new BangumiSubject(
                            id: token["id"].ToObject<uint>(), name: token["name"].ToObject<string>(),
                            nameCn: token["name_cn"].ToObject<string>(), summary: token["summary"].ToObject<string>()
                        )));
                }
                catch (Exception e)
                {
                    logger.Warn($"{e.GetType()}:\t{e.Message}");
                }
            }
            
            logger.Debug($"Search result: [{string.Join(",", searchResult.Select(subject => subject.id))}]");
            return searchResult;
        }

        public BangumiSubject GetSubjectById(uint id)
        {
            logger.Debug($"invoke GetSubjectById({id})");
            
            BangumiSubject bangumiSubject = null;
            
            string raw = DoGet($"/v0/subjects/{id}");
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    bangumiSubject = Parse(raw);
                }
                catch (Exception e)
                {
                    logger.Warn($"{e.GetType()}: {e.Message}");
                }
            }
            
            logger.Debug($"Subject: {bangumiSubject}");
            return bangumiSubject;
        }

        private BangumiSubject Parse(string json)
        {
            JObject jObject;
            try
            {
                jObject = JObject.Parse(json);
            }
            catch (JsonReaderException)
            {
                return null;
            }

            if (!jObject.ContainsKey("id"))
            {
                return null;
            }

            // id
            uint id = 0;
            try
            {
                id = jObject["id"].Value<uint>();
            }
            catch (ArgumentException)
            {
                return null;
            }

            // type
            SubjectType type = SubjectType.Game;

            // name
            string name = null;
            try
            {
                JToken nameToken = jObject["name"];
                if (nameToken != null) name = nameToken.Value<string>();
            }
            catch (ArgumentException)
            {
            }

            // nameCn
            string nameCn = null;
            try
            {
                JToken nameCnToken = jObject["name_cn"];
                if (nameCnToken != null) nameCn = nameCnToken.Value<string>();
            }
            catch (ArgumentException)
            {
            }

            // summary
            string summary = null;
            try
            {
                JToken summaryToken = jObject["summary"];
                if (summaryToken != null)
                {
                    summary = summaryToken.Value<string>()
                        .Replace("\r", "")
                        .Replace("\n", "<br>\n");
                }
            }
            catch (ArgumentException)
            {
            }

            // nsfw
            bool nsfw = false;
            try
            {
                JToken nsfwToken = jObject["nsfw"];
                if (nsfwToken != null) nsfw = nsfwToken.Value<bool>();
            }
            catch (ArgumentException)
            {
            }

            // locked
            bool locked = false;
            try
            {
                JToken lockedToken = jObject["locked"];
                if (lockedToken != null) locked = lockedToken.Value<bool>();
            }
            catch (ArgumentException)
            {
            }

            // date
            string date = null;
            try
            {
                JToken dateToken = jObject["date"];
                if (dateToken != null) date = dateToken.Value<string>();
            }
            catch (ArgumentException)
            {
            }

            // platform
            string platform = null;
            try
            {
                JToken platformToken = jObject["platform"];
                if (platformToken != null) platform = platformToken.Value<string>();
            }
            catch (ArgumentException)
            {
            }

            // images
            Dictionary<string, string> images = null;
            try
            {
                JToken platformToken = jObject["images"];
                if (platformToken != null) images = platformToken.ToObject<Dictionary<string, string>>();
            }
            catch (ArgumentException)
            {
            }

            // info box
            Dictionary<string, string> textInfobox = new Dictionary<string, string>();
            Dictionary<string, List<string>> listInfobox = new Dictionary<string, List<string>>();
            try
            {
                foreach (var jToken in jObject["infobox"].ToObject<JArray>())
                {
                    try
                    {
                        textInfobox[jToken["key"].ToObject<string>()] = jToken["value"].ToObject<string>();
                        continue;
                    }
                    catch (ArgumentException)
                    {
                    }

                    try
                    {
                        listInfobox[jToken["key"].ToObject<string>()] = new List<string>(jToken["value"]
                            .ToObject<JArray>()
                            .Select(token => token["v"].ToObject<string>()));
                        continue;
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
            catch (ArgumentException)
            {
            }

            // volumes
            int volumes = 0;
            try
            {
                JToken volumesToken = jObject["volumes"];
                if (volumesToken != null) volumes = volumesToken.Value<int>();
            }
            catch (ArgumentException)
            {
            }

            // eps
            int eps = 0;
            try
            {
                JToken epsToken = jObject["eps"];
                if (epsToken != null) eps = epsToken.Value<int>();
            }
            catch (ArgumentException)
            {
            }

            // total eps
            int totalEpisodes = 0;
            try
            {
                JToken totalEpisodesToken = jObject["total_episodes"];
                if (totalEpisodesToken != null) totalEpisodes = totalEpisodesToken.Value<int>();
            }
            catch (ArgumentException)
            {
            }

            // rating
            BangumiRating rating = null;
            try
            {
                JToken ratingObject = jObject["rating"];
                if (ratingObject != null)
                    rating = new BangumiRating(
                        ratingObject["rank"].ToObject<int>(),
                        ratingObject["total"].ToObject<int>(),
                        ratingObject["count"].ToObject<Dictionary<string, int>>(),
                        ratingObject["score"].ToObject<double>());
            }
            catch (ArgumentException)
            {
            }

            // collection
            Dictionary<string, int> collection = null;
            try
            {
                JToken collectionToken = jObject["collection"];
                if (collectionToken != null) collection = collectionToken.ToObject<Dictionary<string, int>>();
            }
            catch (ArgumentException)
            {
            }

            // tags
            List<BangumiTag> tags = null;
            try
            {
                JToken tagsToken = jObject["tags"];
                if (tagsToken != null)
                    tags = new List<BangumiTag>(
                        tagsToken.Select(token =>
                            new BangumiTag(token["name"].ToObject<string>(), token["count"].ToObject<int>()))
                    );
            }
            catch (ArgumentException)
            {
            }

            return new BangumiSubject(
                id, type, name, nameCn, summary,
                nsfw, locked, date, platform,
                images, textInfobox, listInfobox,
                volumes, eps, totalEpisodes, rating,
                collection, tags
            );
        }
    }
}