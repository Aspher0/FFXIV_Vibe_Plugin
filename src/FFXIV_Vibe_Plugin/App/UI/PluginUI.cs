using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Device;
using FFXIV_Vibe_Plugin.Triggers;
using FFXIV_Vibe_Plugin.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FFXIV_Vibe_Plugin;

public class PluginUI : Window, IDisposable
{
    private int frameCounter;
    private ConfigurationProfile ConfigurationProfile;
    private readonly DevicesController DevicesController;
    private readonly TriggersController TriggerController;
    private readonly Patterns Patterns = new Patterns();
    private readonly string DonationLink = "https://paypal.me/kaciedev";
    private readonly string KofiLink = "https://ko-fi.com/ffxivvibeplugin";
    private bool _expandedOnce;
    private readonly int WIDTH = 700;
    private readonly int HEIGHT = 600;
    private static readonly Vector2 MinSize = new Vector2(700f, 600f);
    private readonly int COLUMN0_WIDTH = 130;
    private string _tmp_void = "";
    private int simulator_currentAllIntensity;
    private int TRIGGER_CURRENT_SELECTED_DEVICE = -1;
    private string CURRENT_TRIGGER_SELECTOR_SEARCHBAR = "";
    private int _tmp_currentDraggingTriggerIndex = -1;
    private readonly string VALID_REGEXP_PATTERN = "^(\\d+:\\d+)+(\\|\\d+:\\d+)*$";
    private string CURRENT_PATTERN_SEARCHBAR = "";
    private string _tmp_currentPatternNameToAdd = "";
    private string _tmp_currentPatternValueToAdd = "";
    private string _tmp_currentPatternValueState = "unset";
    private string _tmp_currentProfileNameToAdd = "";
    private string _tmp_currentProfile_ErrorMsg = "";
    private readonly int TRIGGER_MIN_AFTER;
    private readonly int TRIGGER_MAX_AFTER = 120;
    private Trigger? SelectedTrigger;
    private string triggersViewMode = "default";
    private string _tmp_exportPatternResponse = "";
    private string PremiumFeatureText = "PREMIUM FEATURE";
    private int FreeAccount_MaxTriggers = 10;

    public PluginUI(
      ConfigurationProfile profile,
      DevicesController deviceController,
      TriggersController triggersController,
      Patterns Patterns) : base("FFXIV Vibe Plugin", (ImGuiWindowFlags)56, false)
    {
        ImGui.SetNextWindowPos(new Vector2(100f, 100f), (ImGuiCond)8);
        ImGui.SetNextWindowSize(new Vector2((float)this.WIDTH, (float)this.HEIGHT), (ImGuiCond)8);
        this.ConfigurationProfile = profile;
        this.DevicesController = deviceController;
        this.TriggerController = triggersController;
        this.Patterns = Patterns;
    }

    public void Dispose()
    {
    }

    public void SetProfile(ConfigurationProfile profile) => this.ConfigurationProfile = profile;

    public override void Draw()
    {
        try
        {
            this.DrawMainWindow();
        }
        catch (Exception ex)
        {
            Logger.Error("UI ERROR: ");
            Logger.Error(ex.ToString());
        }
        this.frameCounter = (this.frameCounter + 1) % 400;
    }

