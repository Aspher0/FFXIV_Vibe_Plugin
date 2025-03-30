using Buttplug.Client;
using Buttplug.Core.Messages;
using DebounceThrottle;
using FFXIV_Vibe_Plugin.Commons;
using System;
using System.Collections.Generic;

namespace FFXIV_Vibe_Plugin.Device;

public class Device
{
    private readonly ButtplugClientDevice? ButtplugClientDevice;
    public int Id = -1;
    public string Name = "UnsetDevice";
    public bool CanVibrate;
    public int VibrateMotors = -1;
    private List<GenericDeviceMessageAttributes> vibrateAttributes = new List<GenericDeviceMessageAttributes>();
    public bool CanRotate;
    public int RotateMotors = -1;
    private List<GenericDeviceMessageAttributes> rotateAttributes = new List<GenericDeviceMessageAttributes>();
    public bool CanLinear;
    public int LinearMotors = -1;
    private List<GenericDeviceMessageAttributes> linearAttribute = new List<GenericDeviceMessageAttributes>();
    public bool CanOscillate;
    public int OscillateMotors = -1;
    private List<GenericDeviceMessageAttributes> oscillateAttribute = new List<GenericDeviceMessageAttributes>();
    public bool CanBattery;
    public double BatteryLevel = -1.0;
    public bool CanStop = true;
    public bool IsConnected;
    public List<UsableCommand> UsableCommands = new List<UsableCommand>();
    public int[] CurrentVibrateIntensity = Array.Empty<int>();
    public int[] CurrentRotateIntensity = Array.Empty<int>();
    public int[] CurrentOscillateIntensity = Array.Empty<int>();
    public int[] CurrentLinearIntensity = Array.Empty<int>();
    public DebounceDispatcher VibrateDebouncer = new DebounceDispatcher(new(25));
    public DebounceDispatcher RotateDebouncer = new DebounceDispatcher(new(25));
    public DebounceDispatcher OscillateDebouncer = new DebounceDispatcher(new(25));
    public DebounceDispatcher LinearDebouncer = new DebounceDispatcher(new(25));

    public Device(ButtplugClientDevice buttplugClientDevice)
    {
        if (buttplugClientDevice == null)
            return;

        ButtplugClientDevice = buttplugClientDevice;
        Id = (int)buttplugClientDevice.Index;
        Name = buttplugClientDevice.Name;

        SetCommands();
        ResetMotors();
        UpdateBatteryLevel();
    }

    public override string ToString()
    {
        List<string> commandsInfo = GetCommandsInfo();
        return $"Device: {Id}:{Name} (connected={IsConnected}, battery={GetBatteryPercentage()}, commands={string.Join(",", commandsInfo)})";
    }

    private void SetCommands()
    {
        if (ButtplugClientDevice == null)
        {
            Logger.Error($"Device {Id}:{Name} has ClientDevice to null!");
        }
        else
        {
            vibrateAttributes = ButtplugClientDevice.VibrateAttributes;

            if (vibrateAttributes.Count > 0)
            {
                CanVibrate = true;
                VibrateMotors = vibrateAttributes.Count;
                UsableCommands.Add(UsableCommand.Vibrate);
            }

            rotateAttributes = ButtplugClientDevice.RotateAttributes;

            if (rotateAttributes.Count > 0)
            {
                CanRotate = true;
                RotateMotors = rotateAttributes.Count;
                UsableCommands.Add(UsableCommand.Rotate);
            }

            linearAttribute = ButtplugClientDevice.LinearAttributes;

            if (linearAttribute.Count > 0)
            {
                CanLinear = true;
                LinearMotors = linearAttribute.Count;
                UsableCommands.Add(UsableCommand.Linear);
            }

            oscillateAttribute = ButtplugClientDevice.OscillateAttributes;

            if (oscillateAttribute.Count > 0)
            {
                CanOscillate = true;
                OscillateMotors = oscillateAttribute.Count;
                UsableCommands.Add(UsableCommand.Oscillate);
            }

            if (!ButtplugClientDevice.HasBattery)
                return;

            CanBattery = true;
            UpdateBatteryLevel();
        }
    }

