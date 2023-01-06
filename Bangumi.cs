using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Bangumi.Services;

namespace Bangumi
{
    public class Bangumi : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        public ILogger Logger => logger;

        private BangumiSettingsViewModel settings { get; set; }
        public BangumiSettings Settings => settings.Settings;

        public BangumiMetadataService Service { get; }

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
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
            Service = new BangumiMetadataService(settings.Settings.AccessToken);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
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