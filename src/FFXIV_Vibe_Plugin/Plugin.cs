using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using FFXIV_Vibe_Plugin.App;

namespace FFXIV_Vibe_Plugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    public string Name => "FFXIV Vibe Plugin";

    public WindowSystem WindowSystem = new("SamplePlugin");

    public static readonly string ShortName = "FVP";
    public readonly string CommandName = "/fvp";

    public Plugin()
    {
        PluginInterface.Create<Service>();
        Service.Plugin = this;
        Service.InitializeService();

        Service.App = PluginInterface.Create<Main>(CommandName, ShortName)!;

        WindowSystem.AddWindow(Service.App.PluginUi);

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A vibe plugin for fun..."
        });
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        Service.CommandManager.RemoveHandler(CommandName);
        Service.App.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        Service.App.OnCommand(command, args);
    }

    private void DrawUI()
    {
        WindowSystem.Draw();

        if (Service.App == null)
            return;

        Service.App.DrawUI();
    }

    public void DrawConfigUI()
    {
        WindowSystem.Windows[0].IsOpen = !WindowSystem.Windows[0].IsOpen;
    }
}