    public void DrawMainWindow()
    {
        if (!_expandedOnce)
        {
            ImGui.SetNextWindowCollapsed(false);
            _expandedOnce = true;
        }
        ImGui.Spacing();
        UIBanner.Draw(frameCounter, this.DonationLink, this.KofiLink, this.DevicesController);
        ImGui.Columns(1);
        if (!ImGui.BeginTabBar("##ConfigTabBar", (ImGuiTabBarFlags)0))
            return;
        if (ImGui.BeginTabItem("Connect"))
        {
            UIConnect.Draw(ConfigurationProfile, DevicesController);
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Options"))
        {
            this.DrawOptionsTab();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Devices"))
        {
            this.DrawDevicesTab();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Triggers"))
        {
            this.DrawTriggersTab();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Patterns"))
        {
            this.DrawPatternsTab();
            ImGui.EndTabItem();
        }
        if (!ImGui.BeginTabItem("Help"))
            return;
        this.DrawHelpTab();
        ImGui.EndTabItem();
    }

    public void DrawOptionsTab()
    {
        ImGui.TextColored(ImGuiColors.DalamudViolet, "General Settings");
        ImGui.BeginChild("###GENERAL_OPTIONS_ZONE", new Vector2(-1f, 145f), true);
        if (ImGui.BeginTable("###GENERAL_OPTIONS_TABLE", 2))
        {
            ImGui.TableSetupColumn("###GENERAL_OPTIONS_TABLE_COL1", (ImGuiTableColumnFlags)8, 250f);
            ImGui.TableSetupColumn("###GENERAL_OPTIONS_TABLE_COL2", (ImGuiTableColumnFlags)4);
            ImGui.TableNextColumn();
            bool buttplugServerShouldWss = ConfigurationProfile.BUTTPLUG_SERVER_SHOULD_WSS;
            ImGui.Text("Connects through WSS");
            ImGui.TableNextColumn();
            if (ImGui.Checkbox("###GENERAL_OPTIONS_WSS", ref buttplugServerShouldWss))
            {
                ConfigurationProfile.BUTTPLUG_SERVER_SHOULD_WSS = buttplugServerShouldWss;
                Service.Configuration!.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Connects through WSS rather than WS which should not be needed for local connection (default: false)");
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            bool autoOpen = this.ConfigurationProfile.AUTO_OPEN;
            ImGui.Text("Automatically open configuration panel.");
            ImGui.TableNextColumn();
            if (ImGui.Checkbox("###GENERAL_OPTIONS_AUTO_OPEN", ref autoOpen))
            {
                this.ConfigurationProfile.AUTO_OPEN = autoOpen;
                Service.Configuration!.Save();
            }
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Global threshold: ");
            ImGui.TableNextColumn();
            int maxVibeThreshold = this.ConfigurationProfile.MAX_VIBE_THRESHOLD;
            ImGui.SetNextItemWidth(201f);
            if (ImGui.SliderInt("###OPTION_MaximumThreshold", ref maxVibeThreshold, 2, 100))
            {
                this.ConfigurationProfile.MAX_VIBE_THRESHOLD = maxVibeThreshold;
                Service.Configuration!.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Maximum threshold for vibes (will override every devices).");
            ImGui.TableNextColumn();
            ImGui.Text("Log casted spells:");
            ImGui.TableNextColumn();
            if (ImGui.Checkbox("###OPTION_VERBOSE_SPELL", ref this.ConfigurationProfile.VERBOSE_SPELL))
                Service.Configuration!.Save();
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Use the /xllog to see all casted spells. Disable this to have better ingame performance.");
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Log chat triggered:");
            ImGui.TableNextColumn();
            if (ImGui.Checkbox("###OPTION_VERBOSE_CHAT", ref this.ConfigurationProfile.VERBOSE_CHAT))
                Service.Configuration!.Save();
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Use the /xllog to see all chat message. Disable this to have better ingame performance.");
            ImGui.EndTable();
        }
        if (this.ConfigurationProfile.VERBOSE_CHAT || this.ConfigurationProfile.VERBOSE_SPELL)
            ImGui.TextColored(ImGuiColors.DalamudOrange, "Please, disabled chat and spell logs for better ingame performance.");
        ImGui.EndChild();
        ImGui.Spacing();
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Profile settings");
        ImGui.BeginChild("###CONFIGURATION_PROFILE_ZONE", new Vector2(-1f, this._tmp_currentProfile_ErrorMsg == "" ? 100f : 120f), true);

        if (ImGui.BeginTable("###CONFIGURATION_PROFILE_TABLE", 3))
        {
            ImGui.TableSetupColumn("###CONFIGURATION_PROFILE_TABLE_COL1", (ImGuiTableColumnFlags)8, 150f);
            ImGui.TableSetupColumn("###CONFIGURATION_PROFILE_TABLE_COL2", (ImGuiTableColumnFlags)8, 350f);
            ImGui.TableSetupColumn("###CONFIGURATION_PROFILE_TABLE_COL3", (ImGuiTableColumnFlags)4);
            ImGui.TableNextColumn();
            ImGui.Text("Current profile:");
            ImGui.TableNextColumn();
            string[] array = Service.Configuration!.Profiles.Select<ConfigurationProfile, string>((Func<ConfigurationProfile, string>)(profile => profile.Name)).ToArray<string>();
            int index = Service.Configuration!.Profiles.FindIndex((Predicate<ConfigurationProfile>)(profile => profile.Name == Service.Configuration!.CurrentProfileName));
            ImGui.SetNextItemWidth(350f);
            if (ImGui.Combo("###CONFIGURATION_CURRENT_PROFILE", ref index, array, array.Length))
            {
                Service.Configuration!.CurrentProfileName = Service.Configuration!.Profiles[index].Name;
                Service.App.SetProfile(Service.Configuration!.CurrentProfileName);
                Logger.Debug("New profile selected: " + Service.Configuration!.CurrentProfileName);
                Service.Configuration!.Save();
            }
            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton((FontAwesomeIcon)61944))
            {
                if (Service.Configuration!.Profiles.Count <= 1)
                {
                    string msg = "You can't delete this profile. At least one profile should exists. Create another one before deleting.";
                    Logger.Error(msg);
                    this._tmp_currentProfile_ErrorMsg = msg;
                }
                else
                {
                    Service.Configuration!.RemoveProfile(this.ConfigurationProfile.Name);
                    ConfigurationProfile firstProfile = Service.Configuration!.GetFirstProfile();
                    if (firstProfile != null)
                        Service.App.SetProfile(firstProfile.Name);
                    Service.Configuration!.Save();
                }
            }
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Add new profile: ");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(350f);
            if (ImGui.InputText("###CONFIGURATION_NEW_PROFILE_NAME", ref this._tmp_currentProfileNameToAdd, 150))
                this._tmp_currentProfile_ErrorMsg = "";
            ImGui.TableNextColumn();
            if (this._tmp_currentProfileNameToAdd.Length > 0 && ImGuiComponents.IconButton((FontAwesomeIcon)61543) && this._tmp_currentProfileNameToAdd.Trim() != "")
            {
                if (!Service.Configuration!.AddProfile(this._tmp_currentProfileNameToAdd))
                {
                    string msg = "The current profile name '" + this._tmp_currentProfileNameToAdd + "' already exists!";
                    Logger.Error(msg);
                    this._tmp_currentProfile_ErrorMsg = msg;
                }
                else
                {
                    Service.App.SetProfile(this._tmp_currentProfileNameToAdd);
                    Logger.Debug("New profile added " + this._tmp_currentProfileNameToAdd);
                    this._tmp_currentProfileNameToAdd = "";
                    Service.Configuration!.Save();
                }
            }
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Rename current profile");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(350f);
            if (ImGui.InputText("###CONFIGURATION_CURRENT_PROFILE_RENAME", ref this.ConfigurationProfile.Name, 150))
            {
                Service.Configuration!.CurrentProfileName = this.ConfigurationProfile.Name;
                Service.Configuration!.Save();
            }
            ImGui.EndTable();
        }
        if (this._tmp_currentProfile_ErrorMsg != "")
            ImGui.TextColored(ImGuiColors.DalamudRed, this._tmp_currentProfile_ErrorMsg);

        ImGui.EndChild();
        ImGui.Spacing();
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Triggers Import/Export Settings");
        ImGui.BeginChild("###EXPORT_OPTIONS_ZONE", new Vector2(-1f, 100f), true);

        if (ImGui.BeginTable("###EXPORT_OPTIONS_TABLE", 2))
        {
            ImGui.TableSetupColumn("###EXPORT_OPTIONS_TABLE_COL1", (ImGuiTableColumnFlags)8, 250f);
            ImGui.TableSetupColumn("###EXPORT_OPTIONS_TABLE_COL2", (ImGuiTableColumnFlags)4);
            ImGui.TableNextColumn();
            ImGui.Text("Trigger Import/Export Directory:");
            ImGui.TableNextColumn();
            if (ImGui.InputText("###EXPORT_DIRECTORY_INPUT", ref this.ConfigurationProfile.EXPORT_DIR, 200))
            {
                Service.Configuration!.EXPORT_DIR = this.ConfigurationProfile.EXPORT_DIR;
                Service.Configuration!.Save();
            }
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Button("Clear Import/Export Directory"))
            {
                if (!this.ConfigurationProfile.EXPORT_DIR.Equals(""))
                {
                    try
                    {
                        foreach (string file in Directory.GetFiles(this.ConfigurationProfile.EXPORT_DIR))
                            File.Delete(file);
                    }
                    catch
                    {
                    }
                }
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Deletes ALL files in the Import/Export Directory.");
            ImGui.EndTable();
        }
        else
            ImGui.TextColored(ImGuiColors.DalamudGrey, this.PremiumFeatureText);

        ImGui.EndChild();
    }

    public void DrawDevicesTab()
    {
        ImGui.Spacing();
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions");
        ImGui.BeginChild("###DevicesTab_General", new Vector2(-1f, 40f), true);
        if (this.DevicesController.IsScanning())
        {
            if (ImGui.Button("Stop scanning", new Vector2(100f, 24f)))
                this.DevicesController.StopScanningDevice();
        }
        else if (ImGui.Button("Scan device", new Vector2(100f, 24f)))
            this.DevicesController.ScanDevice();
        ImGui.SameLine();
        if (ImGui.Button("Update Battery", new Vector2(100f, 24f)))
            this.DevicesController.UpdateAllBatteryLevel();
        ImGui.SameLine();
        if (ImGui.Button("Stop All", new Vector2(100f, 24f)))
        {
            this.DevicesController.StopAll();
            this.simulator_currentAllIntensity = 0;
        }
        ImGui.EndChild();
        if (ImGui.CollapsingHeader("All devices"))
        {
            ImGui.Text("Send to all:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderInt("###SendVibeAll_Intensity", ref this.simulator_currentAllIntensity, 0, 100))
                this.DevicesController.SendVibeToAll(this.simulator_currentAllIntensity);
        }
        foreach (FFXIV_Vibe_Plugin.Device.Device device in this.DevicesController.GetDevices())
        {
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 3);
            interpolatedStringHandler.AppendLiteral("[");
            interpolatedStringHandler.AppendFormatted<int>(device.Id);
            interpolatedStringHandler.AppendLiteral("] ");
            interpolatedStringHandler.AppendFormatted(device.Name);
            interpolatedStringHandler.AppendLiteral(" - Battery: ");
            interpolatedStringHandler.AppendFormatted(device.GetBatteryPercentage());
            if (ImGui.CollapsingHeader(interpolatedStringHandler.ToStringAndClear()))
            {
                ImGui.TextWrapped(device.ToString());
                if (device.CanVibrate)
                {
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "VIBRATE");
                    ImGui.Indent(10f);
                    for (int motorId = 0; motorId < device.VibrateMotors; ++motorId)
                    {
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 1);
                        interpolatedStringHandler.AppendLiteral("Motor ");
                        interpolatedStringHandler.AppendFormatted<int>(motorId + 1);
                        interpolatedStringHandler.AppendLiteral(": ");
                        ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(200f);
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 2);
                        interpolatedStringHandler.AppendLiteral("###");
                        interpolatedStringHandler.AppendFormatted<int>(device.Id);
                        interpolatedStringHandler.AppendLiteral(" Intensity Vibrate Motor ");
                        interpolatedStringHandler.AppendFormatted<int>(motorId);
                        if (ImGui.SliderInt(interpolatedStringHandler.ToStringAndClear(), ref device.CurrentVibrateIntensity[motorId], 0, 100))
                            this.DevicesController.SendVibrate(device, device.CurrentVibrateIntensity[motorId], motorId);
                    }
                    ImGui.Unindent(10f);
                }
                if (device.CanRotate)
                {
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "ROTATE");
                    ImGui.Indent(10f);
                    for (int motorId = 0; motorId < device.RotateMotors; ++motorId)
                    {
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 1);
                        interpolatedStringHandler.AppendLiteral("Motor ");
                        interpolatedStringHandler.AppendFormatted<int>(motorId + 1);
                        interpolatedStringHandler.AppendLiteral(": ");
                        ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(200f);
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
                        interpolatedStringHandler.AppendLiteral("###");
                        interpolatedStringHandler.AppendFormatted<int>(device.Id);
                        interpolatedStringHandler.AppendLiteral(" Intensity Rotate Motor ");
                        interpolatedStringHandler.AppendFormatted<int>(motorId);
                        if (ImGui.SliderInt(interpolatedStringHandler.ToStringAndClear(), ref device.CurrentRotateIntensity[motorId], 0, 100))
                            this.DevicesController.SendRotate(device, device.CurrentRotateIntensity[motorId], motorId);
                    }
                    ImGui.Unindent(10f);
                }
                if (device.CanLinear)
                {
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "LINEAR VIBES");
                    ImGui.Indent(10f);
                    for (int duration = 0; duration < device.LinearMotors; ++duration)
                    {
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 1);
                        interpolatedStringHandler.AppendLiteral("Motor ");
                        interpolatedStringHandler.AppendFormatted<int>(duration + 1);
                        interpolatedStringHandler.AppendLiteral(": ");
                        ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(200f);
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
                        interpolatedStringHandler.AppendLiteral("###");
                        interpolatedStringHandler.AppendFormatted<int>(device.Id);
                        interpolatedStringHandler.AppendLiteral(" Intensity Linear Motor ");
                        interpolatedStringHandler.AppendFormatted<int>(duration);
                        if (ImGui.SliderInt(interpolatedStringHandler.ToStringAndClear(), ref device.CurrentLinearIntensity[duration], 0, 100))
                            this.DevicesController.SendLinear(device, device.CurrentLinearIntensity[duration], 500, duration);
                    }
                    ImGui.Unindent(10f);
                }
                if (device.CanOscillate)
                {
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "OSCILLATE VIBES");
                    ImGui.Indent(10f);
                    for (int duration = 0; duration < device.OscillateMotors; ++duration)
                    {
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 1);
                        interpolatedStringHandler.AppendLiteral("Motor ");
                        interpolatedStringHandler.AppendFormatted<int>(duration + 1);
                        interpolatedStringHandler.AppendLiteral(": ");
                        ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(200f);
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 2);
                        interpolatedStringHandler.AppendLiteral("###");
                        interpolatedStringHandler.AppendFormatted<int>(device.Id);
                        interpolatedStringHandler.AppendLiteral(" Intensity Oscillate Motor ");
                        interpolatedStringHandler.AppendFormatted<int>(duration);
                        if (ImGui.SliderInt(interpolatedStringHandler.ToStringAndClear(), ref device.CurrentOscillateIntensity[duration], 0, 100))
                            this.DevicesController.SendOscillate(device, device.CurrentOscillateIntensity[duration], 500, duration);
                    }
                    ImGui.Unindent(10f);
                }
            }
        }
    }

    public unsafe void DrawTriggersTab()
    {
        List<Trigger> triggers = this.TriggerController.GetTriggers();
        string id = this.SelectedTrigger != null ? this.SelectedTrigger.Id : "";
        DefaultInterpolatedStringHandler interpolatedStringHandler;
        if (ImGui.BeginChild("###TriggersSelector", new Vector2(ImGui.GetWindowContentRegionMax().X / 3f, -ImGui.GetFrameHeightWithSpacing()), true))
        {
            ImGui.SetNextItemWidth(185f);
            ImGui.InputText("###TriggersSelector_SearchBar", ref this.CURRENT_TRIGGER_SELECTOR_SEARCHBAR, 200);
            ImGui.Spacing();
            int num1 = triggers.Count;

            for (int index1 = 0; index1 < triggers.Count; ++index1)
            {
                Trigger trigger1 = triggers[index1];
                if (trigger1 != null)
                {
                    string str1 = trigger1.Enabled ? "" : "[disabled]";
                    string str2 = Enum.GetName(typeof(KIND), (object)trigger1.Kind) ?? "";
                    if (str2 != null)
                        str2 = str2.ToUpper();
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 3);
                    interpolatedStringHandler.AppendFormatted(str1);
                    interpolatedStringHandler.AppendLiteral("[");
                    interpolatedStringHandler.AppendFormatted(str2);
                    interpolatedStringHandler.AppendLiteral("] ");
                    interpolatedStringHandler.AppendFormatted(trigger1.Name);
                    string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                    string str3 = stringAndClear + "###" + trigger1.Id;
                    if (Helpers.RegExpMatch(stringAndClear, this.CURRENT_TRIGGER_SELECTOR_SEARCHBAR))
                    {
                        if (index1 < num1)
                        {
                            if (ImGui.Selectable(stringAndClear ?? "", id == trigger1.Id))
                            {
                                this.SelectedTrigger = trigger1;
                                this.triggersViewMode = "edit";
                            }
                        }
                        else
                            ImGui.TextColored(ImGuiColors.DalamudGrey, this.PremiumFeatureText + ": " + stringAndClear);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(stringAndClear ?? "");

                        if (ImGui.BeginDragDropTarget())
                        {
                            if (this._tmp_currentDraggingTriggerIndex > -1 && ImGui.IsMouseReleased((ImGuiMouseButton)0))
                            {
                                int draggingTriggerIndex = this._tmp_currentDraggingTriggerIndex;
                                int index2 = index1;
                                List<Trigger> triggerList1 = triggers;
                                int index3 = draggingTriggerIndex;
                                List<Trigger> triggerList2 = triggers;
                                int num2 = index2;
                                Trigger trigger2 = triggers[index2];
                                Trigger trigger3 = triggers[draggingTriggerIndex];
                                triggerList1[index3] = trigger2;
                                int index4 = num2;
                                Trigger trigger4 = trigger3;
                                triggerList2[index4] = trigger4;
                                this._tmp_currentDraggingTriggerIndex = -1;
                                Service.Configuration!.Save();
                            }
                            ImGui.EndDragDropTarget();
                        }
                    }
                }
            }
            ImGui.EndChild();
        }
        ImGui.SameLine();
        if (ImGui.BeginChild("###TriggerViewerPanel", new Vector2(0.0f, -ImGui.GetFrameHeightWithSpacing()), true))
        {
            if (this.triggersViewMode == "default")
                ImGui.Text("Please select or add a trigger");
            else if (this.triggersViewMode == "edit")
            {
                if (this.SelectedTrigger != null)
                {
                    if (ImGui.BeginTable("###TRIGGER_FORM_TABLE_GENERAL", 2))
                    {
                        ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_COL1", (ImGuiTableColumnFlags)8, (float)this.COLUMN0_WIDTH);
                        ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_COL2", (ImGuiTableColumnFlags)4);
                        ImGui.TableNextColumn();
                        ImGui.Text("TriggerID:");
                        ImGui.TableNextColumn();
                        ImGui.Text(this.SelectedTrigger.GetShortID() ?? "");
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("Enabled:");
                        ImGui.TableNextColumn();
                        if (ImGui.Checkbox("###TRIGGER_ENABLED", ref this.SelectedTrigger.Enabled))
                            Service.Configuration!.Save();
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("Trigger Name:");
                        ImGui.TableNextColumn();
                        if (ImGui.InputText("###TRIGGER_NAME", ref this.SelectedTrigger.Name, 99))
                        {
                            if (this.SelectedTrigger.Name == "")
                                this.SelectedTrigger.Name = "no_name";
                            Service.Configuration!.Save();
                        }
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("Trigger Description:");
                        ImGui.TableNextColumn();
                        if (ImGui.InputTextMultiline("###TRIGGER_DESCRIPTION", ref this.SelectedTrigger.Description, 500, new Vector2(190f, 50f)))
                        {
                            if (this.SelectedTrigger.Description == "")
                                this.SelectedTrigger.Description = "no_description";
                            Service.Configuration!.Save();
                        }
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("Kind:");
                        ImGui.TableNextColumn();
                        string[] names = Enum.GetNames(typeof(KIND));

                        int num = (int)this.SelectedTrigger.Kind;
                        if (ImGui.Combo("###TRIGGER_FORM_KIND", ref num, names, names.Length))
                        {
                            this.SelectedTrigger.Kind = (KIND)num;
                            Service.Configuration!.Save();
                        }
                        bool flag2 = true;
                        if (ImGui.BeginPopupModal("HpChangeOther Premium Only", ref flag2, (ImGuiWindowFlags)67))
                        {
                            Vector2 vector2 = new Vector2(40f, 25f);
                            ImGui.TextColored(ImGuiColors.DalamudViolet, "The HPChangeOther kind is a premium feature!");
                            ImGui.Indent(ImGui.GetWindowWidth() * 0.5f - vector2.X);
                            if (ImGui.Button("OK", vector2))
                                ImGui.CloseCurrentPopup();
                            ImGui.EndPopup();
                        }
                        ImGui.TableNextRow();
                        if (num != 2)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text("Player name:");
                            ImGui.TableNextColumn();

                            if (ImGui.InputText("###TRIGGER_CHAT_FROM_PLAYER_NAME", ref this.SelectedTrigger.FromPlayerName, 100))
                            {
                                this.SelectedTrigger.FromPlayerName = this.SelectedTrigger.FromPlayerName.Trim();
                                Service.Configuration!.Save();
                            }
                            ImGui.SameLine();
                            ImGuiComponents.HelpMarker("You can use RegExp. Leave empty for any. Ignored if chat listening to 'Echo' and chat message we through it.");

                            ImGui.TableNextRow();
                        }
                        ImGui.TableNextColumn();
                        ImGui.Text("Start after");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(185f);
                        if (ImGui.SliderFloat("###TRIGGER_FORM_START_AFTER", ref this.SelectedTrigger.StartAfter, (float)this.TRIGGER_MIN_AFTER, (float)this.TRIGGER_MAX_AFTER))
                        {
                            this.SelectedTrigger.StartAfter = Helpers.ClampFloat(this.SelectedTrigger.StartAfter, (float)this.TRIGGER_MIN_AFTER, (float)this.TRIGGER_MAX_AFTER);
                            Service.Configuration!.Save();
                        }
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(45f);
                        if (ImGui.InputFloat("###TRIGGER_FORM_START_AFTER_INPUT", ref this.SelectedTrigger.StartAfter, (float)this.TRIGGER_MIN_AFTER, (float)this.TRIGGER_MAX_AFTER))
                        {
                            this.SelectedTrigger.StartAfter = Helpers.ClampFloat(this.SelectedTrigger.StartAfter, (float)this.TRIGGER_MIN_AFTER, (float)this.TRIGGER_MAX_AFTER);
                            Service.Configuration!.Save();
                        }
                        ImGui.SameLine();
                        ImGuiComponents.HelpMarker("In seconds");
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("Stop after");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(185f);
                        if (ImGui.SliderFloat("###TRIGGER_FORM_STOP_AFTER", ref this.SelectedTrigger.StopAfter, (float)this.TRIGGER_MIN_AFTER, (float)this.TRIGGER_MAX_AFTER))
                        {
                            this.SelectedTrigger.StopAfter = Helpers.ClampFloat(this.SelectedTrigger.StopAfter, (float)this.TRIGGER_MIN_AFTER, (float)this.TRIGGER_MAX_AFTER);
                            Service.Configuration!.Save();
                        }
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(45f);
                        if (ImGui.InputFloat("###TRIGGER_FORM_STOP_AFTER_INPUT", ref this.SelectedTrigger.StopAfter, (float)this.TRIGGER_MIN_AFTER, (float)this.TRIGGER_MAX_AFTER))
                        {
                            this.SelectedTrigger.StopAfter = Helpers.ClampFloat(this.SelectedTrigger.StopAfter, (float)this.TRIGGER_MIN_AFTER, (float)this.TRIGGER_MAX_AFTER);
                            Service.Configuration!.Save();
                        }
                        ImGui.SameLine();
                        ImGuiComponents.HelpMarker("In seconds. Use zero to avoid stopping.");
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("Priority");
                        ImGui.TableNextColumn();

                        if (ImGui.InputInt("###TRIGGER_FORM_PRIORITY", ref this.SelectedTrigger.Priority, 1))
                            Service.Configuration!.Save();
                        ImGui.SameLine();
                        ImGuiComponents.HelpMarker("If a trigger have a lower priority, it will be ignored.");
                        ImGui.TableNextRow();

                        ImGui.EndTable();
                    }
                    ImGui.Separator();
                    if (this.SelectedTrigger.Kind == KIND.Chat && ImGui.BeginTable("###TRIGGER_FORM_TABLE_KIND_CHAT", 2))
                    {
                        ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_CHAT_COL1", (ImGuiTableColumnFlags)8, (float)this.COLUMN0_WIDTH);
                        ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_CHAT_COL2", (ImGuiTableColumnFlags)4);
                        ImGui.TableNextColumn();
                        ImGui.Text("Chat text:");
                        ImGui.TableNextColumn();
                        string chatText = this.SelectedTrigger.ChatText;
                        if (ImGui.InputText("###TRIGGER_CHAT_TEXT", ref chatText, 250))
                        {
                            this.SelectedTrigger.ChatText = chatText.ToLower();
                            Service.Configuration!.Save();
                        }
                        ImGui.SameLine();
                        ImGuiComponents.HelpMarker("It is case insensitive. Also, you can use RegExp if you wish to.");
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text("Add chat type:");
                        ImGui.TableNextColumn();
                        int index5 = 0;
                        string[] names = Enum.GetNames(typeof(XivChatType));
                        if (ImGui.Combo("###TRIGGER_CHAT_TEXT_TYPE_ALLOWED", ref index5, names, names.Length))
                        {
                            if (!this.SelectedTrigger.AllowedChatTypes.Contains(index5))
                                this.SelectedTrigger.AllowedChatTypes.Add((int)(XivChatType)Enum.Parse(typeof(XivChatType), names[index5]));
                            Service.Configuration!.Save();
                        }
                        ImGuiComponents.HelpMarker("Select some chats to observe or unselect all to watch every chats.");
                        ImGui.TableNextRow();
                        if (this.SelectedTrigger.AllowedChatTypes.Count > 0)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text("Allowed Type:");
                            ImGui.TableNextColumn();
                            for (int index6 = 0; index6 < this.SelectedTrigger.AllowedChatTypes.Count; ++index6)
                            {
                                int allowedChatType = this.SelectedTrigger.AllowedChatTypes[index6];
                                if (ImGuiComponents.IconButton(index6, (FontAwesomeIcon)61544))
                                {
                                    this.SelectedTrigger.AllowedChatTypes.RemoveAt(index6);
                                    Service.Configuration!.Save();
                                }
                                ImGui.SameLine();
                                ImGui.Text(((XivChatType)(int)(ushort)allowedChatType).ToString() ?? "");
                            }
                            ImGui.TableNextRow();
                        }
                        ImGui.EndTable();
                    }
                    if (ImGui.BeginTable("###TRIGGER_FORM_TABLE_KIND_SPELL", 2))
                    {
                        ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_SPELL_COL1", (ImGuiTableColumnFlags)8, (float)this.COLUMN0_WIDTH);
                        ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_SPELL_COL2", (ImGuiTableColumnFlags)4);
                        if (this.SelectedTrigger.Kind == KIND.Spell)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text("Type:");
                            ImGui.TableNextColumn();
                            string[] names1 = Enum.GetNames(typeof(Structures.ActionEffectType));
                            int actionEffectType = this.SelectedTrigger.ActionEffectType;
                            if (ImGui.Combo("###TRIGGER_FORM_EVENT", ref actionEffectType, names1, names1.Length))
                            {
                                this.SelectedTrigger.ActionEffectType = actionEffectType;
                                this.SelectedTrigger.Reset();
                                Service.Configuration!.Save();
                            }
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Spell Text:");
                            ImGui.TableNextColumn();
                            if (ImGui.InputText("###TRIGGER_FORM_SPELLNAME", ref this.SelectedTrigger.SpellText, 100))
                                Service.Configuration!.Save();
                            ImGui.SameLine();
                            ImGuiComponents.HelpMarker("You can use RegExp.");
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Direction:");
                            ImGui.TableNextColumn();
                            string[] names2 = Enum.GetNames(typeof(DIRECTION));
                            int direction = this.SelectedTrigger.Direction;
                            if (ImGui.Combo("###TRIGGER_FORM_DIRECTION", ref direction, names2, names2.Length))
                            {
                                this.SelectedTrigger.Direction = direction;
                                Service.Configuration!.Save();
                            }
                            ImGui.SameLine();
                            ImGuiComponents.HelpMarker("Warning: Hitting no target will result to self as if you cast on yourself");
                            ImGui.TableNextRow();
                        }
                        if (this.SelectedTrigger.ActionEffectType == 3 || this.SelectedTrigger.ActionEffectType == 4 || this.SelectedTrigger.Kind == KIND.HPChange || this.SelectedTrigger.Kind == KIND.HPChangeOther)
                        {
                            string str = "";
                            if (this.SelectedTrigger.ActionEffectType == 3)
                                str = "damage";
                            if (this.SelectedTrigger.ActionEffectType == 4)
                                str = "heal";
                            if (this.SelectedTrigger.Kind == KIND.HPChange)
                                str = "health";
                            if (this.SelectedTrigger.Kind == KIND.HPChangeOther)
                                str = "health";
                            ImGui.TableNextColumn();
                            ImGui.Text("Amount in percentage?");
                            ImGui.TableNextColumn();
                            if (ImGui.Checkbox("###TRIGGER_AMOUNT_IN_PERCENTAGE", ref this.SelectedTrigger.AmountInPercentage))
                            {
                                this.SelectedTrigger.AmountMinValue = 0;
                                this.SelectedTrigger.AmountMaxValue = 100;
                                Service.Configuration!.Save();
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text("Min " + str + " value:");
                            ImGui.TableNextColumn();
                            if (this.SelectedTrigger.AmountInPercentage)
                            {
                                if (ImGui.SliderInt("###TRIGGER_FORM_MIN_AMOUNT", ref this.SelectedTrigger.AmountMinValue, 0, 100))
                                    Service.Configuration!.Save();
                            }
                            else if (ImGui.InputInt("###TRIGGER_FORM_MIN_AMOUNT", ref this.SelectedTrigger.AmountMinValue, 100))
                                Service.Configuration!.Save();
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Max " + str + " value:");
                            ImGui.TableNextColumn();
                            if (this.SelectedTrigger.AmountInPercentage)
                            {
                                if (ImGui.SliderInt("###TRIGGER_FORM_MAX_AMOUNT", ref this.SelectedTrigger.AmountMaxValue, 0, 100))
                                    Service.Configuration!.Save();
                            }
                            else if (ImGui.InputInt("###TRIGGER_FORM_MAX_AMOUNT", ref this.SelectedTrigger.AmountMaxValue, 100))
                                Service.Configuration!.Save();
                            ImGui.TableNextRow();
                        }
                        ImGui.EndTable();
                    }
                    ImGui.Separator();
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions");
                    ImGui.Separator();
                    if (ImGui.Button("Test trigger"))
                        this.DevicesController.SendTrigger(this.SelectedTrigger);
                    ImGui.SameLine();
                    if (ImGui.Button("Export"))
                        this._tmp_exportPatternResponse = this.export_trigger(this.SelectedTrigger);
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Writes this trigger to your export directory.");
                    ImGui.SameLine();
                    ImGui.Text(this._tmp_exportPatternResponse ?? "");
                    ImGui.Separator();
                    ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions & Devices");
                    ImGui.Separator();
                    Dictionary<string, FFXIV_Vibe_Plugin.Device.Device> visitedDevices = this.DevicesController.GetVisitedDevices();
                    if (visitedDevices.Count == 0)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Please connect yourself to intiface and add device(s)...");
                    }
                    else
                    {
                        string[] array1 = visitedDevices.Keys.ToArray<string>();
                        ImGui.Combo("###TRIGGER_FORM_COMBO_DEVICES", ref this.TRIGGER_CURRENT_SELECTED_DEVICE, array1, array1.Length);
                        ImGui.SameLine();
                        List<TriggerDevice> devices = this.SelectedTrigger.Devices;
                        if (ImGuiComponents.IconButton((FontAwesomeIcon)61543) && this.TRIGGER_CURRENT_SELECTED_DEVICE >= 0)
                        {
                            TriggerDevice triggerDevice = new TriggerDevice(visitedDevices[array1[this.TRIGGER_CURRENT_SELECTED_DEVICE]]);
                            devices.Add(triggerDevice);
                            Service.Configuration!.Save();
                        }
                        string[] array2 = this.Patterns.GetAllPatterns().Select<Pattern, string>((Func<Pattern, string>)(p => p.Name)).ToArray<string>();
                        for (int index7 = 0; index7 < devices.Count; ++index7)
                        {
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 1);
                            interpolatedStringHandler.AppendLiteral("###TRIGGER_FORM_COMBO_DEVICE_$");
                            interpolatedStringHandler.AppendFormatted<int>(index7);
                            string stringAndClear1 = interpolatedStringHandler.ToStringAndClear();
                            TriggerDevice triggerDevice = devices[index7];
                            if (ImGui.CollapsingHeader((triggerDevice.Device != null ? triggerDevice.Device.Name : "UnknownDevice") ?? ""))
                            {
                                ImGui.Indent(10f);
                                if (triggerDevice != null && triggerDevice.Device != null)
                                {
                                    if (triggerDevice.Device.CanVibrate)
                                    {
                                        if (ImGui.Checkbox(stringAndClear1 + "_SHOULD_VIBRATE", ref triggerDevice.ShouldVibrate))
                                        {
                                            triggerDevice.ShouldStop = false;
                                            Service.Configuration!.Save();
                                        }
                                        ImGui.SameLine();
                                        ImGui.Text("Should Vibrate");
                                        if (triggerDevice.ShouldVibrate)
                                        {
                                            ImGui.Indent(20f);
                                            for (int index8 = 0; index8 < triggerDevice.Device.VibrateMotors; ++index8)
                                            {
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 1);
                                                interpolatedStringHandler.AppendLiteral("Motor ");
                                                interpolatedStringHandler.AppendFormatted<int>(index8 + 1);
                                                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                                                ImGui.SameLine();
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(22, 2);
                                                interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                interpolatedStringHandler.AppendLiteral("_SHOULD_VIBRATE_MOTOR_");
                                                interpolatedStringHandler.AppendFormatted<int>(index8);
                                                if (ImGui.Checkbox(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.VibrateSelectedMotors[index8]))
                                                    Service.Configuration!.Save();
                                                if (triggerDevice.VibrateSelectedMotors[index8])
                                                {
                                                    ImGui.SameLine();
                                                    ImGui.SetNextItemWidth(90f);
                                                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
                                                    interpolatedStringHandler.AppendLiteral("###");
                                                    interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                    interpolatedStringHandler.AppendLiteral("_VIBRATE_PATTERNS_");
                                                    interpolatedStringHandler.AppendFormatted<int>(index8);
                                                    if (ImGui.Combo(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.VibrateMotorsPattern[index8], array2, array2.Length))
                                                        Service.Configuration!.Save();
                                                    int num = triggerDevice.VibrateMotorsPattern[index8];
                                                    ImGui.SameLine();
                                                    ImGui.SetNextItemWidth(180f);
                                                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
                                                    interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                    interpolatedStringHandler.AppendLiteral("_SHOULD_VIBRATE_MOTOR_");
                                                    interpolatedStringHandler.AppendFormatted<int>(index8);
                                                    interpolatedStringHandler.AppendLiteral("_THRESHOLD");
                                                    if (ImGui.SliderInt(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.VibrateMotorsThreshold[index8], 0, 100))
                                                    {
                                                        if (triggerDevice.VibrateMotorsThreshold[index8] > 0)
                                                            triggerDevice.VibrateSelectedMotors[index8] = true;
                                                        Service.Configuration!.Save();
                                                    }
                                                }
                                            }
                                            ImGui.Indent(-20f);
                                        }
                                    }
                                    if (triggerDevice.Device.CanRotate)
                                    {
                                        if (ImGui.Checkbox(stringAndClear1 + "_SHOULD_ROTATE", ref triggerDevice.ShouldRotate))
                                        {
                                            triggerDevice.ShouldStop = false;
                                            Service.Configuration!.Save();
                                        }
                                        ImGui.SameLine();
                                        ImGui.Text("Should Rotate");
                                        if (triggerDevice.ShouldRotate)
                                        {
                                            ImGui.Indent(20f);
                                            for (int index9 = 0; index9 < triggerDevice.Device.RotateMotors; ++index9)
                                            {
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 1);
                                                interpolatedStringHandler.AppendLiteral("Motor ");
                                                interpolatedStringHandler.AppendFormatted<int>(index9 + 1);
                                                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                                                ImGui.SameLine();
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
                                                interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                interpolatedStringHandler.AppendLiteral("_SHOULD_ROTATE_MOTOR_");
                                                interpolatedStringHandler.AppendFormatted<int>(index9);
                                                if (ImGui.Checkbox(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.RotateSelectedMotors[index9]))
                                                    Service.Configuration!.Save();
                                                if (triggerDevice.RotateSelectedMotors[index9])
                                                {
                                                    ImGui.SameLine();
                                                    ImGui.SetNextItemWidth(90f);
                                                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 2);
                                                    interpolatedStringHandler.AppendLiteral("###");
                                                    interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                    interpolatedStringHandler.AppendLiteral("_ROTATE_PATTERNS_");
                                                    interpolatedStringHandler.AppendFormatted<int>(index9);
                                                    if (ImGui.Combo(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.RotateMotorsPattern[index9], array2, array2.Length))
                                                        Service.Configuration!.Save();
                                                    int num = triggerDevice.RotateMotorsPattern[index9];
                                                    ImGui.SameLine();
                                                    ImGui.SetNextItemWidth(180f);
                                                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 2);
                                                    interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                    interpolatedStringHandler.AppendLiteral("_SHOULD_ROTATE_MOTOR_");
                                                    interpolatedStringHandler.AppendFormatted<int>(index9);
                                                    interpolatedStringHandler.AppendLiteral("_THRESHOLD");
                                                    if (ImGui.SliderInt(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.RotateMotorsThreshold[index9], 0, 100))
                                                    {
                                                        if (triggerDevice.RotateMotorsThreshold[index9] > 0)
                                                            triggerDevice.RotateSelectedMotors[index9] = true;
                                                        Service.Configuration!.Save();
                                                    }
                                                }
                                            }
                                            ImGui.Indent(-20f);
                                        }
                                    }
                                    if (triggerDevice.Device.CanLinear)
                                    {
                                        if (ImGui.Checkbox(stringAndClear1 + "_SHOULD_LINEAR", ref triggerDevice.ShouldLinear))
                                        {
                                            triggerDevice.ShouldStop = false;
                                            Service.Configuration!.Save();
                                        }
                                        ImGui.SameLine();
                                        ImGui.Text("Should Linear");
                                        if (triggerDevice.ShouldLinear)
                                        {
                                            ImGui.Indent(20f);
                                            for (int index10 = 0; index10 < triggerDevice.Device.LinearMotors; ++index10)
                                            {
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 1);
                                                interpolatedStringHandler.AppendLiteral("Motor ");
                                                interpolatedStringHandler.AppendFormatted<int>(index10 + 1);
                                                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                                                ImGui.SameLine();
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
                                                interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                interpolatedStringHandler.AppendLiteral("_SHOULD_LINEAR_MOTOR_");
                                                interpolatedStringHandler.AppendFormatted<int>(index10);
                                                if (ImGui.Checkbox(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.LinearSelectedMotors[index10]))
                                                    Service.Configuration!.Save();
                                                if (triggerDevice.LinearSelectedMotors[index10])
                                                {
                                                    ImGui.SameLine();
                                                    ImGui.SetNextItemWidth(90f);
                                                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 2);
                                                    interpolatedStringHandler.AppendLiteral("###");
                                                    interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                    interpolatedStringHandler.AppendLiteral("_LINEAR_PATTERNS_");
                                                    interpolatedStringHandler.AppendFormatted<int>(index10);
                                                    if (ImGui.Combo(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.LinearMotorsPattern[index10], array2, array2.Length))
                                                        Service.Configuration!.Save();
                                                    int num = triggerDevice.LinearMotorsPattern[index10];
                                                    ImGui.SameLine();
                                                    ImGui.SetNextItemWidth(180f);
                                                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 2);
                                                    interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                    interpolatedStringHandler.AppendLiteral("_SHOULD_LINEAR_MOTOR_");
                                                    interpolatedStringHandler.AppendFormatted<int>(index10);
                                                    interpolatedStringHandler.AppendLiteral("_THRESHOLD");
                                                    if (ImGui.SliderInt(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.LinearMotorsThreshold[index10], 0, 100))
                                                    {
                                                        if (triggerDevice.LinearMotorsThreshold[index10] > 0)
                                                            triggerDevice.LinearSelectedMotors[index10] = true;
                                                        Service.Configuration!.Save();
                                                    }
                                                }
                                            }
                                            ImGui.Indent(-20f);
                                        }
                                    }
                                    if (triggerDevice.Device.CanOscillate)
                                    {
                                        if (ImGui.Checkbox(stringAndClear1 + "_SHOULD_OSCILLATE", ref triggerDevice.ShouldOscillate))
                                        {
                                            triggerDevice.ShouldStop = false;
                                            Service.Configuration!.Save();
                                        }
                                        ImGui.SameLine();
                                        ImGui.Text("Should Oscillate");
                                        if (triggerDevice.ShouldOscillate)
                                        {
                                            ImGui.Indent(20f);
                                            for (int index11 = 0; index11 < triggerDevice.Device.OscillateMotors; ++index11)
                                            {
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 1);
                                                interpolatedStringHandler.AppendLiteral("Motor ");
                                                interpolatedStringHandler.AppendFormatted<int>(index11 + 1);
                                                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
                                                ImGui.SameLine();
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 2);
                                                interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                interpolatedStringHandler.AppendLiteral("_SHOULD_OSCILLATE_MOTOR_");
                                                interpolatedStringHandler.AppendFormatted<int>(index11);
                                                if (ImGui.Checkbox(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.OscillateSelectedMotors[index11]))
                                                    Service.Configuration!.Save();
                                                if (triggerDevice.OscillateSelectedMotors[index11])
                                                {
                                                    ImGui.SameLine();
                                                    ImGui.SetNextItemWidth(90f);
                                                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 2);
                                                    interpolatedStringHandler.AppendLiteral("###");
                                                    interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                    interpolatedStringHandler.AppendLiteral("_OSCILLATE_PATTERNS_");
                                                    interpolatedStringHandler.AppendFormatted<int>(index11);
                                                    if (ImGui.Combo(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.OscillateMotorsPattern[index11], array2, array2.Length))
                                                        Service.Configuration!.Save();
                                                    int num = triggerDevice.OscillateMotorsPattern[index11];
                                                    ImGui.SameLine();
                                                    ImGui.SetNextItemWidth(180f);
                                                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 2);
                                                    interpolatedStringHandler.AppendFormatted(stringAndClear1);
                                                    interpolatedStringHandler.AppendLiteral("_SHOULD_OSCILLATE_MOTOR_");
                                                    interpolatedStringHandler.AppendFormatted<int>(index11);
                                                    interpolatedStringHandler.AppendLiteral("_THRESHOLD");
                                                    if (ImGui.SliderInt(interpolatedStringHandler.ToStringAndClear(), ref triggerDevice.OscillateMotorsThreshold[index11], 0, 100))
                                                    {
                                                        if (triggerDevice.OscillateMotorsThreshold[index11] > 0)
                                                            triggerDevice.OscillateSelectedMotors[index11] = true;
                                                        Service.Configuration!.Save();
                                                    }
                                                }
                                            }
                                            ImGui.Indent(-20f);
                                        }
                                    }
                                    if (ImGui.Button("Remove###" + stringAndClear1 + "_REMOVE"))
                                    {
                                        devices.RemoveAt(index7);
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 1);
                                        interpolatedStringHandler.AppendLiteral("DEBUG: removing ");
                                        interpolatedStringHandler.AppendFormatted<int>(index7);
                                        string stringAndClear2 = interpolatedStringHandler.ToStringAndClear();
                                        Logger.Log(stringAndClear2);
                                        Service.Configuration!.Save();
                                    }
                                }
                                ImGui.Indent(-10f);
                            }
                        }
                    }
                }
                else
                    ImGui.TextColored(ImGuiColors.DalamudRed, "Current selected trigger is null");
            }
            else if (this.triggersViewMode == "delete" && this.SelectedTrigger != null)
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, "Are you sure you want to delete trigger ID: " + this.SelectedTrigger.Name + ":" + this.SelectedTrigger.Id);
                if (ImGui.Button("Yes"))
                {
                    if (this.SelectedTrigger != null)
                    {
                        this.TriggerController.RemoveTrigger(this.SelectedTrigger);
                        this.SelectedTrigger = (Trigger)null;
                        Service.Configuration!.Save();
                    }
                    this.triggersViewMode = "default";
                }
                ImGui.SameLine();
                if (ImGui.Button("No"))
                {
                    this.SelectedTrigger = (Trigger)null;
                    this.triggersViewMode = "default";
                }
            }
            ImGui.EndChild();
        }

        if (ImGui.Button("Add"))
        {
            int num = 0;
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 1);
            interpolatedStringHandler.AppendLiteral("New Trigger ");
            interpolatedStringHandler.AppendFormatted<int>(num);
            Trigger trigger;
            for (trigger = new Trigger(interpolatedStringHandler.ToStringAndClear()); this.TriggerController.GetTriggers().Contains(trigger); trigger = new Trigger(interpolatedStringHandler.ToStringAndClear()))
            {
                ++num;
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 1);
                interpolatedStringHandler.AppendLiteral("New Trigger ");
                interpolatedStringHandler.AppendFormatted<int>(num);
            }
            this.TriggerController.AddTrigger(trigger);
            this.SelectedTrigger = trigger;
            this.triggersViewMode = "edit";
            Service.Configuration!.Save();
        }
        ImGui.SameLine();

        int num3 = ImGui.Button("Delete") ? 1 : 0;
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hold shift to avoid confirmation message");
        if (num3 != 0)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            if (io.KeyShift && this.SelectedTrigger != null)
            {
                this.TriggerController.RemoveTrigger(this.SelectedTrigger);
                this.SelectedTrigger = (Trigger)null;
                Service.Configuration!.Save();
            }
            this.triggersViewMode = "delete";
        }
        ImGui.SameLine();

        if (ImGui.Button("Import Triggers"))
        {
            if (!this.ConfigurationProfile.EXPORT_DIR.Equals(""))
            {
                try
                {
                    foreach (string file in Directory.GetFiles(this.ConfigurationProfile.EXPORT_DIR))
                    {
                        Trigger trigger = JsonConvert.DeserializeObject<Trigger>(File.ReadAllText(file));
                        this.TriggerController.RemoveTrigger(trigger);
                        this.TriggerController.AddTrigger(trigger);
                    }
                }
                catch
                {
                }
            }
        }
        ImGui.SameLine();
        if (!ImGui.Button("Export All") || this.ConfigurationProfile.EXPORT_DIR.Equals(""))
            return;
        foreach (Trigger trigger in this.TriggerController.GetTriggers())
            this.export_trigger(trigger);
    }

    public void DrawPatternsTab()
    {
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Add or edit a new pattern:");
        ImGui.Indent(20f);
        List<Pattern> customPatterns = this.Patterns.GetCustomPatterns();
        if (ImGui.BeginTable("###PATTERN_ADD_FORM", 3))
        {
            ImGui.TableSetupColumn("###PATTERN_ADD_FORM_COL1", (ImGuiTableColumnFlags)8, 100f);
            ImGui.TableSetupColumn("###PATTERN_ADD_FORM_COL2", (ImGuiTableColumnFlags)8, 300f);
            ImGui.TableSetupColumn("###PATTERN_ADD_FORM_COL3", (ImGuiTableColumnFlags)4);
            ImGui.TableNextColumn();
            ImGui.Text("Pattern Name:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(300f);
            if (ImGui.InputText("###PATTERNS_CURRENT_PATTERN_NAME_TO_ADD", ref this._tmp_currentPatternNameToAdd, 150))
                this._tmp_currentPatternNameToAdd = this._tmp_currentPatternNameToAdd.Trim();
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Pattern Value:");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(300f);
            if (ImGui.InputText("###PATTERNS_CURRENT_PATTERN_VALUE_TO_ADD", ref this._tmp_currentPatternValueToAdd, 500))
            {
                this._tmp_currentPatternValueToAdd = this._tmp_currentPatternValueToAdd.Trim();
                this._tmp_currentPatternValueState = !(this._tmp_currentPatternValueToAdd.Trim() == "") ? (Helpers.RegExpMatch(this._tmp_currentPatternValueToAdd, this.VALID_REGEXP_PATTERN) ? "valid" : "unvalid") : "unset";
            }
            ImGui.TableNextColumn();
            ImGuiComponents.HelpMarker("Example: 50:1000|100:2000 means 50% for 1000 milliseconds followed by 100% for 2000 milliseconds.");
            if (this._tmp_currentPatternNameToAdd.Trim() != "" && this._tmp_currentPatternValueState == "valid")
            {
                ImGui.TableNextColumn();
                if (ImGui.Button("Save"))
                {
                    this.Patterns.AddCustomPattern(new Pattern(this._tmp_currentPatternNameToAdd, this._tmp_currentPatternValueToAdd));
                    this.ConfigurationProfile.PatternList = this.Patterns.GetCustomPatterns();
                    Service.Configuration!.Save();
                    this._tmp_currentPatternNameToAdd = "";
                    this._tmp_currentPatternValueToAdd = "";
                    this._tmp_currentPatternValueState = "unset";
                }
            }
            ImGui.TableNextRow();
            if (this._tmp_currentPatternValueState == "unvalid")
            {
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.DalamudRed, "WRONG FORMAT!");
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.DalamudRed, "Format: <int>:<ms>|<int>:<ms>...");
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.DalamudRed, "Eg: 10:500|100:1000|20:500|0:0");
            }
            ImGui.EndTable();
        }
        ImGui.Indent(-20f);
        ImGui.Separator();
        if (customPatterns.Count == 0)
        {
            ImGui.Text("No custom patterns, please add some");
        }
        else
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Custom Patterns:");
            ImGui.Indent(20f);
            if (ImGui.BeginTable("###PATTERN_CUSTOM_LIST", 3))
            {
                ImGui.TableSetupColumn("###PATTERN_CUSTOM_LIST_COL1", (ImGuiTableColumnFlags)8, 100f);
                ImGui.TableSetupColumn("###PATTERN_CUSTOM_LIST_COL2", (ImGuiTableColumnFlags)8, 430f);
                ImGui.TableSetupColumn("###PATTERN_CUSTOM_LIST_COL3", (ImGuiTableColumnFlags)4);
                ImGui.TableNextColumn();
                ImGui.TextColored(ImGuiColors.DalamudGrey2, "Search name:");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(150f);
                ImGui.InputText("###PATTERN_SEARCH_BAR", ref this.CURRENT_PATTERN_SEARCHBAR, 200);
                ImGui.TableNextRow();
                for (int index = 0; index < customPatterns.Count; ++index)
                {
                    Pattern pattern = customPatterns[index];
                    if (Helpers.RegExpMatch(pattern.Name, this.CURRENT_PATTERN_SEARCHBAR))
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(pattern.Name ?? "");
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(pattern.Name ?? "");
                        ImGui.TableNextColumn();
                        string str = pattern.Value;
                        if (str.Length > 70)
                            str = str.Substring(0, 70) + "...";
                        ImGui.Text(str);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(pattern.Value ?? "");
                        ImGui.TableNextColumn();
                        if (ImGuiComponents.IconButton(index, (FontAwesomeIcon)61944))
                        {
                            if (!this.Patterns.RemoveCustomPattern(pattern))
                            {
                                Logger.Error("Could not remove pattern " + pattern.Name);
                            }
                            else
                            {
                                this.ConfigurationProfile.PatternList = this.Patterns.GetCustomPatterns();
                                Service.Configuration!.Save();
                            }
                        }
                        ImGui.SameLine();
                        if (ImGuiComponents.IconButton(index, (FontAwesomeIcon)62212))
                        {
                            this._tmp_currentPatternNameToAdd = pattern.Name;
                            this._tmp_currentPatternValueToAdd = pattern.Value;
                            this._tmp_currentPatternValueState = "valid";
                        }
                        ImGui.TableNextRow();
                    }
                }
                ImGui.EndTable();
            }
            ImGui.Indent(-20f);
        }
    }

    public void DrawHelpTab()
    {
        ImGui.TextWrapped(Main.GetHelp(Service.App.CommandName));
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Plugin information");
        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 1);
        interpolatedStringHandler.AppendLiteral("App version: ");
        interpolatedStringHandler.AppendFormatted<Version>(Assembly.GetExecutingAssembly().GetName().Version);
        ImGui.Text(interpolatedStringHandler.ToStringAndClear());
        interpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 1);
        interpolatedStringHandler.AppendLiteral("Config version: ");
        interpolatedStringHandler.AppendFormatted<int>(Service.Configuration!.Version);
        ImGui.Text(interpolatedStringHandler.ToStringAndClear());
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Pattern information");
        ImGui.TextWrapped("You should use a string separated by the | (pipe) symbol with a pair of <Intensity> and <Duration in milliseconds>.");
        ImGui.TextWrapped("Below is an example of a pattern that would vibe 1sec at 50pct intensity and 2sec at 100pct:");
        ImGui.TextWrapped("Pattern example:");
        this._tmp_void = "50:1000|100:2000";
        ImGui.InputText("###HELP_PATTERN_EXAMPLE", ref this._tmp_void, 50);
    }

    public string export_trigger(Trigger trigger)
    {
        if (this.ConfigurationProfile.EXPORT_DIR.Equals(""))
            return "No export directory has been set! Set one in Options.";
        try
        {
            File.WriteAllText(Path.Join(this.ConfigurationProfile.EXPORT_DIR, trigger.Name + ".json"), JsonConvert.SerializeObject((object)trigger, Formatting.Indented));
            return "Successfully exported trigger!";
        }
        catch
        {
            return "Something went wrong while exporting!";
        }
    }
}
