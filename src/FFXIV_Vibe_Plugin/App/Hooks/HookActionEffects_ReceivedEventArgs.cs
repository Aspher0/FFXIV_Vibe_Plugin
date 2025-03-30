using FFXIV_Vibe_Plugin.Commons;
using System;

namespace FFXIV_Vibe_Plugin.Hooks;

internal class HookActionEffects_ReceivedEventArgs : EventArgs
{
    public Structures.Spell Spell { get; set; }
}
