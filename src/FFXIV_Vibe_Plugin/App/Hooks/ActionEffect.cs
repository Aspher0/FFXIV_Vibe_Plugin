using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using Lumina.Excel;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FFXIV_Vibe_Plugin.Hooks;

internal class ActionEffect
{
    private readonly ExcelSheet<Lumina.Excel.Sheets.Action>? LuminaActionSheet;
    private Hook<HOOK_ReceiveActionEffectDelegate> receiveActionEffectHook;

    public event EventHandler<HookActionEffects_ReceivedEventArgs>? ReceivedEvent;

    public ActionEffect()
    {
        InitHook();
        LuminaActionSheet = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
    }

    public void Dispose()
    {
        receiveActionEffectHook?.Disable();
        receiveActionEffectHook?.Dispose();
    }

    private void InitHook()
    {
        try
        {
            // Found on: https://github.com/perchbirdd/DamageInfoPlugin/blob/main/DamageInfoPlugin/DamageInfoPlugin.cs#L126
            var receiveActionEffectFuncPtr = Service.Scanner.ScanText("40 55 53 56 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70");
            receiveActionEffectHook = Service.InteropProvider.HookFromAddress<HOOK_ReceiveActionEffectDelegate>(receiveActionEffectFuncPtr, new HOOK_ReceiveActionEffectDelegate(ReceiveActionEffect), 0);
            receiveActionEffectHook.Enable();
        }
        catch (Exception ex)
        {
            Dispose();
            Logger.Warn("Encountered an error loading HookActionEffect: " + ex.Message + ". Disabling it...");
            throw;
        }

        Logger.Log("HookActionEffect was correctly enabled!");
    }

    private void ReceiveActionEffect(int sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
    {
        Structures.Spell spell = new Structures.Spell();

        try
        {
            string nameFromSourceId = GetCharacterNameFromSourceId(sourceId);

            unsafe
            {
                uint actionId = *(uint*)((IntPtr)effectHeader.ToPointer() + new IntPtr(2) * 4);
                int num1 = *(ushort*)((IntPtr)effectHeader.ToPointer() + new IntPtr(14) * 2);
                int num2 = *(ushort*)((IntPtr)effectHeader.ToPointer() - new IntPtr(7) * 2);
                byte count = *(byte*)(effectHeader + new IntPtr(33));

                Structures.EffectEntry effectEntry = *(Structures.EffectEntry*)effectArray;

                string spellName = GetSpellName(actionId, true);
                int[] amounts = GetAmounts(count, effectArray);
                float averageAmount = ComputeAverageAmount(amounts);

                List<Structures.Player> allTarget = GetAllTarget(count, effectTrail, amounts);

                spell.Id = (int)actionId;
                spell.Name = spellName;
                spell.Player = new Structures.Player(sourceId, nameFromSourceId);
                spell.Amounts = amounts;
                spell.AmountAverage = averageAmount;
                spell.Targets = allTarget;
                spell.DamageType = Structures.DamageType.Unknown;
                spell.ActionEffectType = allTarget.Count != 0 ? effectEntry.type : Structures.ActionEffectType.Any;

                DispatchReceivedEvent(spell);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(ex.Message + " " + ex.StackTrace);
        }

        RestoreOriginalHook(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
    }

    private void RestoreOriginalHook(
      int sourceId,
      IntPtr sourceCharacter,
      IntPtr pos,
      IntPtr effectHeader,
      IntPtr effectArray,
      IntPtr effectTrail)
    {
        if (receiveActionEffectHook == null)
            return;

        receiveActionEffectHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
    }

    private unsafe int[] GetAmounts(byte count, IntPtr effectArray)
    {
        int[] amounts = new int[count];

        int num1 = count;

        int capacity = 0;

        if (num1 == 0)
            capacity = 0;
        else if (num1 == 1)
            capacity = 8;
        else if (num1 <= 8)
            capacity = 64;
        else if (num1 <= 16)
            capacity = 128;
        else if (num1 <= 24)
            capacity = 192;
        else if (num1 <= 32)
            capacity = 256;

        List<Structures.EffectEntry> effectEntryList = new List<Structures.EffectEntry>(capacity);

        for (int index = 0; index < capacity; ++index)
            effectEntryList.Add(*(Structures.EffectEntry*)(effectArray + (index * 8)));

        int index1 = 0;

        for (int index2 = 0; index2 < effectEntryList.Count; ++index2)
        {
            if (index2 % 8 == 0)
            {
                uint num2 = effectEntryList[index2].value;

                if (effectEntryList[index2].mult != 0)
                    num2 += 65536U * effectEntryList[index2].mult;

                if (index1 < count)
                    amounts[index1] = (int)num2;

                ++index1;
            }
        }

        return amounts;
    }

    private static int ComputeAverageAmount(int[] amounts)
    {
        int num = 0;

        for (int index = 0; index < amounts.Length; ++index)
            num += amounts[index];

        return num != 0 ? num / amounts.Length : num;
    }

    private unsafe List<Structures.Player> GetAllTarget(byte count,IntPtr effectTrail,int[] amounts)
    {
        List<Structures.Player> allTarget = new List<Structures.Player>();

        if (count >= 1)
        {
            ulong[] numArray = new ulong[count];

            for (int index = 0; index < count; ++index)
            {
                numArray[index] = (ulong)*(long*)(effectTrail + (index * 8));

                int sourceId = (int)numArray[index];
                string nameFromSourceId = GetCharacterNameFromSourceId(sourceId);

                Structures.Player player = new Structures.Player();
                ref Structures.Player local = ref player;

                int id = sourceId;
                string name = nameFromSourceId;

                local = new Structures.Player(id, name, $"{amounts[index]}");
                allTarget.Add(player);
            }
        }
        return allTarget;
    }

    private string GetSpellName(uint actionId, bool withId)
    {
        if (LuminaActionSheet == null)
        {
            Logger.Warn("HookActionEffect.GetSpellName: LuminaActionSheet is null");
            return "***LUMINA ACTION SHEET NOT LOADED***";
        }

        try
        {
            Lumina.Excel.Sheets.Action row = LuminaActionSheet.GetRow(actionId);
            string spellName = "";

            if (withId)
            {
                spellName = $"{row.RowId}:";
            }
            if (!row.Name.IsEmpty)
            {
                spellName = spellName + $"{row.Name}";
            }

            return spellName;
        }
        catch (ArgumentOutOfRangeException)
        {
            return "!Unknown Spell Name!";
        }
    }

    private string GetCharacterNameFromSourceId(int sourceId)
    {
        IGameObject gameObject = Service.GameObjects!.SearchById((uint)sourceId);

        string nameFromSourceId = "";

        if (gameObject != null)
            nameFromSourceId = gameObject.Name.TextValue;

        return nameFromSourceId;
    }

    protected virtual void DispatchReceivedEvent(Structures.Spell spell)
    {
        HookActionEffects_ReceivedEventArgs e = new HookActionEffects_ReceivedEventArgs();
        e.Spell = spell;

        EventHandler<HookActionEffects_ReceivedEventArgs> receivedEvent = ReceivedEvent;

        if (receivedEvent == null)
            return;

        receivedEvent(this, e);
    }

    private delegate void HOOK_ReceiveActionEffectDelegate(
      int sourceId,
      IntPtr sourceCharacter,
      IntPtr pos,
      IntPtr effectHeader,
      IntPtr effectArray,
      IntPtr effectTrail);
}
