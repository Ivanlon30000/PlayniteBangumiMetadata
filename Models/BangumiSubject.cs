using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bangumi.Models
{
    public enum SubjectType
    {
        None = 0,
        Book = 1,
        Anime = 2,
        Music = 3,
        Game = 4,
        Real = 6
    }
    
    public class BangumiRating
    {
        public int rank { get; set; }
        public int total { get; set; }
        public Dictionary<string, int> count { get; set; }
        public double score { get; set; }

        public BangumiRating(int rank, int total, Dictionary<string, int> count, double score)
        {
            this.rank = rank;
            this.total = total;
            this.count = count;
            this.score = score;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public struct BangumiTag
    {
        public string name { get; }
        public int count { get; }

        public BangumiTag(string name, int count)
        {
            this.name = name;
            this.count = count;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }


    public class BangumiSubject
    {
        public uint id { get; }
        public SubjectType type { get; }
        public string name { get; }
        public string nameCn { get; }
        public string summary { get; }
        public bool nsfw { get; }
        public bool locked { get; }
        public string date { get; }
        public string platform { get; }
        public Dictionary<string, string> images { get; }
        /*
         * raw infobox 有 2 种可能的元素：
         *  1. `{"key": string, "value": string}`
         *  2. `{“key": string, "value": [{"v": string}, {"v", string} ... ]}`
         * 分别反序列化为 `Dictionary<string, string>` 和 `Dictionary<string, List<string>>`
         */
        public Dictionary<string, string> textInfobox { get; }
        public Dictionary<string, List<string>> listInfoBox { get; }
        public int volumes { get; }
        public int eps { get; }
        public int totalEpisodes { get; }
        public BangumiRating rating { get; }
        public Dictionary<string, int> collection { get; }
        public List<BangumiTag> tags { get; }

        public BangumiSubject(uint id = default, SubjectType type = default, string name = null,
            string nameCn = null, string summary = null, bool nsfw = default, bool locked = default,
            string date = null, string platform = null, Dictionary<string, string> images = null,
            Dictionary<string, string> textInfobox = null, Dictionary<string, List<string>> listInfoBox = null,
            int volumes = default, int eps = default, int totalEpisodes = default, BangumiRating rating = null,
            Dictionary<string, int> collection = default, List<BangumiTag> tags = null)
        {
            this.id = id;
            this.type = type;
            this.name = name;
            this.nameCn = nameCn;
            this.summary = summary;
            this.nsfw = nsfw;
            this.locked = locked;
            this.date = date;
            this.platform = platform;
            this.images = images;
            this.textInfobox = textInfobox;
            this.listInfoBox = listInfoBox;
            this.volumes = volumes;
            this.eps = eps;
            this.totalEpisodes = totalEpisodes;
            this.rating = rating;
            this.collection = collection;
            this.tags = tags;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

}