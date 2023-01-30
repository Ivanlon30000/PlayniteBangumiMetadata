using System;
using Playnite.SDK.Plugins;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bangumi.Models;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Bangumi
{
    public class BangumiProvider : OnDemandMetadataProvider
    {
        private ILogger logger;

        private MetadataRequestOptions options;
        private Bangumi plugin;
        
        private List<MetadataField> availableFields;
        private PlayniteSubject subject;
        public override List<MetadataField> AvailableFields
        {
            get
            {
                if (availableFields == null)
                {
                    availableFields = GetAvailableFields();
                }

                return availableFields;
            }
        }

        public BangumiProvider(MetadataRequestOptions options, Bangumi plugin)
        {
            this.options = options;
            this.plugin = plugin;
            this.logger = plugin.Logger;
            
            logger.Debug("new BangumiProvider");
        }

        private List<MetadataField> GetAvailableFields()
        {
            logger.Debug("invoke GetAvailableFields");
            List<MetadataField> metadataFields = new List<MetadataField>();
            if (GetBangumiMetadata() && subject !=null)
            {
                logger.Debug(subject.ToString());
                if (!string.IsNullOrEmpty(subject.Name))
                {
                    metadataFields.Add(MetadataField.Name);
                }                
                if (!string.IsNullOrEmpty(subject.Description))
                {
                    metadataFields.Add(MetadataField.Description);
                }             
                if (subject.CommunityScore > 0)
                {
                    metadataFields.Add(MetadataField.CommunityScore);
                }
                if (subject.CoverImage != null)
                {
                    metadataFields.Add(MetadataField.CoverImage);
                }
                if (subject.BackgroundImage != null)
                {
                    metadataFields.Add(MetadataField.BackgroundImage);
                }
                if (subject.Developers != null && subject.Developers.Count > 0)
                {
                    metadataFields.Add(MetadataField.Developers);
                }
                if (subject.Genres != null && subject.Genres.Count > 0)
                {
                    metadataFields.Add(MetadataField.Genres);
                }
                if (subject.Links != null && subject.Links.Count > 0)
                {
                    metadataFields.Add(MetadataField.Links);
                }
                if (subject.Publishers != null && subject.Publishers.Count > 0)
                {
                    metadataFields.Add(MetadataField.Publishers);
                }
                if (subject.ReleaseDate != ReleaseDate.Empty)
                {
                    metadataFields.Add(MetadataField.ReleaseDate);
                }
                if (subject.Tags != null && subject.Tags.Count > 0)
                {
                    metadataFields.Add(MetadataField.Tags);
                }                
                if (subject.AgeRating != null && subject.AgeRating.Count > 0)
                {
                    metadataFields.Add(MetadataField.AgeRating);
                }
                if (subject.Platform != null && subject.Platform.Count > 0)
                {
                    metadataFields.Add(MetadataField.Platform);
                }
            }
            logger.Debug($"available fields:{string.Join(",", metadataFields)}");
            return metadataFields;
        }

        /*
         * 根据名称 `options.GameData.Name` 获取 metadata；
         * 获取成功返回 true
         */
        private bool GetBangumiMetadata()
        {
            logger.Debug("invoke GetBangumiMetadata");
            if (subject != null)
            {
                logger.Debug($@"Subject not null: {subject}");
                return true;
            }

            if (options.IsBackgroundDownload)
            {
                logger.Debug("Background download");
                return false;
            }

            string keyword = options.GameData.Name;
            string pattern = plugin.Settings.NameFormatPattern;
            
            // 直接解析为bangumi id
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
                // 成功解析id
                logger.Debug($"match subject with id {id}");
            }
            else
            {
                // 未成功解析id，打开搜索窗口
                SearchOption item = ((SearchOption)plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(null,
                    searchKeyword =>
                    {
                        List<GenericItemOption> itemOptions = new List<GenericItemOption>();
                        foreach (var bangumiSubject in plugin.Service.Search(searchKeyword))
                        {
                            string optionName = PlayniteSubject.FormatName(bangumiSubject, plugin.Settings);
                            string optionDescription = bangumiSubject.summary
                                .Replace("\r", "")
                                .Replace("\n", "");
                            if (optionDescription.Length > 100)
                            {
                                optionDescription = optionDescription.Substring(0, 100) + "...";
                            }
                            itemOptions.Add(new SearchOption(bangumiSubject.id, optionName, optionDescription));
                        }

                        return itemOptions;
                    }, 
                    options.GameData.Name, "搜索"));
            
                if (item != null)
                {
                    logger.Debug($@"selected item: {item}");
                    id = item.Id;
                }
            }

            if (id > 0)
            {
                subject = new PlayniteSubject(plugin.Service.GetSubjectById(id), plugin.Settings);
            }
            
            logger.Debug($@"subject is {subject}, GetBangumiMetadata returned");
            return subject != null;
        }
        
        // Override additional methods based on supported metadata fields.
        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            // TODO 根据图片大小过滤背景
            // logger.Debug("invoke GetBackgroundImage");
            if (AvailableFields.Contains(MetadataField.BackgroundImage))
            {
                return subject.BackgroundImage;
            }
            return base.GetBackgroundImage(args);
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            // logger.Debug("invoke GetCommunityScore");
            if (AvailableFields.Contains(MetadataField.CommunityScore))
            {
                return subject.CommunityScore;
            }
            return base.GetCommunityScore(args);
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetCoverImage");
            if (AvailableFields.Contains(MetadataField.CoverImage))
            {
                return subject.CoverImage;
            }
            return base.GetCoverImage(args);
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetDescription");
            if (AvailableFields.Contains(MetadataField.Description))
            {
                return subject.Description;
            }
            return base.GetDescription(args);
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetDevelopers");
            if (AvailableFields.Contains(MetadataField.Developers))
            {
                return subject.Developers;
            }
            return base.GetDevelopers(args);
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetLinks");
            if (AvailableFields.Contains(MetadataField.Links))
            {
                return subject.Links;
            }

            return base.GetLinks(args);
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetName");
            if (AvailableFields.Contains(MetadataField.Name))
            {
                return subject.Name;
            }
            return base.GetName(args);
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetPublishers");
            if (AvailableFields.Contains(MetadataField.Publishers))
            {
                return subject.Publishers;
            }
            return base.GetPublishers(args);
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetReleaseDate");
            if (AvailableFields.Contains(MetadataField.ReleaseDate))
            {
                return subject.ReleaseDate;
            }
            return base.GetReleaseDate(args);
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetTags");
            if (AvailableFields.Contains(MetadataField.Tags))
            {
                return subject.Tags;
            }
            return base.GetTags(args);
        }

        public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetAgeRatings");
            if (AvailableFields.Contains(MetadataField.AgeRating))
            {
                return subject.AgeRating;
            }
            return base.GetAgeRatings(args);
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetPlatforms");
            if (AvailableFields.Contains(MetadataField.Platform))
            {
                return subject.Platform;
            }
            return base.GetPlatforms(args);
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            logger.Debug("invoke GetGenres");
            if (AvailableFields.Contains(MetadataField.Genres))
            {
                return subject.Genres;
            }
            return base.GetGenres(args);
        }
    }
}