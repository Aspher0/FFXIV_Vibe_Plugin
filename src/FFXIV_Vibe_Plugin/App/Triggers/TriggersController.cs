using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

#nullable enable
namespace FFXIV_Vibe_Plugin.Triggers
{
    public class TriggersController
    {
        private readonly PlayerStats PlayerStats;
        private ConfigurationProfile Profile;
        private List<Trigger> Triggers = new List<Trigger>();

        public TriggersController(PlayerStats playerStats, ConfigurationProfile profile)
        {
            this.PlayerStats = playerStats;
            this.Profile = profile;
        }

        public void SetProfile(ConfigurationProfile profile)
        {
            this.Profile = profile;
            this.Triggers = profile.TRIGGERS;
        }

        public List<Trigger> GetTriggers() => this.Triggers;

        public void AddTrigger(Trigger trigger) => this.Triggers.Add(trigger);

        public void RemoveTrigger(Trigger trigger) => this.Triggers.Remove(trigger);

        public List<Trigger> CheckTrigger_Chat(
          XivChatType chatType,
          string ChatFromPlayerName,
          string ChatMsg)
        {
            List<Trigger> triggerList = new List<Trigger>();
            ChatFromPlayerName = ChatFromPlayerName.Trim().ToLower();
            for (int index = 0; index < this.Triggers.Count; ++index)
            {
                Trigger trigger = this.Triggers[index];
                if (trigger.Enabled && (chatType == XivChatType.Echo || Helpers.RegExpMatch(ChatFromPlayerName, trigger.FromPlayerName) && (trigger.AllowedChatTypes.Count <= 0 || trigger.AllowedChatTypes.Any(ct => (int)ct == (int)chatType))) && trigger.Kind == 0 && Helpers.RegExpMatch(ChatMsg, trigger.ChatText))
                {
                    if (this.Profile.VERBOSE_CHAT)
                    {
                        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 3);
                        interpolatedStringHandler.AppendLiteral("ChatTrigger matched ");
                        interpolatedStringHandler.AppendFormatted(trigger.ChatText);
                        interpolatedStringHandler.AppendLiteral("<>");
                        interpolatedStringHandler.AppendFormatted(ChatMsg);
                        interpolatedStringHandler.AppendLiteral(", adding ");
                        interpolatedStringHandler.AppendFormatted<Trigger>(trigger);
                        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                        Logger.Debug(stringAndClear);
                    }
                    triggerList.Add(trigger);
                }
            }
            return triggerList;
        }

