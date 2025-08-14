using Dalamud.Game.Network;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using System;

namespace FFXIV_Vibe_Plugin.Experimental;

internal class NetworkCapture
{
    private bool ExperimentalNetworkCaptureStarted;

    public void Dispose() => StopNetworkCapture();

    public void StartNetworkCapture()
    {
    }

    public void StopNetworkCapture()
    {
        if (!ExperimentalNetworkCaptureStarted)
            return;

        Logger.Debug("STOPPING EXPERIMENTAL");

        //Service.GameNetwork.NetworkMessage -= new IGameNetwork.OnNetworkMessageDelegate(OnNetworkReceived);

        ExperimentalNetworkCaptureStarted = false;
    }

    private unsafe void OnNetworkReceived(
      IntPtr dataPtr,
      ushort opCode,
      uint sourceActorId,
      uint targetActorId,
      NetworkMessageDirection direction)
    {
        int int32 = Convert.ToInt32(opCode);
        string name = OpCodes.GetName(opCode);
        uint num1 = 111111111;

        if (direction == NetworkMessageDirection.ZoneUp)
            num1 = *(uint*)(dataPtr + new IntPtr(4));

        Logger.Log($"Hex: {int32:X} Decimal: {opCode} ActionId: {num1} SOURCE_ID: {sourceActorId} TARGET_ID: {targetActorId} DIRECTION: {direction} DATA_PTR: {dataPtr} NAME: {name}");

        if (!(name == "ClientZoneIpcType-ClientTrigger"))
            return;

        ushort num2 = *(ushort*)dataPtr;
        byte num3 = *(byte*)(dataPtr + new IntPtr(2));
        byte num4 = *(byte*)(dataPtr + new IntPtr(3));
        uint num5 = *(uint*)(dataPtr + new IntPtr(4));
        uint num6 = *(uint*)(dataPtr + new IntPtr(8));
        uint num7 = *(uint*)(dataPtr + new IntPtr(12));
        uint num8 = *(uint*)(dataPtr + new IntPtr(16));
        uint num9 = *(uint*)(dataPtr + new IntPtr(20));
        ulong num10 = (ulong)*(long*)(dataPtr + new IntPtr(24));
        string str = "";

        switch (num5)
        {
            case 0:
                str += "WeaponIn";
                break;
            case 1:
                str += "WeaponOut";
                break;
        }

        Logger.Log($"{name} {direction} {str} {num2} {num3} {num4} {num5} {num6} {num7} {num7} {num8} {num9} {num10}");
    }
}
