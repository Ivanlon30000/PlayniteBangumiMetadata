﻿using System;
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
                    availableFields = UpdateAvailableFields();
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

        private List<MetadataField> UpdateAvailableFields()
        {
            logger.Debug("invoke GetAvailableFields");
            List<MetadataField> metadataFields = new List<MetadataField>();
            if (UpdateSubject() && subject !=null)
            {
                logger.Debug(subject.ToString());
                if (plugin.Settings.EnableName && !string.IsNullOrEmpty(subject.Name))
                {
                    metadataFields.Add(MetadataField.Name);
                }                
                if (plugin.Settings.EnableDescription && !string.IsNullOrEmpty(subject.Description))
                {
                    metadataFields.Add(MetadataField.Description);
                }             
                if (plugin.Settings.EnableCommunityScore && subject.CommunityScore > 0)
                {
                    metadataFields.Add(MetadataField.CommunityScore);
                }
                if (plugin.Settings.EnableCoverImage && subject.CoverImage != null)
                {
                    metadataFields.Add(MetadataField.CoverImage);
                }
                if (plugin.Settings.EnableBackgroundImage && subject.BackgroundImage != null)
                {
                    metadataFields.Add(MetadataField.BackgroundImage);
                }
                if (plugin.Settings.EnableDevelopers && subject.Developers != null && subject.Developers.Count > 0)
                {
                    metadataFields.Add(MetadataField.Developers);
                }
                if (plugin.Settings.EnableGenres && subject.Genres != null && subject.Genres.Count > 0)
                {
                    metadataFields.Add(MetadataField.Genres);
                }
                if (plugin.Settings.EnableLinks && subject.Links != null && subject.Links.Count > 0)
                {
                    metadataFields.Add(MetadataField.Links);
                }
                if (plugin.Settings.EnablePublishers && subject.Publishers != null && subject.Publishers.Count > 0)
                {
                    metadataFields.Add(MetadataField.Publishers);
                }
                if (plugin.Settings.EnableReleaseDate && subject.ReleaseDate != ReleaseDate.Empty)
                {
                    metadataFields.Add(MetadataField.ReleaseDate);
                }
                if (plugin.Settings.EnableTags && subject.Tags != null && subject.Tags.Count > 0)
                {
                    metadataFields.Add(MetadataField.Tags);
                }                
                if (plugin.Settings.EnableAgeRating && subject.AgeRating != null && subject.AgeRating.Count > 0)
                {
                    metadataFields.Add(MetadataField.AgeRating);
                }
                if (plugin.Settings.EnablePlatform && subject.Platform != null && subject.Platform.Count > 0)
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
        private bool UpdateSubject()
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
                BangumiSubject bangumiSubject = plugin.Service.GetSubjectById(id);
                if (bangumiSubject != null)
                {
                    subject = new PlayniteSubject(bangumiSubject, plugin.Settings);
                } 
                else
                {
                    plugin.PlayniteApi.Dialogs.ShowErrorMessage(
                        "可能是AccessKey未配置或配置错误导致的，此时能够搜索到条目但是无法获取详细信息。",
                        "获取条目详情失败");
                }
            }
            
            logger.Debug($@"subject is {subject}, GetBangumiMetadata returned");
            return subject != null;
        }
        
        // Override additional methods based on supported metadata fields.
        public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
        {
            // TODO 根据图片大小过滤背景
            if (AvailableFields.Contains(MetadataField.BackgroundImage))
            {
                return subject.BackgroundImage;
            }
            return base.GetBackgroundImage(args);
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.CommunityScore))
            {
                return subject.CommunityScore;
            }
            return base.GetCommunityScore(args);
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.CoverImage))
            {
                return subject.CoverImage;
            }
            return base.GetCoverImage(args);
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Description))
            {
                return subject.Description;
            }
            return base.GetDescription(args);
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Developers))
            {
                return subject.Developers;
            }
            return base.GetDevelopers(args);
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Links))
            {
                return subject.Links;
            }

            return base.GetLinks(args);
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Name))
            {
                return subject.Name;
            }
            return base.GetName(args);
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Publishers))
            {
                return subject.Publishers;
            }
            return base.GetPublishers(args);
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.ReleaseDate))
            {
                return subject.ReleaseDate;
            }
            return base.GetReleaseDate(args);
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Tags))
            {
                return subject.Tags;
            }
            return base.GetTags(args);
        }

        public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.AgeRating))
            {
                return subject.AgeRating;
            }
            return base.GetAgeRatings(args);
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Platform))
            {
                return subject.Platform;
            }
            return base.GetPlatforms(args);
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Genres))
            {
                return subject.Genres;
            }
            return base.GetGenres(args);
        }
    }
}