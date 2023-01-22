using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Bangumi.Services;
using Bangumi.Utils;

namespace Bangumi
{
    public class Bangumi : MetadataPlugin
    {
        public static readonly string VERSION = "1.1.0";
        private ILogger logger;
        private BangumiSettingsViewModel settings { get; set; }
        
        
        public BangumiSettings Settings => settings.Settings;
        public BangumiMetadataService Service { get; }
        public ILogger Logger => logger;
        
        

        public override Guid Id { get; } = Guid.Parse("fea02b9a-ab77-47e3-8fb1-6512c9261fbe");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.CommunityScore,
            MetadataField.CoverImage,
            MetadataField.Description,
            MetadataField.Developers,
            MetadataField.Genres,
            MetadataField.Links,
            MetadataField.Name,
            MetadataField.Publishers,
            MetadataField.ReleaseDate,
            MetadataField.Tags,
            MetadataField.BackgroundImage,
            MetadataField.AgeRating,
            MetadataField.Platform
            // Include addition fields if supported by the metadata source
        };

        // Change to something more appropriate
        public override string Name => "Bangumi";

        public Bangumi(IPlayniteAPI api) : base(api)
        {
            settings = new BangumiSettingsViewModel(this);
            logger = new Logger(LogManager.GetLogger(), Settings.EnableDebug);
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
            Service = new BangumiMetadataService(this);
            
            logger.Info($"Bangumi Initialization. Ver.{VERSION}");
            logger.Info($"Settings:");
            logger.Info($"\tAccessToken: (hidden)({Settings.AccessToken.Length})");
            logger.Info($"\tTagThresh: {Settings.TagThres}");
            logger.Info($"\tCoverImageAsBackgroundImage: {Settings.CoverImageAsBackgroundImage}");
            logger.Info($"\tNameFormatNameCnExists: {Settings.NameFormatNameCnExists}");
            logger.Info($"\tNameFormatNameCnNotExists: {Settings.NameFormatNameCnNotExists}");
            logger.Info($"\tNameFormatPattern: {Settings.NameFormatPattern}");
            logger.Info($"\tNsfwTag: {Settings.NsfwTag}");
            logger.Info($"\tSfwTag: {Settings.SfwTag}");
            logger.Info($"\tEnableDebug: {Settings.EnableDebug}");
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            logger.Debug($@"invoke GetMetadataProvider:{options}");
            return new BangumiProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new BangumiSettingsView();
        }
    }
}