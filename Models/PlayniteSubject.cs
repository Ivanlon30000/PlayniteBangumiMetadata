using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Bangumi.Models
{
    public class PlayniteSubject
    {
        private readonly BangumiSubject bangumiSubject;
        private readonly BangumiSettings settings;

        public PlayniteSubject(BangumiSubject bangumiSubject, BangumiSettings settings )
        {
            this.bangumiSubject = bangumiSubject;
            this.settings = settings;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
        
        /*
         * getters
         */
        public string Name => FormatName(bangumiSubject, settings);
        // {
        //     get
        //     {
        //         return (string.IsNullOrEmpty(bangumiSubject.nameCn)
        //                 ? settings.NameFormatNameCnNotExists
        //                 : settings.NameFormatNameCnExists)
        //             .Replace("%name%", bangumiSubject.name)
        //             .Replace("%name_cn%", bangumiSubject.nameCn)
        //             .Replace("%id%", bangumiSubject.id.ToString())
        //             .Replace("%%", "%");
        //     }
        // }

        public static string FormatName(BangumiSubject subject, BangumiSettings settings)
        {
            return (string.IsNullOrEmpty(subject.nameCn)
                    ? settings.NameFormatNameCnNotExists
                    : settings.NameFormatNameCnExists)
                .Replace("%name%", subject.name)
                .Replace("%name_cn%", subject.nameCn)
                .Replace("%id%", subject.id.ToString())
                .Replace("%%", "%");
        }

        private string description;
        public string Description
        {
            get
            {
                if (description == null)
                {
                    description = bangumiSubject.summary
                        .Replace("\r", "")
                        .Replace("\n", "<br>\n");
                }

                return description;
            }
        }
        
        public int CommunityScore
        {
            get
            {
                if (bangumiSubject.rating != null && bangumiSubject.rating.score > 0)
                {
                    return (int)(bangumiSubject.rating.score * 10);
                }

                return 0;
            }
        }


        private MetadataFile coverImage;
        public MetadataFile CoverImage
        {
            get
            {
                if (bangumiSubject.images != null)
                {
                    if (coverImage == null)
                    {
                        coverImage = new MetadataFile(bangumiSubject.images["large"]);
                    }

                    return coverImage;
                }
                
                return null;
            }
        }

        public MetadataFile BackgroundImage
        {
            get
            {
                if (settings.CoverImageAsBackgroundImage && CoverImage != null)
                {
                    return CoverImage;
                }

                return null;
            }
        }

        private List<MetadataProperty> developers;
        public List<MetadataProperty> Developers
        {
            get
            {
                if (developers == null)
                {
                    developers = new List<MetadataProperty>(bangumiSubject.textInfobox
                        .Where(pair =>  pair.Key.Equals("??????") ||pair.Key.Equals("??????") || pair.Key.Contains("?????????") 
                                        || pair.Key.ToLower().Contains("developer"))
                        .Select(pair => new MetadataNameProperty(pair.Value)));
                }

                return developers;
            }
        }

        private List<MetadataProperty> genres;
        public List<MetadataProperty> Genres
        {
            get
            {
                if (genres == null)
                {
                    genres = new List<MetadataProperty>();
                    foreach (var pair in bangumiSubject.textInfobox.Where(pair => pair.Key.Equals("????????????")))
                    {
                        genres.AddRange(pair.Value
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => new MetadataNameProperty(s.Trim()))
                        );
                        break;
                    }
                }

                return genres;
            }
        }

        private List<Link> links;

        public List<Link> Links
        {
            get
            {
                if (links == null)
                {
                    links = new List<Link> { new Link("Bangumi", $"https://bgm.tv/subject/{bangumiSubject.id}") };
                    if (bangumiSubject.textInfobox.ContainsKey("website"))
                    {
                        links.Add(new Link("????????????", bangumiSubject.textInfobox["website"]));
                    }
                }

                return links;
            }
        }
        
        private List<MetadataNameProperty> publisher;
        public List<MetadataNameProperty> Publishers
        {
            get
            {
                if (publisher == null)
                {
                    publisher = new List<MetadataNameProperty>(bangumiSubject.textInfobox
                        .Where(pair => pair.Key.Contains("?????????") || pair.Key.ToLower().Contains("publisher"))
                        .Select(pair => new MetadataNameProperty(pair.Value)));
                }

                return publisher;
            }
        }
        
        public ReleaseDate ReleaseDate
        {
            get
            {
                if (!string.IsNullOrEmpty(bangumiSubject.date))
                {
                    return new ReleaseDate(DateTime.Parse(bangumiSubject.date));
                }

                return ReleaseDate.Empty;
            }
        }

        private List<MetadataProperty> tags;
        public List<MetadataProperty> Tags
        {
            get
            {
                if (tags == null)
                {
                    tags = new List<MetadataProperty>(
                        bangumiSubject.tags
                            .Where(tag => tag.count >= settings.TagThres)
                            // .Where(tag => tag.count >= int.Parse(settings.TagThres))
                            .Select(tag => new MetadataNameProperty(tag.name))
                    );
                }

                return tags;
            }
        }

        private List<MetadataProperty> ageRating;

        public List<MetadataProperty> AgeRating
        {
            get
            {
                if (ageRating == null)
                {
                    ageRating = new List<MetadataProperty>();
                    if (bangumiSubject.nsfw && !string.IsNullOrEmpty(settings.NsfwTag))
                    {
                        ageRating.AddRange(settings.NsfwTag
                            .Split(new []{','})
                            .Select(s => new MetadataNameProperty(s.Trim())));
                    }
                    if (!bangumiSubject.nsfw && !string.IsNullOrEmpty(settings.SfwTag))
                    {
                        ageRating.AddRange(settings.SfwTag
                            .Split(new []{','})
                            .Select(s => new MetadataNameProperty(s.Trim())));
                    }
                }
                return ageRating;
            }
        }

        private List<MetadataProperty> platform;
        public List<MetadataProperty> Platform
        {
            get
            {
                if (platform == null)
                {
                    platform = new List<MetadataProperty>();
                    if (bangumiSubject.textInfobox.ContainsKey("??????"))
                    {
                        platform.Add(new MetadataNameProperty(bangumiSubject.textInfobox["??????"]));
                    }

                    if (bangumiSubject.listInfoBox.ContainsKey("??????"))
                    {
                        platform.AddRange(bangumiSubject.listInfoBox["??????"].Select(s => new MetadataNameProperty(s)));
                    }
                }

                return platform;
            }
        }

    }
}