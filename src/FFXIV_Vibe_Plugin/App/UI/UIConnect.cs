using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Device;
using ImGuiNET;
using System.Numerics;

#nullable enable
namespace FFXIV_Vibe_Plugin.UI
{
    internal class UIConnect
    {
        public static void Draw(
          ConfigurationProfile configurationProfile,
          DevicesController devicesController)
        {
            ImGui.Spacing();
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Server address & port");
            ImGui.BeginChild("###Server", new Vector2(-1f, 40f), true);
            string buttplugServerHost = configurationProfile.BUTTPLUG_SERVER_HOST;
            ImGui.SetNextItemWidth(200f);
            if (ImGui.InputText("##serverHost", ref buttplugServerHost, 99U))
            {
                configurationProfile.BUTTPLUG_SERVER_HOST = buttplugServerHost.Trim().ToLower();
                Service.Configuration!.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Go in the option tab if you need WSS (default: 127.0.0.1)");
            ImGui.SameLine();
            int buttplugServerPort = configurationProfile.BUTTPLUG_SERVER_PORT;
            ImGui.SetNextItemWidth(100f);
            if (ImGui.InputInt("##serverPort", ref buttplugServerPort, 10))
            {
                configurationProfile.BUTTPLUG_SERVER_PORT = buttplugServerPort;
                Service.Configuration!.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Use '-1' as port number to not define it (default: 12345)");
            ImGui.EndChild();
            ImGui.Spacing();
            ImGui.BeginChild("###Main_Connection", new Vector2(-1f, 40f), true);
            if (!devicesController.IsConnected())
            {
                if (ImGui.Button("Connect", new Vector2(100f, 24f)))
                    Service.App.Command_DeviceController_Connect();
            }
            else if (ImGui.Button("Disconnect", new Vector2(100f, 24f)))
                devicesController.Disconnect();
            ImGui.SameLine();
            bool autoConnect = configurationProfile.AUTO_CONNECT;
            if (ImGui.Checkbox("Automatically connects. ", ref autoConnect))
            {
                configurationProfile.AUTO_CONNECT = autoConnect;
                Service.Configuration!.Save();
            }
            ImGui.EndChild();
        }
    }
}
