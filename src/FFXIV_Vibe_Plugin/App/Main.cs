using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Device;
using FFXIV_Vibe_Plugin.Experimental;
using FFXIV_Vibe_Plugin.Hooks;
using FFXIV_Vibe_Plugin.Migrations;
using FFXIV_Vibe_Plugin.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FFXIV_Vibe_Plugin;

public class Main
{
    public readonly string CommandName = "";
    public PluginUI PluginUi { get; init; }

    private bool ThreadMonitorPartyListRunning = true;
    private readonly bool wasInit;
    private readonly string ShortName = "";
    private bool _firstUpdated;
    private readonly PlayerStats PlayerStats;
    private ConfigurationProfile ConfigurationProfile;
    private readonly ActionEffect hook_ActionEffect;
    private readonly DevicesController DeviceController;
    private readonly TriggersController TriggersController;
    private readonly Patterns Patterns;
    private readonly NetworkCapture experiment_networkCapture;

    public Main(string commandName, string shortName)
    {
        CommandName = commandName;
        ShortName = shortName;

        Service.DalamudChat.ChatMessage += new IChatGui.OnMessageDelegate(ChatWasTriggered);

        Migration migration = new Migration();

        ConfigurationProfile = Service.Configuration!.GetDefaultProfile();
        Patterns = new Patterns();
        Patterns.SetCustomPatterns(ConfigurationProfile.PatternList);
        DeviceController = new DevicesController(ConfigurationProfile, Patterns);

        if (ConfigurationProfile.AUTO_CONNECT)
            new Thread((() =>
            {
                Thread.Sleep(2000);
                Service.App.Command_DeviceController_Connect();
            })).Start();

        hook_ActionEffect = new ActionEffect();

        hook_ActionEffect.ReceivedEvent += new EventHandler<HookActionEffects_ReceivedEventArgs>(SpellWasTriggered);

        Service.ClientState.Login += new Action(ClientState_LoginEvent);

        PlayerStats = new PlayerStats();

        PlayerStats.Event_CurrentHpChanged += new EventHandler(PlayerCurrentHPChanged);

        PlayerStats.Event_MaxHpChanged += new EventHandler(PlayerCurrentHPChanged);

        TriggersController = new TriggersController(PlayerStats, ConfigurationProfile);

        PluginUi = new PluginUI(ConfigurationProfile, DeviceController, TriggersController, Patterns);

        experiment_networkCapture = new NetworkCapture();

        SetProfile(Service.Configuration.CurrentProfileName);

        if (Service.PartyList != null)
            new Thread(() => MonitorPartyList(Service.PartyList)).Start();
        else
            Logger.Error("PAS DE SERVICE");

        wasInit = true;
    }

    public void Dispose()
    {
        Logger.Debug("Disposing plugin...");

        if (!wasInit)
            return;

        if (DeviceController != null)
        {
            try
            {
                DeviceController.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error("App.Dispose: " + ex.Message);
            }
        }

        Service.DalamudChat.ChatMessage -= new IChatGui.OnMessageDelegate(ChatWasTriggered);

        hook_ActionEffect.Dispose();
        PluginUi.Dispose();
        experiment_networkCapture.Dispose();

        Logger.Debug("Plugin disposed!");

        ThreadMonitorPartyListRunning = false;
    }

    public static string GetHelp(string command) => $"Usage:\n      {command} config      \n      {command} connect\n      {command} disconnect\n      {command} send <0-100> # Send vibe intensity to all toys\n      {command} stop\n";

    public void OnCommand(string command, string args)
    {
        if (args.Length == 0)
            DisplayUI();
        else if (args.StartsWith("help"))
            Logger.Chat(GetHelp("/" + ShortName));
        else if (args.StartsWith("config"))
            DisplayConfigUI();
        else if (args.StartsWith("connect"))
            Command_DeviceController_Connect();
        else if (args.StartsWith("disconnect"))
            Command_DeviceController_Disconnect();
        else if (args.StartsWith("send"))
            Command_SendIntensity(args);
        else if (args.StartsWith("stop"))
            DeviceController.SendVibeToAll(0);
        else if (args.StartsWith("profile"))
            Command_ProfileSet(args);
        else if (args.StartsWith("exp_network_start"))
            experiment_networkCapture.StartNetworkCapture();
        else if (args.StartsWith("exp_network_stop"))
            experiment_networkCapture.StopNetworkCapture();
        else
            Logger.Chat("Unknown subcommand: " + args);
    }

    private void FirstUpdated()
    {
        Logger.Debug("First updated");

        if (ConfigurationProfile == null || !ConfigurationProfile.AUTO_OPEN)
            return;

        this.DisplayUI();
    }

    private void DisplayUI() => Service.Plugin.DrawConfigUI();

    private void DisplayConfigUI() => Service.Plugin.DrawConfigUI();

    public void DrawUI()
    {
        if (PluginUi == null)
            return;

        if (Service.ClientState.IsLoggedIn)
            PlayerStats.Update();

        if (_firstUpdated)
            return;

        FirstUpdated();
        _firstUpdated = true;
    }

