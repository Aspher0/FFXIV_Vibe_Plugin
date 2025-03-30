using System;

namespace FFXIV_Vibe_Plugin.Triggers;

[Serializable]
public class ChatTrigger : IComparable
{
    public ChatTrigger(int intensity, string text)
    {
        Intensity = intensity;
        Text = text;
    }

    public int Intensity { get; }

    public string Text { get; }

    public override string ToString() => $"Trigger(intensity: {Intensity}, text: '{Text}')";

    public string ToConfigString() => $"{Intensity} {Text}";

    public int CompareTo(object? obj) => Intensity.CompareTo(obj is ChatTrigger chatTrigger ? chatTrigger.Intensity : 0);
}
