using Dalamud.Interface;
using Dalamud.Interface.Components;
using FFXIV_Vibe_Plugin.Commons;
using ImGuiNET;
using System;
using System.Diagnostics;

namespace FFXIV_Vibe_Plugin.UI.Components;

internal class ButtonLink
{
    public static void Draw(string text, string link, FontAwesomeIcon Icon)
    {
        if (ImGuiComponents.IconButton(Icon))
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = link,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Could not open repoUrl: " + link, ex);
            }
        }

        if (!ImGui.IsItemHovered())
            return;

        ImGui.SetTooltip(text);
    }
}
