﻿using System;
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;


namespace Bangumi
{
    public class BangumiSettings : ObservableObject
    {
        public string AccessToken { get; set; } = string.Empty;

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
        

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        // [DontSerialize]
        // public bool OptionThatWontBeSaved { get => optionThatWontBeSaved; set => SetValue(ref optionThatWontBeSaved, value); }
    }

    public class BangumiSettingsViewModel : ObservableObject, ISettings
    {
        private readonly Bangumi plugin;
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

        public BangumiSettingsViewModel(Bangumi plugin)
        {
            // Injecting your plugin instance is required for Save/Load method
            // because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;
            
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

        private Dictionary<string, string> info;
        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            if (String.IsNullOrEmpty(settings.AccessToken))
            {
                Settings.AccessTokenStatusMessage = "未登录，部分条目搜索不到";
            }
            else
            {
                if (info == null)
                {
                    info = plugin.Service.GetMe();
                }
                Settings.AccessTokenStatusMessage = 
                    info != null 
                    ? $"当前登录：{info["nickname"]}({info["username"]})" 
                    : "访问Bangumi API失败，请检查Access Token";
            }
            
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
            return true;
        }
    }
}