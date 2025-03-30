using Dalamud.Game.Network;
using Dalamud.Plugin.Services;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using System;
using System.Runtime.CompilerServices;

#nullable enable
namespace FFXIV_Vibe_Plugin.Experimental
{
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

            Service.GameNetwork.NetworkMessage -= new IGameNetwork.OnNetworkMessageDelegate(OnNetworkReceived);

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
            if (direction == NetworkMessageDirection.ZoneUp) // A VERIFIER
                num1 = *(uint*)(dataPtr + new IntPtr(4));
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(80, 8);
            interpolatedStringHandler.AppendLiteral("Hex: ");
            interpolatedStringHandler.AppendFormatted<int>(int32, "X");
            interpolatedStringHandler.AppendLiteral(" Decimal: ");
            interpolatedStringHandler.AppendFormatted<ushort>(opCode);
            interpolatedStringHandler.AppendLiteral(" ActionId: ");
            interpolatedStringHandler.AppendFormatted<uint>(num1);
            interpolatedStringHandler.AppendLiteral(" SOURCE_ID: ");
            interpolatedStringHandler.AppendFormatted<uint>(sourceActorId);
            interpolatedStringHandler.AppendLiteral(" TARGET_ID: ");
            interpolatedStringHandler.AppendFormatted<uint>(targetActorId);
            interpolatedStringHandler.AppendLiteral(" DIRECTION: ");
            interpolatedStringHandler.AppendFormatted<NetworkMessageDirection>(direction);
            interpolatedStringHandler.AppendLiteral(" DATA_PTR: ");
            interpolatedStringHandler.AppendFormatted<IntPtr>(dataPtr);
            interpolatedStringHandler.AppendLiteral(" NAME: ");
            interpolatedStringHandler.AppendFormatted(name);
            string stringAndClear1 = interpolatedStringHandler.ToStringAndClear();
            Logger.Log(stringAndClear1);
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
            interpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 13);
            interpolatedStringHandler.AppendFormatted(name);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<NetworkMessageDirection>(direction);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted(str);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<ushort>(num2);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<byte>(num3);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<byte>(num4);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<uint>(num5);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<uint>(num6);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<uint>(num7);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<uint>(num7);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<uint>(num8);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<uint>(num9);
            interpolatedStringHandler.AppendLiteral(" ");
            interpolatedStringHandler.AppendFormatted<ulong>(num10);
            string stringAndClear2 = interpolatedStringHandler.ToStringAndClear();
            Logger.Log(stringAndClear2);
        }
    }
}
