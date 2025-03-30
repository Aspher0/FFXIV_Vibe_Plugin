using Dalamud.Configuration;
using Dalamud.Plugin;
using FFXIV_Vibe_Plugin.Triggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace FFXIV_Vibe_Plugin.App
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public string CurrentProfileName = "Default";
        public List<ConfigurationProfile> Profiles = new List<ConfigurationProfile>();
        public bool VERBOSE_SPELL;
        public bool VERBOSE_CHAT;
        public List<Pattern> PatternList = new List<Pattern>();
        public string EXPORT_DIR = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\FFXIV_Vibe_Plugin";
        public Dictionary<string, Device.Device> VISITED_DEVICES = new Dictionary<string, Device.Device>();

        public int Version { get; set; }

        public bool VIBE_HP_TOGGLE { get; set; }

        public int VIBE_HP_MODE { get; set; }

        public int MAX_VIBE_THRESHOLD { get; set; } = 100;

        public bool AUTO_CONNECT { get; set; } = true;

        public bool AUTO_OPEN { get; set; }

        public string BUTTPLUG_SERVER_HOST { get; set; } = "127.0.0.1";

        public int BUTTPLUG_SERVER_PORT { get; set; } = 12345;

        public bool BUTTPLUG_SERVER_SHOULD_WSS { get; set; }

        public List<Trigger> TRIGGERS { get; set; } = new List<Trigger>();

        public string PREMIUM_TOKEN { get; set; } = "";

        public string PREMIUM_TOKEN_SECRET { get; set; } = "";

        //public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true; // Sample Plugin

        public void Initialize()
        {
            try
            {
                Directory.CreateDirectory(EXPORT_DIR);
            }
            catch
            {
            }
        }

        public void Save()
        {
            Service.PluginInterface.SavePluginConfig(this);
        }

        public ConfigurationProfile? GetProfile(string name = "")
        {
            if (name == "")
                name = CurrentProfileName;
            return Profiles.Find(i => i.Name == name);
        }

        public ConfigurationProfile GetDefaultProfile()
        {
            var name = "Default profile";
            ConfigurationProfile configurationProfile = GetProfile(CurrentProfileName) ?? GetProfile(name);
            ConfigurationProfile defaultProfile = configurationProfile ?? new ConfigurationProfile();
            if (configurationProfile == null)
            {
                defaultProfile.Name = name;
                Profiles.Add(defaultProfile);
                CurrentProfileName = name;
                Save();
            }
            return defaultProfile;
        }
        public ConfigurationProfile? GetFirstProfile()
        {
            var firstProfile = (ConfigurationProfile)null;
            if (firstProfile == null && Profiles.Count > 0)
                firstProfile = Profiles[0];
            return firstProfile;
        }

        public void RemoveProfile(string name)
        {
            ConfigurationProfile profile = GetProfile(name);
            if (profile == null)
                return;
            Profiles.Remove(profile);
        }

        public bool AddProfile(string name)
        {
            if (GetProfile(name) != null)
                return false;
            Profiles.Add(new ConfigurationProfile()
            {
                Name = name
            });
            return true;
        }

        public bool SetCurrentProfile(string name)
        {
            ConfigurationProfile profile = GetProfile(name);
            if (profile == null)
                return false;
            CurrentProfileName = profile.Name;
            return true;
        }
    }
}
