using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.Commons;
using System.Collections.Generic;
using System.Linq;

namespace FFXIV_Vibe_Plugin.Triggers;

public class TriggersController
{
    private readonly PlayerStats PlayerStats;
    private ConfigurationProfile Profile;
    private List<Trigger> Triggers = new List<Trigger>();

    public TriggersController(PlayerStats playerStats, ConfigurationProfile profile)
    {
        PlayerStats = playerStats;
        Profile = profile;
    }

    public void SetProfile(ConfigurationProfile profile)
    {
        Profile = profile;
        Triggers = profile.TRIGGERS;
    }

    public List<Trigger> GetTriggers() => Triggers;

    public void AddTrigger(Trigger trigger) => Triggers.Add(trigger);

    public void RemoveTrigger(Trigger trigger) => Triggers.Remove(trigger);

    public List<Trigger> CheckTrigger_Chat(
      XivChatType chatType,
      string ChatFromPlayerName,
      string ChatMsg)
    {
        List<Trigger> triggerList = new List<Trigger>();
        ChatFromPlayerName = ChatFromPlayerName.Trim().ToLower();

        for (int index = 0; index < Triggers.Count; ++index)
        {
            Trigger trigger = Triggers[index];
            if (trigger.Enabled && (chatType == XivChatType.Echo || Helpers.RegExpMatch(ChatFromPlayerName, trigger.FromPlayerName) &&
               (trigger.AllowedChatTypes.Count <= 0 || trigger.AllowedChatTypes.Any(ct => ct == (int)chatType))) && trigger.Kind == 0 &&
               Helpers.RegExpMatch(ChatMsg, trigger.ChatText))
            {
                if (Profile.VERBOSE_CHAT)
                    Logger.Debug($"ChatTrigger matched {trigger.ChatText}<>{ChatMsg}, adding {trigger}");

                triggerList.Add(trigger);
            }
        }

        return triggerList;
    }

    public List<Trigger> CheckTrigger_Spell(Structures.Spell spell)
    {
        List<Trigger> triggerList = new List<Trigger>();

        string text = spell.Name != null ? spell.Name.Trim() : "";

        for (int index = 0; index < Triggers.Count; ++index)
        {
            Trigger trigger = Triggers[index];

            if (trigger.Enabled && Helpers.RegExpMatch(spell.Player.Name, trigger.FromPlayerName) && trigger.Kind == 1 &&
                Helpers.RegExpMatch(text, trigger.SpellText) && (trigger.ActionEffectType == 0 || (Structures.ActionEffectType)trigger.ActionEffectType == spell.ActionEffectType) &&
                (trigger.ActionEffectType != 3 && trigger.ActionEffectType != 4 || trigger.AmountMinValue < spell.AmountAverage && trigger.AmountMaxValue > spell.AmountAverage))
            {
                DIRECTION spellDirection = GetSpellDirection(spell);

                if (trigger.Direction == 0 || spellDirection == (DIRECTION)trigger.Direction)
                {
                    if (Profile.VERBOSE_SPELL)
                        Logger.Debug($"SpellTrigger matched {spell}, adding {trigger}");

                    triggerList.Add(trigger);
                }
            }
        }

        return triggerList;
    }

    public List<Trigger> CheckTrigger_HPChanged(int currentHP, float percentageHP)
    {
        List<Trigger> triggerList = new List<Trigger>();

        for (int index = 0; index < Triggers.Count; ++index)
        {
            Trigger trigger = Triggers[index];

            if (trigger.Enabled && trigger.Kind == 2)
            {
                if (trigger.AmountInPercentage)
                {
                    if ((double)percentageHP < trigger.AmountMinValue || (double)percentageHP > trigger.AmountMaxValue)
                        continue;
                }
                else if (trigger.AmountMinValue >= currentHP || trigger.AmountMaxValue <= currentHP)
                    continue;

                if (trigger.AmountInPercentage)
                    Logger.Debug($"HPChanged Triggers (in percentage): {percentageHP}%, {trigger.AmountMinValue}, {trigger.AmountMaxValue}");
                else
                    Logger.Debug($"HPChanged Triggers: {currentHP}, {trigger.AmountMinValue}, {trigger.AmountMaxValue}");

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

        for (int index1 = 0; index1 < Triggers.Count; ++index1)
        {
            Trigger trigger = Triggers[index1];

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
                                    if (num < trigger.AmountMinValue || num > trigger.AmountMaxValue)
                                        continue;
                                }
                                else if (trigger.AmountMinValue >= currentHp || trigger.AmountMaxValue <= currentHp)
                                    continue;

                                if (trigger.AmountInPercentage)
                                    Logger.Debug($"HPChangedOther for {text} Triggers (in percentage): {num}%, {trigger.AmountMinValue}, {trigger.AmountMaxValue}");
                                else
                                    Logger.Debug($"HPChangedOther for {text} Triggers: {currentHp}, {trigger.AmountMinValue}, {trigger.AmountMaxValue}");

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
        string playerName = PlayerStats.GetPlayerName();

        List<Structures.Player> playerList = new List<Structures.Player>();

        if (spell.Targets != null)
            playerList = spell.Targets;

        if (playerList.Count >= 1 && playerList[0].Name != playerName)
            return DIRECTION.Outgoing;

        return spell.Player.Name != playerName ? DIRECTION.Incoming : DIRECTION.Self;
    }
}
