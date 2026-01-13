using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace FFXIV_Vibe_Plugin.Triggers;

public class Trigger : IComparable<Trigger>, IEquatable<Trigger>
{
    private static readonly int _initAmountMinValue = -1;
    private static readonly int _initAmountMaxValue = 10000000;
    public bool Enabled = true;
    public int SortOder = -1;
    public readonly string Id = "";
    public string Name = "";
    public string Description = "";
    public KIND Kind;
    public int ActionEffectType;
    public int Direction;
    public string ChatText = "hello world";
    public string SpellText = "";
    public int AmountMinValue = _initAmountMinValue;
    public int AmountMaxValue = _initAmountMaxValue;
    public bool AmountInPercentage;
    public string FromPlayerName = "";
    public float StartAfter;
    public float StopAfter;
    public int Priority;
    public readonly List<int> AllowedChatTypes = new List<int>();
    public List<TriggerDevice> Devices = new List<TriggerDevice>();

    public Trigger(string name)
    {
        Name = name;
        var randomInt8Digits = new byte[8];
        RandomNumberGenerator.Fill(randomInt8Digits);
        Id = BitConverter.ToString(SHA256.Create().ComputeHash(randomInt8Digits)).Replace("-", string.Empty);
    }

    public override string ToString() => $"Trigger(name={Name}, id={GetShortID()})";

    public int CompareTo(Trigger? other) => other == null ? 1 : other.Name.CompareTo(Name);

    public bool Equals(Trigger? other) => other != null && Name.Equals(other.Name);

    public string GetShortID() => Id.Substring(0, 12);

    public void Reset()
    {
        AmountMaxValue = _initAmountMaxValue;
        AmountMinValue = _initAmountMinValue;
    }
}
