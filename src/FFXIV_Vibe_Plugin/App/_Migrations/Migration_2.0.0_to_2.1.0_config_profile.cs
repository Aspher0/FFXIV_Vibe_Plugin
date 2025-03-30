using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;

#nullable enable
namespace FFXIV_Vibe_Plugin.Migrations
{
    internal class Migration
    {
        public bool Patch_0_2_0_to_1_0_0_config_profile()
        {
            int num = 0;

            if (Service.Configuration!.Version != num || Service.Configuration! == null)
                return false;

            ConfigurationProfile configurationProfile = new ConfigurationProfile()
            {
                Name = "Default (auto-migration from v0.2.0 to v1.0.0)",
                VERBOSE_SPELL = Service.Configuration!.VERBOSE_SPELL,
                VERBOSE_CHAT = Service.Configuration!.VERBOSE_CHAT,
                VIBE_HP_TOGGLE = Service.Configuration!.VIBE_HP_TOGGLE,
                VIBE_HP_MODE = Service.Configuration!.VIBE_HP_MODE,
                MAX_VIBE_THRESHOLD = Service.Configuration!.MAX_VIBE_THRESHOLD,
                AUTO_CONNECT = Service.Configuration!.AUTO_CONNECT,
                AUTO_OPEN = Service.Configuration!.AUTO_OPEN,
                PatternList = Service.Configuration!.PatternList,
                BUTTPLUG_SERVER_HOST = Service.Configuration!.BUTTPLUG_SERVER_HOST,
                BUTTPLUG_SERVER_PORT = Service.Configuration!.BUTTPLUG_SERVER_PORT,
                TRIGGERS = Service.Configuration!.TRIGGERS,
                VISITED_DEVICES = Service.Configuration!.VISITED_DEVICES
            };
            Service.Configuration!.Version = num + 1;
            Service.Configuration!.CurrentProfileName = configurationProfile.Name;
            Service.Configuration!.Profiles.Add(configurationProfile);
            Service.Configuration!.Save();
            Logger.Warn("Migration from 2.0.0 to 2.1.0 using profiles done successfully");
            return true;
        }
    }
}
