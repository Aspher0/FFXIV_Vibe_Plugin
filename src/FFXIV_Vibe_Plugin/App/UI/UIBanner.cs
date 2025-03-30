using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Device;
using FFXIV_Vibe_Plugin.UI.Components;
using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;

#nullable enable
namespace FFXIV_Vibe_Plugin.UI
{
    internal class UIBanner
    {
        public static void Draw(
          int frameCounter,
          string donationLink,
          string KofiLink,
          DevicesController devicesController)
        {
            ImGui.Columns(1, "###main_header", false);
            if (devicesController.IsConnected())
            {
                int count = devicesController.GetDevices().Count;
                ImGui.TextColored(ImGuiColors.ParsedGreen, "You are connnected!");
                ImGui.SameLine();
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 1);
                interpolatedStringHandler.AppendLiteral("/ Number of device(s): ");
                interpolatedStringHandler.AppendFormatted<int>(count);
                ImGui.Text(interpolatedStringHandler.ToStringAndClear());
            }
            else
                ImGui.TextColored(ImGuiColors.ParsedGrey, "Your are not connected!");
        }
    }
}