        public List<Trigger> CheckTrigger_Spell(Structures.Spell spell)
        {
            List<Trigger> triggerList = new List<Trigger>();
            string text = spell.Name != null ? spell.Name.Trim() : "";
            for (int index = 0; index < this.Triggers.Count; ++index)
            {
                Trigger trigger = this.Triggers[index];
                if (trigger.Enabled && Helpers.RegExpMatch(spell.Player.Name, trigger.FromPlayerName) && trigger.Kind == 1 && Helpers.RegExpMatch(text, trigger.SpellText) && (trigger.ActionEffectType == 0 || (Structures.ActionEffectType)trigger.ActionEffectType == spell.ActionEffectType) && (trigger.ActionEffectType != 3 && trigger.ActionEffectType != 4 || (double)trigger.AmountMinValue < (double)spell.AmountAverage && (double)trigger.AmountMaxValue > (double)spell.AmountAverage))
                {
                    DIRECTION spellDirection = this.GetSpellDirection(spell);
                    if (trigger.Direction == 0 || spellDirection == (DIRECTION)trigger.Direction)
                    {
                        if (this.Profile.VERBOSE_SPELL)
                        {
                            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 2);
                            interpolatedStringHandler.AppendLiteral("SpellTrigger matched ");
                            interpolatedStringHandler.AppendFormatted<Structures.Spell>(spell);
                            interpolatedStringHandler.AppendLiteral(", adding ");
                            interpolatedStringHandler.AppendFormatted<Trigger>(trigger);
                            string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                            Logger.Debug(stringAndClear);
                        }
                        triggerList.Add(trigger);
                    }
                }
            }
            return triggerList;
        }

        public List<Trigger> CheckTrigger_HPChanged(int currentHP, float percentageHP)
        {
            List<Trigger> triggerList = new List<Trigger>();
            for (int index = 0; index < this.Triggers.Count; ++index)
            {
                Trigger trigger = this.Triggers[index];
                if (trigger.Enabled && trigger.Kind == 2)
                {
                    if (trigger.AmountInPercentage)
                    {
                        if ((double)percentageHP < (double)trigger.AmountMinValue || (double)percentageHP > (double)trigger.AmountMaxValue)
                            continue;
                    }
                    else if (trigger.AmountMinValue >= currentHP || trigger.AmountMaxValue <= currentHP)
                        continue;
                    DefaultInterpolatedStringHandler interpolatedStringHandler;
                    if (trigger.AmountInPercentage)
                    {
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 3);
                        interpolatedStringHandler.AppendLiteral("HPChanged Triggers (in percentage): ");
                        interpolatedStringHandler.AppendFormatted<float>(percentageHP);
                        interpolatedStringHandler.AppendLiteral("%, ");
                        interpolatedStringHandler.AppendFormatted<int>(trigger.AmountMinValue);
                        interpolatedStringHandler.AppendLiteral(", ");
                        interpolatedStringHandler.AppendFormatted<int>(trigger.AmountMaxValue);
                        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                        Logger.Debug(stringAndClear);
                    }
                    else
                    {
                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 3);
                        interpolatedStringHandler.AppendLiteral("HPChanged Triggers: ");
                        interpolatedStringHandler.AppendFormatted<int>(currentHP);
                        interpolatedStringHandler.AppendLiteral(", ");
                        interpolatedStringHandler.AppendFormatted<int>(trigger.AmountMinValue);
                        interpolatedStringHandler.AppendLiteral(", ");
                        interpolatedStringHandler.AppendFormatted<int>(trigger.AmountMaxValue);
                        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                        Logger.Debug(stringAndClear);
                    }
                    triggerList.Add(trigger);
                }
            }
            return triggerList;
        }

        public List<Trigger> CheckTrigger_HPChangedOther(IPartyList partyList)
        {
            List<Trigger> triggerList = new List<Trigger>();
            if (partyList == null)
                return triggerList;
            for (int index1 = 0; index1 < this.Triggers.Count; ++index1)
            {
                Trigger trigger = this.Triggers[index1];
                if (trigger.Enabled && trigger.Kind == 3)
                {
                    int length = partyList.Length;
                    for (int index2 = 0; index2 < length; ++index2)
                    {
                        IPartyMember party = partyList[index2];
                        if (party != null)
                        {
                            string text = party.Name.ToString();
                            if (Helpers.RegExpMatch(text, trigger.FromPlayerName))
                            {
                                uint maxHp = party.MaxHP;
                                uint currentHp = party.CurrentHP;
                                if (maxHp != 0U)
                                {
                                    uint num = currentHp * 100U / maxHp;
                                    if (trigger.AmountInPercentage)
                                    {
                                        if ((long)num < (long)trigger.AmountMinValue || (long)num > (long)trigger.AmountMaxValue)
                                            continue;
                                    }
                                    else if ((long)trigger.AmountMinValue >= (long)currentHp || (long)trigger.AmountMaxValue <= (long)currentHp)
                                        continue;
                                    DefaultInterpolatedStringHandler interpolatedStringHandler;
                                    if (trigger.AmountInPercentage)
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 4);
                                        interpolatedStringHandler.AppendLiteral("HPChangedOther for ");
                                        interpolatedStringHandler.AppendFormatted(text);
                                        interpolatedStringHandler.AppendLiteral(" Triggers (in percentage): ");
                                        interpolatedStringHandler.AppendFormatted<uint>(num);
                                        interpolatedStringHandler.AppendLiteral("%, ");
                                        interpolatedStringHandler.AppendFormatted<int>(trigger.AmountMinValue);
                                        interpolatedStringHandler.AppendLiteral(", ");
                                        interpolatedStringHandler.AppendFormatted<int>(trigger.AmountMaxValue);
                                        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                        Logger.Debug(stringAndClear);
                                    }
                                    else
                                    {
                                        interpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 4);
                                        interpolatedStringHandler.AppendLiteral("HPChangedOther for ");
                                        interpolatedStringHandler.AppendFormatted(text);
                                        interpolatedStringHandler.AppendLiteral(" Triggers: ");
                                        interpolatedStringHandler.AppendFormatted<uint>(currentHp);
                                        interpolatedStringHandler.AppendLiteral(", ");
                                        interpolatedStringHandler.AppendFormatted<int>(trigger.AmountMinValue);
                                        interpolatedStringHandler.AppendLiteral(", ");
                                        interpolatedStringHandler.AppendFormatted<int>(trigger.AmountMaxValue);
                                        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                        Logger.Debug(stringAndClear);
                                    }
                                    triggerList.Add(trigger);
                                }
                            }
                        }
                    }
                }
            }
            return triggerList;
        }

        public DIRECTION GetSpellDirection(Structures.Spell spell)
        {
            string playerName = this.PlayerStats.GetPlayerName();
            List<Structures.Player> playerList = new List<Structures.Player>();
            if (spell.Targets != null)
                playerList = spell.Targets;
            if (playerList.Count >= 1 && playerList[0].Name != playerName)
                return DIRECTION.Outgoing;
            return spell.Player.Name != playerName ? DIRECTION.Incoming : DIRECTION.Self;
        }
    }
}
