using System.Collections.Generic;
using Bangumi.Models;
using EasyHttp.Http;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;

namespace Bangumi.Services
{
    public class BangumiMetadataService
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private const string BANGUMI_API = "https://api.bgm.tv";
        private readonly HttpClient client;

        public BangumiMetadataService(string accessToken)
        {
            this.client = new HttpClient(BANGUMI_API);
            this.client.Request.AddExtraHeader("Authorization", $"Bearer {accessToken}");
            this.client.Request.Accept = HttpContentTypes.ApplicationJson;
            this.client.Request.UserAgent =
                "ivanlon/PlayniteBangumiMetadataProvider/1.0 (https://github.com/Ivanlon30000/PlayniteBangumiMetadata)";
        }

        public Dictionary<string, string> GetMe()
        {
            string uri = "/v0/me";
            try
            {
                // logger.Debug("get me");
                HttpResponse httpResponse = client.Get(uri);
                JObject jObject = JsonConvert.DeserializeObject<JObject>(httpResponse.RawText);
                Dictionary<string,string> info = new Dictionary<string, string>
                {
                    { "username", jObject["username"].ToObject<string>() },
                    { "nickname", jObject["nickname"].ToObject<string>() },
                    { "sign", jObject["sign"].ToObject<string>() },
                    { "avatar", jObject["avatar"]["large"].ToObject<string>() },
                    { "id", jObject["id"].ToObject<int>().ToString() }
                };
                // logger.Debug(JsonConvert.SerializeObject(info));
                return info;
            }
            catch (Exception e)
            {
                logger.Error($"get me error: {e.GetType()}: {e.Message}");
            }

            return null;
        }

        public List<BangumiSubject> Search(string keyword, string pattern = null)
        {
            // logger.Debug($"Search '{keyword}'");
            
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
                // logger.Debug($"match subject with id {id}");
            }
            

            // search
            string uri = $"/search/subject/{Uri.EscapeDataString(keyword.Replace("!", ""))}";

            try
            {
                // logger.Debug($"HTTP Get {uri}");
                HttpResponse httpResponse = client.Get(uri, new { type = 4, responseGroup = "medium" });
                JObject jObject = JsonConvert.DeserializeObject<JObject>(httpResponse.RawText);
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
                logger.Error($"{e.GetType()}:\t{e.Message}");
            }

            // logger.Debug($"Search result: [{string.Join(",", searchResult)}]");
            return searchResult;
        }

        public BangumiSubject GetSubjectById(uint id)
        {
            string uri = $"/v0/subjects/{id}";
            BangumiSubject bangumiSubject = null;
            try
            {
                // logger.Debug($"Get {uri}");
                HttpResponse httpResponse = client.Get(uri);
                bangumiSubject = Parse(httpResponse.RawText);
            }
            catch (Exception e)
            {
                logger.Error($"{e.GetType()}: {e.Message}");
            }

            return bangumiSubject;
        }

        private BangumiSubject Parse(string json)
        {
            JObject jObject = JsonConvert.DeserializeObject<JObject>(json);
            if (!jObject.ContainsKey("id"))
            {
                return null;
            }
            
            uint id = jObject["id"].ToObject<uint>();
            
            SubjectType type = SubjectType.Game;
            
            string name = null;
            try
            {
                name = jObject["name"].ToObject<string>();
            }
            catch (ArgumentException)
            {
            }

            string nameCn = null;
            try
            {
                nameCn = jObject["name_cn"].ToObject<string>();
            }
            catch (ArgumentException)
            {
            }

            string summary = null;
            try
            {
                summary = jObject["summary"].ToObject<string>()
                    .Replace("\r", "")
                    .Replace("\n", "<br>\n");
            }
            catch (ArgumentException)
            {
            }

            bool nsfw = false;
            try
            {
                nsfw = jObject["nsfw"].ToObject<bool>();
            }
            catch (ArgumentException)
            {
            }

            bool locked = false;
            try
            {
                locked = jObject["locked"].ToObject<bool>();
            }
            catch (ArgumentException)
            {
            }

            string date = null;
            try
            {
                date = jObject["date"].ToObject<string>();
            }
            catch (ArgumentException)
            {
            }

            string platform = null;
            try
            {
                platform = jObject["platform"].ToObject<string>();
            }
            catch (ArgumentException)
            {
            }

            Dictionary<string, string> images = null;
            try
            {
                images = jObject["images"].ToObject<Dictionary<string, string>>();
            }
            catch(ArgumentException)
            {
            }

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

            int volumes = 0;
            try
            {
                volumes = jObject["volumes"].ToObject<int>();
            }
            catch (ArgumentException)
            {
            }

            int eps = 0;
            try
            {
                eps = jObject["eps"].ToObject<int>();
            }
            catch (ArgumentException)
            {
            }

            int totalEpisodes = 0;
            try
            {
                totalEpisodes = jObject["total_episodes"].ToObject<int>();
            }
            catch (ArgumentException)
            {
            }

            BangumiRating rating = null;
            JToken ratingObject = jObject["rating"];
            try
            {
                rating = new BangumiRating(
                    ratingObject["rank"].ToObject<int>(),
                    ratingObject["total"].ToObject<int>(),
                    ratingObject["count"].ToObject<Dictionary<string, int>>(),
                    ratingObject["score"].ToObject<double>());
            }
            catch (ArgumentException)
            {
            }

            Dictionary<string, int> collection = null;
            try
            {
                collection = jObject["collection"].ToObject<Dictionary<string, int>>();
            }
            catch (ArgumentException)
            {
            }

            List<BangumiTag> tags = null;
            try
            {
                tags = new List<BangumiTag>(
                    jObject["tags"].Select(token =>
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