using Dalamud.Interface.Colors;
using FFXIV_Vibe_Plugin.Device;
using ImGuiNET;
using System.Runtime.CompilerServices;

namespace FFXIV_Vibe_Plugin.UI;

internal class UIBanner
{
    public static void Draw(int frameCounter, string donationLink, string KofiLink, DevicesController devicesController)
    {
        ImGui.Columns(1, "###main_header", false);

        if (devicesController.IsConnected())
        {
            int count = devicesController.GetDevices().Count;

            ImGui.TextColored(ImGuiColors.ParsedGreen, "You are connnected!");
            ImGui.SameLine();

            ImGui.Text($"/ Number of device(s): {count}");
        }
        else
            ImGui.TextColored(ImGuiColors.ParsedGrey, "Your are not connected!");
    }
}