    private void ResetMotors()
    {
        if (CanVibrate)
        {
            CurrentVibrateIntensity = new int[VibrateMotors];

            for (int index = 0; index < VibrateMotors; ++index)
                CurrentVibrateIntensity[index] = 0;
        }

        if (CanRotate)
        {
            CurrentRotateIntensity = new int[RotateMotors];

            for (int index = 0; index < RotateMotors; ++index)
                CurrentRotateIntensity[index] = 0;
        }

        if (CanOscillate)
        {
            CurrentOscillateIntensity = new int[OscillateMotors];

            for (int index = 0; index < OscillateMotors; ++index)
                CurrentOscillateIntensity[index] = 0;
        }

        if (!CanLinear)
            return;

        CurrentLinearIntensity = new int[LinearMotors];

        for (int index = 0; index < LinearMotors; ++index)
            CurrentLinearIntensity[index] = 0;
    }

    public List<UsableCommand> GetUsableCommands() => UsableCommands;

    public List<string> GetCommandsInfo()
    {
        List<string> commandsInfo = new List<string>();

        if (CanVibrate)
        {
            List<string> stringList = commandsInfo;
            stringList.Add($"vibrate motors={VibrateMotors}");
        }

        if (CanRotate)
        {
            List<string> stringList = commandsInfo;
            stringList.Add($"rotate motors={RotateMotors} ");
        }

        if (CanLinear)
        {
            List<string> stringList = commandsInfo;
            stringList.Add($"rotate motors={LinearMotors}");
        }

        if (CanOscillate)
        {
            List<string> stringList = commandsInfo;
            stringList.Add($"oscillate motors={OscillateMotors}");
        }

        if (CanBattery)
            commandsInfo.Add("battery");

        if (CanStop)
            commandsInfo.Add("stop");

        return commandsInfo;
    }

    public async void UpdateBatteryLevel()
    {
        if (ButtplugClientDevice == null || !CanBattery)
            return;

        try
        {
            BatteryLevel = await ButtplugClientDevice.BatteryAsync();
        }
        catch (Exception ex)
        {
            Logger.Warn("Device.UpdateBatteryLevel: " + ex.Message);
        }
    }

    public string GetBatteryPercentage()
    {
        if (BatteryLevel == -1.0)
            return "Unknown";

        return $"{BatteryLevel * 100.0}%";
    }

    public async void Stop()
    {
        if (ButtplugClientDevice == null)
            return;

        try
        {
            if (CanVibrate)
                await ButtplugClientDevice.VibrateAsync(0.0);

            if (CanRotate)
                await ButtplugClientDevice.RotateAsync(0.0, true);

            if (CanOscillate)
                await ButtplugClientDevice.OscillateAsync(0.0);

            if (CanStop)
                await ButtplugClientDevice.Stop();
        }
        catch (Exception ex)
        {
            Logger.Error("Device.Stop: " + ex.Message);
        }

        ResetMotors();
    }

    public void SendVibrate(int intensity, int motorId = -1, int threshold = 100, int timer = 2000)
    {
        if (ButtplugClientDevice == null || !CanVibrate || !IsConnected)
            return;

        int vibrateMotors = VibrateMotors;

        try
        {
            if (motorId != -1)
            {
                CurrentVibrateIntensity[motorId] = intensity;
            }
            else
            {
                for (int index = 0; index < vibrateMotors; ++index)
                    CurrentVibrateIntensity[index] = intensity;
            }

            double[] motorIntensity = new double[vibrateMotors];

            for (int index = 0; index < vibrateMotors; ++index)
            {
                double num = Helpers.ClampIntensity(CurrentVibrateIntensity[index], threshold) / 100.0;
                motorIntensity[index] = num;
            }

            VibrateDebouncer.Debounce(() => ButtplugClientDevice.VibrateAsync(motorIntensity));
        }
        catch (Exception ex)
        {
            Logger.Error("Device.SendVibrate: " + ex.Message);
        }
    }

