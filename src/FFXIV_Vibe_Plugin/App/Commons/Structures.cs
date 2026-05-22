using System.Collections.Generic;

namespace FFXIV_Vibe_Plugin.Commons;

public class Structures
{
    public enum ActionEffectKind : int
    {
        Any = 0,
        Miss = 1,
        FullResist = 2,
        Damage = 3,
        Heal = 4,
        BlockedDamage = 5,
        ParriedDamage = 6,
        Invulnerable = 7,
        NoEffect = 8,
        Unknown9 = 9,
        MpLoss = 10,
        MpGain = 11,
        TpLoss = 12,
        GpGain = 13,

        ApplyStatusTarget = 14,
        ApplyStatusSource = 15,
        RecoveredStatus = 16,
        LoseStatusTarget = 17,
        LoseStatusSource = 18,
        StatusNoEffect = 20,

        EnminityIndex = 24,
        EnmityAmountUp = 25,
        Unk_EnmityAmountDown = 26,      // Unsure but makes sense with the position
        StartActionCombo = 27,
        ComboStep = 28,

        Knockback = 31,
        Attract = 32,
        Attract2 = 33,
        Dash = 34,
        Dash2 = 35,
        Dash3 = 36,

        MountSfx = 39,

        StatusDispel1 = 47,
        StatusDispel2 = 48,
        StatusDispel3 = 49,

        InstantDeath = 50,              // Some sources mention this is Revive LB, triggers LogMessage 519
        InstantDeath2 = 51,             // Triggers LogMessage 519

        FullResistStatus = 55,          // Trigger LogMessage 596
        Vulnerability = 57,             // Triggers LogMessage 456, "been sentenced to death!"

        SxtBattleLogMessage = 60,
        ActionChange = 61,              // Some sources say this is JobGauge, unsure.
        Unknown62 = 62,                 // Some sources mention this is Gaining WAR Gauge
        ToggleVis = 65,
        SetModelScale = 68,
        Unk_SetModelState = 73,         // Not up-to-date, might be wrong

        SetHP = 74,                     // e.g. zodiark's kokytos
        PartialInvulnerable = 75,
        Interrupt = 76,

        Unk_MountJapaneseVersion = 240, // Not up-to-date, might be wrong
        Unknown = 255,
    }

    public enum DamageType
    {
        Unknown = 0,
        Slashing = 1,
        Piercing = 2,
        Blunt = 3,
        Magic = 5,
        Darkness = 6,
        Physical = 7,
        LimitBreak = 8,
    }

    public struct Player
    {
        public uint Id;
        public string Name;
        public string? Info;

        public Player(uint id, string name, string? info = null)
        {
            Id = id;
            Name = name;
            Info = info;
        }

        public override string ToString()
        {
            if (Info != null)
                return $"{Name}({Id}) [info:{Info}]";

            return $"{Name}({Id})";
        }
    }

    public struct Spell
    {
        public uint Id;
        public string Name;
        public Player Player;
        public int[]? Amounts;
        public float AmountAverage;
        public List<Player>? Targets;
        public DamageType DamageType;
        public ActionEffectKind ActionEffectType;

        public Spell(
          uint id,
          string name,
          Player player,
          int[]? amounts,
          float amountAverage,
          List<Player>? targets,
          DamageType damageType,
          ActionEffectKind actionEffectType)
        {
            Name = "Undefined_Spell_Name";
            DamageType = DamageType.Unknown;
            ActionEffectType = ActionEffectKind.Any;
            Id = id;
            Name = name;
            Player = player;
            Amounts = amounts;
            AmountAverage = amountAverage;
            Targets = targets;
            DamageType = damageType;
            ActionEffectType = actionEffectType;
        }

        public override string ToString()
        {
            string str = "";

            if (Targets != null)
                str = Targets.Count <= 0 ? "*no target*" : string.Join(",", Targets);

            return $"{Player} casts {Name}#{ActionEffectType} on: {str}. Avg: {AmountAverage}";
        }
    }
}
