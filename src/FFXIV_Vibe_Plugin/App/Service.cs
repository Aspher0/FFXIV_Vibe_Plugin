using Dalamud.Game.ClientState.Objects.SubKinds;
using NoireLib;

namespace FFXIV_Vibe_Plugin.App;

public class Service
{
    public static Plugin Plugin { get; set; }
    public static Main App { get; set; }
    public static Configuration? Configuration { get; set; }
    public static IPlayerCharacter? ConnectedPlayerObject { get; set; }

    public static void InitializeService()
    {
        InitializeConfig();
        GetConnectedPlayer();

        NoireService.ClientState.Login += OnLogin;
        NoireService.ClientState.Logout += OnLogout;
    }

    private static void OnLogout(int type, int code)
    {
        ClearConnectedPlayer();
    }

    private static void OnLogin()
    {
        GetConnectedPlayer();
    }

    public static void InitializeConfig()
    {
        Configuration = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Save();
    }

    public static void GetConnectedPlayer()
    {
        NoireService.Framework.RunOnFrameworkThread(() =>
        {
            ConnectedPlayerObject = NoireService.ObjectTable.LocalPlayer;
        });
    }

    public static void ClearConnectedPlayer()
    {
        ConnectedPlayerObject = null;
    }

    public static void Dispose()
    {
        NoireService.CommandManager.RemoveHandler(Plugin.CommandName);
        App.Dispose();

        NoireService.ClientState.Login -= OnLogin;
        NoireService.ClientState.Logout -= OnLogout;

        ClearConnectedPlayer();
    }
}
