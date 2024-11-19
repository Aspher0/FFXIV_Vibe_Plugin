using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.Commons;
using Lumina.Excel;
using Lumina.Text;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable enable
namespace FFXIV_Vibe_Plugin.Hooks
{
    internal class ActionEffect
    {
        private readonly IDataManager? DataManager;
        private readonly Logger Logger;
        private readonly SigScanner Scanner;
        private readonly IClientState ClientState;
        private readonly IObjectTable GameObjects;
        private readonly IGameInteropProvider InteropProvider;
        private readonly ExcelSheet<Lumina.Excel.Sheets.Action>? LuminaActionSheet;
        private Hook<ActionEffect.HOOK_ReceiveActionEffectDelegate> receiveActionEffectHook;

        public event EventHandler<HookActionEffects_ReceivedEventArgs>? ReceivedEvent;

        public ActionEffect(
          IDataManager dataManager,
          Logger logger,
          SigScanner scanner,
          IClientState clientState,
          IObjectTable gameObjects,
          IGameInteropProvider interopProvider)
        {
            this.DataManager = dataManager;
            this.Logger = logger;
            this.Scanner = scanner;
            this.ClientState = clientState;
            this.GameObjects = gameObjects;
            this.InteropProvider = interopProvider;
            this.InitHook();
            if (this.DataManager == null)
                return;
            this.LuminaActionSheet = this.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        }

        public void Dispose()
        {
            this.receiveActionEffectHook?.Disable();
            this.receiveActionEffectHook?.Dispose();
        }

        private void InitHook()
        {
            try
            {
                // Found on: https://github.com/lmcintyre/DamageInfoPlugin/blob/main/DamageInfoPlugin/DamageInfoPlugin.cs#L133
                string signature = "40 55 56 57 41 54 41 55 41 56 48 8D AC 24";
                this.receiveActionEffectHook = this.InteropProvider.HookFromAddress<ActionEffect.HOOK_ReceiveActionEffectDelegate>(this.Scanner.ScanText(signature), new ActionEffect.HOOK_ReceiveActionEffectDelegate(this.ReceiveActionEffect), (IGameInteropProvider.HookBackend)0);
                this.receiveActionEffectHook.Enable();
            }
            catch (Exception ex)
            {
                this.Dispose();
                this.Logger.Warn("Encountered an error loading HookActionEffect: " + ex.Message + ". Disabling it...");
                throw;
            }

            this.Logger.Log("HookActionEffect was correctly enabled!");
        }