    public void SendRotate(int intensity, bool clockWise = true, int motorId = -1, int threshold = 100)
    {
        if (ButtplugClientDevice == null || !CanRotate || !IsConnected)
            return;

        int nbrMotors = RotateMotors;

        try
        {
            if (motorId != -1)
            {
                CurrentRotateIntensity[motorId] = intensity;
            }
            else
            {
                for (int index = 0; index < nbrMotors; ++index)
                    CurrentRotateIntensity[index] = intensity;
            }

            List<RotateCmd.RotateCommand> motorIntensity = new List<RotateCmd.RotateCommand>();

            for (int index = 0; index < nbrMotors; ++index)
            {
                double num = Helpers.ClampIntensity(CurrentRotateIntensity[index], threshold) / 100.0;
                motorIntensity.Add(new(num, clockWise));
            }

            RotateDebouncer.Debounce(() =>
            {
                for (int index = 0; index < nbrMotors; ++index)
                    Logger.Warn(index.ToString() + " MotorIntensity: " + motorIntensity[index].ToString());

                ButtplugClientDevice.RotateAsync(motorIntensity);
            });
        }
        catch (Exception ex)
        {
            Logger.Error("Device.SendRotate: " + ex.Message);
        }
    }

    public void SendOscillate(int intensity, int duration = 500, int motorId = -1, int threshold = 100)
    {
        if (ButtplugClientDevice == null || !CanOscillate || !IsConnected)
            return;

        int nbrMotors = OscillateMotors;

        try
        {
            if (motorId != -1)
            {
                CurrentOscillateIntensity[motorId] = intensity;
            }
            else
            {
                for (int index = 0; index < nbrMotors; ++index)
                    CurrentOscillateIntensity[index] = intensity;
            }

            double[] motorIntensity = new double[nbrMotors];

            for (int index = 0; index < nbrMotors; ++index)
            {
                double num = Helpers.ClampIntensity(CurrentOscillateIntensity[index], threshold) / 100.0;
                motorIntensity[index] = num;
            }

            OscillateDebouncer.Debounce(() =>
            {
                for (int index = 0; index < nbrMotors; ++index)
                    Logger.Warn(index.ToString() + " MotorIntensity: " + motorIntensity[index].ToString());

                ButtplugClientDevice.OscillateAsync(motorIntensity);
            });
        }
        catch (Exception ex)
        {
            Logger.Error("Device.SendOscillate: " + ex.Message);
        }
    }

    public void SendLinear(int intensity, int duration = 500, int motorId = -1, int threshold = 100)
    {
        if (ButtplugClientDevice == null || !CanLinear || !IsConnected)
            return;

        int nbrMotors = RotateMotors;

        try
        {
            if (motorId != -1)
            {
                CurrentLinearIntensity[motorId] = intensity;
            }
            else
            {
                for (int index = 0; index < nbrMotors; ++index)
                    CurrentLinearIntensity[index] = intensity;
            }

            List<LinearCmd.VectorCommand> motorIntensity = new List<LinearCmd.VectorCommand>();

            for (int index = 0; index < nbrMotors; ++index)
            {
                double num = Helpers.ClampIntensity(CurrentLinearIntensity[index], threshold) / 100.0;
                motorIntensity.Add(new(index, (uint)num));
            }

            LinearDebouncer.Debounce(() =>
            {
                for (int index = 0; index < nbrMotors; ++index)
                    Logger.Warn(index.ToString() + " MotorIntensity: " + motorIntensity[index].ToString());

                ButtplugClientDevice.LinearAsync(motorIntensity);
            });
        }
        catch (Exception ex)
        {
            Logger.Error("Device.SendRotate: " + ex.Message);
        }
    }
}
