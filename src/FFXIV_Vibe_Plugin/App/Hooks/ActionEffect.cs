using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using FFXIV_Vibe_Plugin.Commons;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel;
using NoireLib;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace FFXIV_Vibe_Plugin.Hooks;

internal class ActionEffect
{
    private readonly ExcelSheet<Lumina.Excel.Sheets.Action>? LuminaActionSheet;
    private Hook<ActionEffectHandler.Delegates.Receive> receiveActionEffectHook;

    public event EventHandler<HookActionEffects_ReceivedEventArgs>? ReceivedEvent;

    public ActionEffect()
    {
        InitHook();
        LuminaActionSheet = NoireService.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
    }

    public void Dispose()
    {
        receiveActionEffectHook?.Disable();
        receiveActionEffectHook?.Dispose();
    }

    private unsafe void InitHook()
    {
        NoireService.Framework.RunOnFrameworkThread(() =>
        {
            try
            {
                receiveActionEffectHook = NoireService.GameInteropProvider.HookFromAddress<ActionEffectHandler.Delegates.Receive>(
                    ActionEffectHandler.MemberFunctionPointers.Receive,
                    ReceiveActionEffect
                );
                receiveActionEffectHook.Enable();
            }
            catch (Exception ex)
            {
                Dispose();
                Logger.Warn("Encountered an error loading HookActionEffect: " + ex.Message + ". Disabling it...");
                throw;
            }

            Logger.Log("HookActionEffect was correctly enabled!");
        });
    }

    private unsafe void ReceiveActionEffect(uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targetEntityIds)
    {
        Structures.Spell spell = new Structures.Spell();

        try
        {
            string nameFromSourceId = GetCharacterNameFromSourceId(casterEntityId);

            uint actionId = header->ActionId;
            byte count = header->NumTargets;

            var effectEntry = effects->Effects[0]; // ??

            string spellName = GetSpellName(actionId, true);
            int[] amounts = GetAmounts(count, effects);
            float averageAmount = ComputeAverageAmount(amounts);

            List<Structures.Player> allTarget = GetAllTarget(count, targetEntityIds, amounts);

            spell.Id = actionId;
            spell.Name = spellName;
            spell.Player = new Structures.Player(casterEntityId, nameFromSourceId);
            spell.Amounts = amounts;
            spell.AmountAverage = averageAmount;
            spell.Targets = allTarget;
            spell.DamageType = Structures.DamageType.Unknown;
            spell.ActionEffectType = allTarget.Count != 0 ? (Structures.ActionEffectType)effectEntry.Type : Structures.ActionEffectType.Any;

            DispatchReceivedEvent(spell);
        }
        catch (Exception ex)
        {
            Logger.Log(ex.Message + " " + ex.StackTrace);
        }

        RestoreOriginalHook(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);
    }

    private unsafe void RestoreOriginalHook(
      uint casterEntityId,
      Character* casterPtr,
      Vector3* targetPos,
      ActionEffectHandler.Header* header,
      ActionEffectHandler.TargetEffects* effects,
      GameObjectId* targetEntityIds)
    {
        if (receiveActionEffectHook == null)
            return;

        receiveActionEffectHook.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);
    }

    //private unsafe int[] GetAmounts(byte count, ActionEffectHandler.TargetEffects* effectArray)
    //{
    //    try
    //    {
    //        int[] amounts = new int[count];

    //        int num1 = count;

    //        int capacity = 0;

    //        if (num1 == 0)
    //            capacity = 0;
    //        else if (num1 == 1)
    //            capacity = 8;
    //        else if (num1 <= 8)
    //            capacity = 64;
    //        else if (num1 <= 16)
    //            capacity = 128;
    //        else if (num1 <= 24)
    //            capacity = 192;
    //        else if (num1 <= 32)
    //            capacity = 256;

    //        List<ActionEffectHandler.Effect> effectEntryList = new List<ActionEffectHandler.Effect>(capacity);

    //        for (int index = 0; index < capacity; ++index)
    //            effectEntryList.Add(effectArray->Effects[index]);

    //        int index1 = 0;

    //        for (int index2 = 0; index2 < effectEntryList.Count; ++index2)
    //        {
    //            if (index2 % 8 == 0)
    //            {
    //                uint num2 = effectEntryList[index2].Value;

    //                if (effectEntryList[index2].Param3 != 0)
    //                    num2 += 65536U * effectEntryList[index2].Param3;

    //                if (index1 < count)
    //                    amounts[index1] = (int)num2;

    //                ++index1;
    //            }
    //        }

    //        return amounts;
    //    }
    //    catch (Exception ex)
    //    {
    //        NoireLogger.LogError(ex, $"HookActionEffect.GetAmounts");
    //        return [];
    //    }
    //}

    private unsafe int[] GetAmounts(byte count, ActionEffectHandler.TargetEffects* effectArray)
    {
        try
        {
            var amounts = new int[count];

            if (count == 0)
                return amounts;

            var capacity = count switch
            {
                0 => 0,
                < 8 => 8,
                < 16 => 64,
                < 24 => 128,
                < 32 => 192,
                32 => 256,
                _ => 0
            };

            if (capacity == 0)
                return amounts;

            var amountIndex = 0;

            for (var i = 0; i < capacity && amountIndex < count; i += 8)
            {
                var effect = effectArray->Effects[i];
                var value = effect.Value;

                if (effect.Param3 != 0)
                    value += (ushort)(65536U * effect.Param3);

                amounts[amountIndex] = (int)value;
                amountIndex++;
            }

            return amounts;
        }
        catch (Exception ex)
        {
            NoireLogger.LogError(ex, "HookActionEffect.GetAmounts");
            return [];
        }
    }

    private static int ComputeAverageAmount(int[] amounts)
    {
        int num = 0;

        for (int index = 0; index < amounts.Length; ++index)
            num += amounts[index];

        return num != 0 ? num / amounts.Length : num;
    }

    private unsafe List<Structures.Player> GetAllTarget(byte count, GameObjectId* targetEntityIds, int[] amounts)
    {
        List<Structures.Player> allTarget = new List<Structures.Player>();

        if (count >= 1)
        {
            ulong[] numArray = new ulong[count];

            for (int index = 0; index < count; ++index)
            {
                numArray[index] = targetEntityIds[index];

                uint sourceId = (uint)numArray[index];
                string nameFromSourceId = GetCharacterNameFromSourceId(sourceId);

                Structures.Player player = new Structures.Player();
                ref Structures.Player local = ref player;

                uint id = sourceId;
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

    private string GetCharacterNameFromSourceId(uint sourceId)
    {
        IGameObject gameObject = NoireService.ObjectTable!.SearchById(sourceId);

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