        private unsafe void ReceiveActionEffect(
          int sourceId,
          IntPtr sourceCharacter,
          IntPtr pos,
          IntPtr effectHeader,
          IntPtr effectArray,
          IntPtr effectTrail)
        {
            Structures.Spell spell = new Structures.Spell();
            try
            {
                uint actionId = *(uint*)((IntPtr)effectHeader.ToPointer() + new IntPtr(2) * 4);
                int num1 = (int)*(ushort*)((IntPtr)effectHeader.ToPointer() + new IntPtr(14) * 2);
                int num2 = (int)*(ushort*)((IntPtr)effectHeader.ToPointer() - new IntPtr(7) * 2);
                byte count = *(byte*)(effectHeader + new IntPtr(33));
                Structures.EffectEntry effectEntry = *(Structures.EffectEntry*)effectArray;
                string nameFromSourceId = this.GetCharacterNameFromSourceId(sourceId);
                string spellName = this.GetSpellName(actionId, true);
                int[] amounts = this.GetAmounts(count, effectArray);
                float averageAmount = (float)ActionEffect.ComputeAverageAmount(amounts);
                List<Structures.Player> allTarget = this.GetAllTarget(count, effectTrail, amounts);
                spell.Id = (int)actionId;
                spell.Name = spellName;
                spell.Player = new Structures.Player(sourceId, nameFromSourceId);
                spell.Amounts = amounts;
                spell.AmountAverage = averageAmount;
                spell.Targets = allTarget;
                spell.DamageType = Structures.DamageType.Unknown;
                spell.ActionEffectType = allTarget.Count != 0 ? effectEntry.type : Structures.ActionEffectType.Any;
                this.DispatchReceivedEvent(spell);
            }
            catch (Exception ex)
            {
                this.Logger.Log(ex.Message + " " + ex.StackTrace);
            }
            this.RestoreOriginalHook(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
        }

        private void RestoreOriginalHook(
          int sourceId,
          IntPtr sourceCharacter,
          IntPtr pos,
          IntPtr effectHeader,
          IntPtr effectArray,
          IntPtr effectTrail)
        {
            if (this.receiveActionEffectHook == null)
                return;
            this.receiveActionEffectHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
        }

        private unsafe int[] GetAmounts(byte count, IntPtr effectArray)
        {
            int[] amounts = new int[(int)count];
            int num1 = (int)count;
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
                effectEntryList.Add(*(Structures.EffectEntry*)(effectArray + (IntPtr)(index * 8)));
            int index1 = 0;
            for (int index2 = 0; index2 < effectEntryList.Count; ++index2)
            {
                if (index2 % 8 == 0)
                {
                    uint num2 = (uint)effectEntryList[index2].value;
                    if (effectEntryList[index2].mult != (byte)0)
                        num2 += 65536U * (uint)effectEntryList[index2].mult;
                    if (index1 < (int)count)
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

        private unsafe List<Structures.Player> GetAllTarget(
          byte count,
          IntPtr effectTrail,
          int[] amounts)
        {
            List<Structures.Player> allTarget = new List<Structures.Player>();
            if (count >= (byte)1)
            {
                ulong[] numArray = new ulong[(int)count];
                for (int index = 0; index < (int)count; ++index)
                {
                    numArray[index] = (ulong)*(long*)(effectTrail + (IntPtr)(index * 8));
                    int sourceId = (int)numArray[index];
                    string nameFromSourceId = this.GetCharacterNameFromSourceId(sourceId);
                    Structures.Player player = new Structures.Player(); // A VERIFIER
                    ref Structures.Player local = ref player;
                    int id = sourceId;
                    string name = nameFromSourceId;
                    DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                    interpolatedStringHandler.AppendFormatted<int>(amounts[index]);
                    string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                    local = new Structures.Player(id, name, stringAndClear);
                    allTarget.Add(player);
                }
            }
            return allTarget;
        }

        private string GetSpellName(uint actionId, bool withId)
        {
            if (this.LuminaActionSheet == null)
            {
                this.Logger.Warn("HookActionEffect.GetSpellName: LuminaActionSheet is null");
                return "***LUMINA ACTION SHEET NOT LOADED***";
            }

            try
            {
                Lumina.Excel.Sheets.Action row = this.LuminaActionSheet.GetRow(actionId);
                string spellName = "";

                DefaultInterpolatedStringHandler interpolatedStringHandler;
                if (withId)
                {
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
                    interpolatedStringHandler.AppendFormatted<uint>(row.RowId);
                    interpolatedStringHandler.AppendLiteral(":");
                    spellName = interpolatedStringHandler.ToStringAndClear();
                }
                if (!row.Name.IsEmpty)
                {
                    string str = spellName;
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                    interpolatedStringHandler.AppendFormatted<ReadOnlySeString>(row.Name);
                    string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                    spellName = str + stringAndClear;
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
            IGameObject gameObject = this.GameObjects.SearchById((ulong)(uint)sourceId);
            string nameFromSourceId = "";
            //if (GameObject.op_Inequality(gameObject, (GameObject)null))
            if (gameObject != null) // A VERIFIER
                nameFromSourceId = gameObject.Name.TextValue;
            return nameFromSourceId;
        }

        protected virtual void DispatchReceivedEvent(Structures.Spell spell)
        {
            HookActionEffects_ReceivedEventArgs e = new HookActionEffects_ReceivedEventArgs();
            e.Spell = spell;
            EventHandler<HookActionEffects_ReceivedEventArgs> receivedEvent = this.ReceivedEvent;
            if (receivedEvent == null)
                return;
            receivedEvent((object)this, e);
        }

        private delegate void HOOK_ReceiveActionEffectDelegate(
          int sourceId,
          IntPtr sourceCharacter,
          IntPtr pos,
          IntPtr effectHeader,
          IntPtr effectArray,
          IntPtr effectTrail);
    }
}
