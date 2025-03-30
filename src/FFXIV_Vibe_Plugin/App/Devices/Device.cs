using Buttplug.Client;
using Buttplug.Core.Messages;
using DebounceThrottle;
using FFXIV_Vibe_Plugin.Commons;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using vtortola.WebSockets.Http;

#nullable enable
namespace FFXIV_Vibe_Plugin.Device
{
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
            this.ButtplugClientDevice = buttplugClientDevice;
            this.Id = (int)buttplugClientDevice.Index;
            this.Name = buttplugClientDevice.Name;
            this.SetCommands();
            this.ResetMotors();
            this.UpdateBatteryLevel();
        }

        public override string ToString()
        {
            List<string> commandsInfo = this.GetCommandsInfo();
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 5);
            interpolatedStringHandler.AppendLiteral("Device: ");
            interpolatedStringHandler.AppendFormatted<int>(this.Id);
            interpolatedStringHandler.AppendLiteral(":");
            interpolatedStringHandler.AppendFormatted(this.Name);
            interpolatedStringHandler.AppendLiteral(" (connected=");
            interpolatedStringHandler.AppendFormatted<bool>(this.IsConnected);
            interpolatedStringHandler.AppendLiteral(", battery=");
            interpolatedStringHandler.AppendFormatted(this.GetBatteryPercentage());
            interpolatedStringHandler.AppendLiteral(", commands=");
            interpolatedStringHandler.AppendFormatted(string.Join(",", (IEnumerable<string>)commandsInfo));
            interpolatedStringHandler.AppendLiteral(")");
            return interpolatedStringHandler.ToStringAndClear();
        }

        private void SetCommands()
        {
            if (ButtplugClientDevice == null)
            {
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 2);
                interpolatedStringHandler.AppendLiteral("Device ");
                interpolatedStringHandler.AppendFormatted<int>(this.Id);
                interpolatedStringHandler.AppendLiteral(":");
                interpolatedStringHandler.AppendFormatted(this.Name);
                interpolatedStringHandler.AppendLiteral(" has ClientDevice to null!");
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                Logger.Error(stringAndClear);
            }
            else
            {
                this.vibrateAttributes = this.ButtplugClientDevice.VibrateAttributes;
                if (this.vibrateAttributes.Count > 0)
                {
                    this.CanVibrate = true;
                    this.VibrateMotors = this.vibrateAttributes.Count;
                    this.UsableCommands.Add(UsableCommand.Vibrate);
                }
                this.rotateAttributes = this.ButtplugClientDevice.RotateAttributes;
                if (this.rotateAttributes.Count > 0)
                {
                    this.CanRotate = true;
                    this.RotateMotors = this.rotateAttributes.Count;
                    this.UsableCommands.Add(UsableCommand.Rotate);
                }
                this.linearAttribute = this.ButtplugClientDevice.LinearAttributes;
                if (this.linearAttribute.Count > 0)
                {
                    this.CanLinear = true;
                    this.LinearMotors = this.linearAttribute.Count;
                    this.UsableCommands.Add(UsableCommand.Linear);
                }
                this.oscillateAttribute = this.ButtplugClientDevice.OscillateAttributes;
                if (this.oscillateAttribute.Count > 0)
                {
                    this.CanOscillate = true;
                    this.OscillateMotors = this.oscillateAttribute.Count;
                    this.UsableCommands.Add(UsableCommand.Oscillate);
                }
                if (!this.ButtplugClientDevice.HasBattery)
                    return;
                this.CanBattery = true;
                this.UpdateBatteryLevel();
            }
        }

        private void ResetMotors()
        {
            if (this.CanVibrate)
            {
                this.CurrentVibrateIntensity = new int[this.VibrateMotors];
                for (int index = 0; index < this.VibrateMotors; ++index)
                    this.CurrentVibrateIntensity[index] = 0;
            }
            if (this.CanRotate)
            {
                this.CurrentRotateIntensity = new int[this.RotateMotors];
                for (int index = 0; index < this.RotateMotors; ++index)
                    this.CurrentRotateIntensity[index] = 0;
            }
            if (this.CanOscillate)
            {
                this.CurrentOscillateIntensity = new int[this.OscillateMotors];
                for (int index = 0; index < this.OscillateMotors; ++index)
                    this.CurrentOscillateIntensity[index] = 0;
            }
            if (!this.CanLinear)
                return;
            this.CurrentLinearIntensity = new int[this.LinearMotors];
            for (int index = 0; index < this.LinearMotors; ++index)
                this.CurrentLinearIntensity[index] = 0;
        }

        public List<UsableCommand> GetUsableCommands() => this.UsableCommands;

        public List<string> GetCommandsInfo()
        {
            List<string> commandsInfo = new List<string>();
            DefaultInterpolatedStringHandler interpolatedStringHandler;
            if (this.CanVibrate)
            {
                List<string> stringList = commandsInfo;
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 1);
                interpolatedStringHandler.AppendLiteral("vibrate motors=");
                interpolatedStringHandler.AppendFormatted<int>(this.VibrateMotors);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                stringList.Add(stringAndClear);
            }
            if (this.CanRotate)
            {
                List<string> stringList = commandsInfo;
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 1);
                interpolatedStringHandler.AppendLiteral("rotate motors=");
                interpolatedStringHandler.AppendFormatted<int>(this.RotateMotors);
                interpolatedStringHandler.AppendLiteral(" ");
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                stringList.Add(stringAndClear);
            }
            if (this.CanLinear)
            {
                List<string> stringList = commandsInfo;
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 1);
                interpolatedStringHandler.AppendLiteral("rotate motors=");
                interpolatedStringHandler.AppendFormatted<int>(this.LinearMotors);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                stringList.Add(stringAndClear);
            }
            if (this.CanOscillate)
            {
                List<string> stringList = commandsInfo;
                interpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 1);
                interpolatedStringHandler.AppendLiteral("oscillate motors=");
                interpolatedStringHandler.AppendFormatted<int>(this.OscillateMotors);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                stringList.Add(stringAndClear);
            }
            if (this.CanBattery)
                commandsInfo.Add("battery");
            if (this.CanStop)
                commandsInfo.Add("stop");
            return commandsInfo;
        }

        public async void UpdateBatteryLevel()
        {
            if (this.ButtplugClientDevice == null)
                return;
            if (!this.CanBattery)
                return;
            try
            {
                this.BatteryLevel = await this.ButtplugClientDevice.BatteryAsync();
            }
            catch (Exception ex)
            {
                Logger.Warn("Device.UpdateBatteryLevel: " + ex.Message);
            }
        }

        public string GetBatteryPercentage()
        {
            if (this.BatteryLevel == -1.0)
                return "Unknown";
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
            interpolatedStringHandler.AppendFormatted<double>(this.BatteryLevel * 100.0);
            interpolatedStringHandler.AppendLiteral("%");
            return interpolatedStringHandler.ToStringAndClear();
        }

        public async void Stop()
        {
            if (this.ButtplugClientDevice == null)
                return;
            try
            {
                if (this.CanVibrate)
                    await this.ButtplugClientDevice.VibrateAsync(0.0);
                if (this.CanRotate)
                    await this.ButtplugClientDevice.RotateAsync(0.0, true);
                if (this.CanOscillate)
                    await this.ButtplugClientDevice.OscillateAsync(0.0);
                if (this.CanStop)
                    await this.ButtplugClientDevice.Stop();
            }
            catch (Exception ex)
            {
                Logger.Error("Device.Stop: " + ex.Message);
            }
            this.ResetMotors();
        }

        public async void SendVibrate(int intensity, int motorId = -1, int threshold = 100, int timer = 2000)
        {
            if (this.ButtplugClientDevice == null || !this.CanVibrate || !this.IsConnected)
                return;
            int vibrateMotors = this.VibrateMotors;
            try
            {
                if (motorId != -1)
                {
                    this.CurrentVibrateIntensity[motorId] = intensity;
                }
                else
                {
                    for (int index = 0; index < vibrateMotors; ++index)
                        this.CurrentVibrateIntensity[index] = intensity;
                }
                double[] motorIntensity = new double[vibrateMotors];
                for (int index = 0; index < vibrateMotors; ++index)
                {
                    double num = (double)Helpers.ClampIntensity(this.CurrentVibrateIntensity[index], threshold) / 100.0;
                    motorIntensity[index] = num;
                }
                this.VibrateDebouncer.Debounce((Action)(() => this.ButtplugClientDevice.VibrateAsync((IEnumerable<double>)motorIntensity)));
            }
            catch (Exception ex)
            {
                Logger.Error("Device.SendVibrate: " + ex.Message);
            }
        }

        public void SendRotate(int intensity, bool clockWise = true, int motorId = -1, int threshold = 100)
        {
            if (this.ButtplugClientDevice == null || !this.CanRotate || !this.IsConnected)
                return;
            int nbrMotors = this.RotateMotors;
            try
            {
                if (motorId != -1)
                {
                    this.CurrentRotateIntensity[motorId] = intensity;
                }
                else
                {
                    for (int index = 0; index < nbrMotors; ++index)
                        this.CurrentRotateIntensity[index] = intensity;
                }
                List<RotateCmd.RotateCommand> motorIntensity = new List<RotateCmd.RotateCommand>();
                for (int index = 0; index < nbrMotors; ++index)
                {
                    double num = (double)Helpers.ClampIntensity(this.CurrentRotateIntensity[index], threshold) / 100.0;
                    motorIntensity.Add(new(num, clockWise));
                }
                this.RotateDebouncer.Debounce((Action)(() =>
                {
                    for (int index = 0; index < nbrMotors; ++index)
                        Logger.Warn(index.ToString() + " MotorIntensity: " + motorIntensity[index].ToString());
                    this.ButtplugClientDevice.RotateAsync(motorIntensity);
                }));
            }
            catch (Exception ex)
            {
                Logger.Error("Device.SendRotate: " + ex.Message);
            }
        }

        public void SendOscillate(int intensity, int duration = 500, int motorId = -1, int threshold = 100)
        {
            if (this.ButtplugClientDevice == null || !this.CanOscillate || !this.IsConnected)
                return;
            int nbrMotors = this.OscillateMotors;
            try
            {
                if (motorId != -1)
                {
                    this.CurrentOscillateIntensity[motorId] = intensity;
                }
                else
                {
                    for (int index = 0; index < nbrMotors; ++index)
                        this.CurrentOscillateIntensity[index] = intensity;
                }
                double[] motorIntensity = new double[nbrMotors];
                for (int index = 0; index < nbrMotors; ++index)
                {
                    double num = (double)Helpers.ClampIntensity(this.CurrentOscillateIntensity[index], threshold) / 100.0;
                    motorIntensity[index] = num;
                }
                this.OscillateDebouncer.Debounce((Action)(() =>
                {
                    for (int index = 0; index < nbrMotors; ++index)
                        Logger.Warn(index.ToString() + " MotorIntensity: " + motorIntensity[index].ToString());
                    this.ButtplugClientDevice.OscillateAsync(motorIntensity);
                }));
            }
            catch (Exception ex)
            {
                Logger.Error("Device.SendOscillate: " + ex.Message);
            }
        }

        public void SendLinear(int intensity, int duration = 500, int motorId = -1, int threshold = 100)
        {
            if (this.ButtplugClientDevice == null || !this.CanLinear || !this.IsConnected)
                return;
            int nbrMotors = this.RotateMotors;
            try
            {
                if (motorId != -1)
                {
                    this.CurrentLinearIntensity[motorId] = intensity;
                }
                else
                {
                    for (int index = 0; index < nbrMotors; ++index)
                        this.CurrentLinearIntensity[index] = intensity;
                }
                List<LinearCmd.VectorCommand> motorIntensity = new List<LinearCmd.VectorCommand>();
                for (int index = 0; index < nbrMotors; ++index)
                {
                    double num = Helpers.ClampIntensity(this.CurrentLinearIntensity[index], threshold) / 100.0;
                    motorIntensity.Add(new(index, (uint)num));
                }
                this.LinearDebouncer.Debounce((Action)(() =>
                {
                    for (int index = 0; index < nbrMotors; ++index)
                        Logger.Warn(index.ToString() + " MotorIntensity: " + motorIntensity[index].ToString());
                    this.ButtplugClientDevice.LinearAsync(motorIntensity);
                }));
            }
            catch (Exception ex)
            {
                Logger.Error("Device.SendRotate: " + ex.Message);
            }
        }
    }
}