    public void Command_DeviceController_Connect()
    {
        if (DeviceController == null)
        {
            Logger.Error("No device controller available to connect.");
        }
        else
        {
            if (ConfigurationProfile == null)
                return;

            DeviceController.Connect(ConfigurationProfile.BUTTPLUG_SERVER_HOST, ConfigurationProfile.BUTTPLUG_SERVER_PORT);
        }
    }

    private void Command_DeviceController_Disconnect()
    {
        if (DeviceController == null)
        {
            Logger.Error("No device controller available to disconnect.");
        }
        else
        {
            try
            {
                DeviceController.Disconnect();
            }
            catch (Exception ex)
            {
                Logger.Error("App.Command_DeviceController_Disconnect: " + ex.Message);
            }
        }
    }

    private void Command_SendIntensity(string args)
    {
        int intensity;

        try
        {
            intensity = int.Parse(args.Split(" ", 2)[1]);
            Logger.Chat($"Command Send intensity {intensity}");
        } catch (Exception ex) when (ex is FormatException || ex is IndexOutOfRangeException)
        {
            Logger.Error("Malformed arguments for send [intensity].", ex);
            return;
        }

        if (DeviceController == null)
            Logger.Error("No device controller available to send intensity.");
        else
            DeviceController.SendVibeToAll(intensity);
    }

    private void SpellWasTriggered(object? sender, HookActionEffects_ReceivedEventArgs args)
    {
        if (TriggersController == null)
        {
            Logger.Warn("SpellWasTriggered: TriggersController not init yet, ignoring spell...");
        }
        else
        {
            Structures.Spell spell = args.Spell;

            if (ConfigurationProfile != null && ConfigurationProfile.VERBOSE_SPELL)
                Logger.Debug($"VERBOSE_SPELL: {spell}");

            foreach (Trigger trigger in TriggersController.CheckTrigger_Spell(spell))
                DeviceController.SendTrigger(trigger);
        }
    }
        
    private void ChatWasTriggered(XivChatType chatType, int timestamp, ref SeString _sender, ref SeString _message, ref bool isHandled)
    {
        string ChatFromPlayerName = _sender.ToString();

        if (TriggersController == null)
            Logger.Warn("ChatWasTriggered: TriggersController not init yet, ignoring chat...");
        else
        {
            if (ConfigurationProfile != null && ConfigurationProfile.VERBOSE_CHAT)
                Logger.Debug($"VERBOSE_CHAT: {ChatFromPlayerName} type={chatType.ToString()}: {_message}");

            foreach (Trigger trigger in TriggersController.CheckTrigger_Chat(chatType, ChatFromPlayerName, _message.TextValue))
                DeviceController.SendTrigger(trigger);
        }
    }

    private void PlayerCurrentHPChanged(object? send, EventArgs e)
    {
        float currentHp = PlayerStats.GetCurrentHP();
        float maxHp = PlayerStats.GetMaxHP();

        if (TriggersController == null)
            Logger.Warn("PlayerCurrentHPChanged: TriggersController not init yet, ignoring HP change...");
        else
        {
            float percentageHP = currentHp * 100f / maxHp;
            List<Trigger> triggerList = TriggersController.CheckTrigger_HPChanged((int)currentHp, percentageHP);

            Logger.Verbose($"PlayerCurrentHPChanged SelfPlayer {currentHp}/{maxHp} {percentageHP:0.##}%");

            foreach (Trigger trigger in triggerList)
                DeviceController.SendTrigger(trigger);
        }
    }

    private void ClientState_LoginEvent() => PlayerStats.Update();

    private void MonitorPartyList(IPartyList partyList)
    {
        while (ThreadMonitorPartyListRunning)
        {
            if (TriggersController == null)
            {
                Logger.Warn("HPChangedOtherPlayer: TriggersController not init yet, ignoring HP change other...");
                break;
            }

            if (partyList.Length >= 0)
            {
                foreach (Trigger trigger in TriggersController.CheckTrigger_HPChangedOther(partyList))
                {
                    Logger.Verbose($"HPChangedOtherPlayer {trigger.FromPlayerName} min:{trigger.AmountMinValue} max:{trigger.AmountMaxValue} triggered!");
                    DeviceController.SendTrigger(trigger);
                }
            }

            Thread.Sleep(500);
        }
    }

    public bool SetProfile(string profileName)
    {
        if (!Service.Configuration!.SetCurrentProfile(profileName))
        {
            Logger.Warn("You are trying to use profile " + profileName + " which can't be found");
            return false;
        }

        ConfigurationProfile profile = Service.Configuration!.GetProfile(profileName);

        if (profile != null)
        {
            ConfigurationProfile = profile;
            PluginUi.SetProfile(ConfigurationProfile);
            DeviceController.SetProfile(ConfigurationProfile);
            TriggersController.SetProfile(ConfigurationProfile);
        }

        return true;
    }

    private void Command_ProfileSet(string args)
    {
        List<string> list = (args.Split(" ")).ToList();

        if (list.Count == 2)
                SetProfile(list[1]);
        else
            Logger.Error("Wrong command: /fvp profile [name]");
    }
}
