using Dalamud.Game.ClientState.Objects;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace FFXIV_Vibe_Plugin.App;

public class Service
{
    [PluginService] public static IChatGui DalamudChat { get; private set; } = null!;
    [PluginService] public static IGameNetwork GameNetwork { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ISigScanner Scanner { get; private set; } = null!;
    [PluginService] public static IObjectTable GameObjects { get; private set; } = null!;
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IPartyList PartyList { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider InteropProvider { get; private set; } = null!;
    [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    public static Plugin Plugin { get; set; }
    public static Main App { get; set; }
    public static Configuration? Configuration { get; set; }
    public static IPlayerCharacter? ConnectedPlayerObject { get; set; }

    public static void InitializeService()
    {
        InitializeConfig();
        GetConnectedPlayer();
    }

    public static void InitializeConfig()
    {
        Configuration = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Save();
    }

    public async static void GetConnectedPlayer()
    {
        ConnectedPlayerObject = await GetPlayerCharacterAsync();
    }

    public static void ClearConnectedPlayer()
    {
        ConnectedPlayerObject = null;
    }

    public static void Dispose()
    {
        ClearConnectedPlayer();
    }

    public async static Task<IPlayerCharacter?> GetPlayerCharacterAsync() => await Framework.RunOnFrameworkThread(() => ClientState!.LocalPlayer);
}
