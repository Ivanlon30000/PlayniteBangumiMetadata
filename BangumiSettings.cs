using System;
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;


namespace Bangumi
{
    public class BangumiSettings : ObservableObject
    {
        // Acess Token
        public string AccessToken { get; set; } = string.Empty;
        
        // Metadata开关
        public bool EnableCommunityScore { get; set;} = true;
        public bool EnableCoverImage { get; set;} = true;
        public bool EnableDescription { get; set;} = true;
        public bool EnableDevelopers { get; set;} = true;
        public bool EnableGenres { get; set;} = true;
        public bool EnableLinks { get; set;} = true;
        public bool EnableName { get; set;} = true;
        public bool EnablePublishers { get; set;} = true;
        public bool EnableReleaseDate { get; set;} = true;
        public bool EnableTags { get; set;} = true;
        public bool EnableBackgroundImage { get; set;} = true;
        public bool EnableAgeRating { get; set;} = true;
        public bool EnablePlatform { get; set;} = true;

        // Tag
        public int TagThres { get; set; } = 50;

        public bool CoverImageAsBackgroundImage { get; set; } = true;

        public string NameFormatNameCnExists { get; set; } = "%name% (%name_cn%)";
        
        public string NameFormatNameCnNotExists { get; set; } = "%name%";

        public string NameFormatPattern { get; set; } = @"^(?<id>\d+)$";

        public string NsfwTag { get; set; } = string.Empty;
        public string SfwTag { get; set; } = string.Empty;
        
        public bool EnableDebug { get; set; } = false;

        [DontSerialize] 
        public string AccessTokenStatusMessage { get; set; } = string.Empty;

        [DontSerialize] 
        public string VersionString => $"Ver. {Bangumi.VERSION}";


        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        // [DontSerialize]
        // public bool OptionThatWontBeSaved { get => optionThatWontBeSaved; set => SetValue(ref optionThatWontBeSaved, value); }
    }

    public class BangumiSettingsViewModel : ObservableObject, ISettings
    {
        private Bangumi plugin;
        private BangumiSettings editingClone { get; set; }

        private BangumiSettings settings;

        public BangumiSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        private ILogger logger;

        public BangumiSettingsViewModel(Bangumi plugin)
        {
            // Injecting your plugin instance is required for Save/Load method
            // because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;
            this.logger = plugin.Logger;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<BangumiSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new BangumiSettings();
            }
        }
        
        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            Dictionary<string, string> me = null;
            if (!String.IsNullOrEmpty(settings.AccessToken))
            {
                me = plugin.Service.GetMe();
            }
            UpdateLoginStatus(me);
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();

            if (String.IsNullOrEmpty(Settings.AccessToken))
            {
                return true;
            }

            var me = plugin.Service.GetMe();
            UpdateLoginStatus(me);
            if (me == null)
            {
                errors.Add("使用Access Token认证失败");
                return false;
            }

            return true;
        }

        private void UpdateLoginStatus(Dictionary<string, string> me)
        {
            if (String.IsNullOrEmpty(settings.AccessToken))
            {
                Settings.AccessTokenStatusMessage = "未登录，部分条目搜索不到/无法获取详情";
            }
            else
            {
                Settings.AccessTokenStatusMessage = 
                    me != null 
                    ? $"当前登录：{me["nickname"]}({me["username"]})" 
                    : "访问Bangumi API失败，请检查Access Token并重启Playnite";
            }
        }
    }
}