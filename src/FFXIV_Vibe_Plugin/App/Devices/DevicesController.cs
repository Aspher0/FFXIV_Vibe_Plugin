using Buttplug.Client;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Triggers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

#nullable enable
namespace FFXIV_Vibe_Plugin.Device
{
    public class DevicesController
    {
        private ConfigurationProfile Profile;
        private readonly Patterns Patterns;
        private Trigger? CurrentPlayingTrigger;
        public bool isConnected;
        public bool shouldExit;
        private readonly Dictionary<string, int> CurrentDeviceAndMotorPlaying = new Dictionary<string, int>();
        private ButtplugClient? BPClient;
        private readonly List<FFXIV_Vibe_Plugin.Device.Device> Devices = new List<FFXIV_Vibe_Plugin.Device.Device>();
        private bool isScanning;
        private static readonly Mutex mut = new Mutex();

        public DevicesController(
          ConfigurationProfile profile,
          Patterns patterns)
        {
            Profile = profile;
            Patterns = patterns;
        }

        public void Dispose()
        {
            shouldExit = true;
            Disconnect();
        }

        public void SetProfile(ConfigurationProfile profile) => this.Profile = profile;

        public async void Connect(string host, int port)
        {
            DevicesController devicesController = this;
            Thread.Sleep(2000);
            Logger.Log("Connecting to Intiface...");
            devicesController.isConnected = false;
            devicesController.shouldExit = false;
            devicesController.BPClient = new ButtplugClient("FFXIV_Vibe_Plugin");
            string str1 = host;
            if (port > 0)
                str1 = str1 + ":" + port.ToString();
            ButtplugWebsocketConnector connector = null;
            try
            {
                string str2 = "ws";
                if (devicesController.Profile.BUTTPLUG_SERVER_SHOULD_WSS)
                    str2 = "wss";
                connector = new ButtplugWebsocketConnector(new Uri(str2 + "://" + str1));
            }
            catch (Exception ex)
            {
                Logger.Error("DeviceController.Connect: ButtplugWebsocketConnector error: " + ex.Message);
            }
            devicesController.BPClient.DeviceAdded += new EventHandler<DeviceAddedEventArgs>(devicesController.BPClient_DeviceAdded);
            devicesController.BPClient.DeviceRemoved += new EventHandler<DeviceRemovedEventArgs>(devicesController.BPClient_DeviceRemoved);
            try
            {
                await devicesController.BPClient.ConnectAsync((IButtplugClientConnector)connector);
            }
            catch (Exception ex)
            {
                Logger.Warn("Can't connect, exiting!");
                Logger.Warn("Message: " + ex.InnerException?.Message);
                return;
            }
            devicesController.isConnected = true;
            Logger.Log("Connected!");
            try
            {
                Logger.Log("Fast scanning!");
                devicesController.ScanDevice();
                Thread.Sleep(1000);
                devicesController.StopScanningDevice();
                devicesController.BPClient.StopScanningAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("DeviceController fast scanning: " + ex.Message);
            }
            Logger.Log("Scanning done!");
            devicesController.StartBatteryUpdaterThread();
        }

        private void BPClient_ServerDisconnected(object? sender, EventArgs e)
        {
            Logger.Debug("Server disconnected");
            Disconnect();
        }

        public bool IsConnected()
        {
            refreshIsConnected();
            return isConnected;
        }

        public void refreshIsConnected()
        {
            if (BPClient == null)
                return;

            isConnected = BPClient.Connected;
        }

        public async void ScanDevice()
        {
            if (BPClient == null)
                return;

            Logger.Debug("Scanning for devices...");

            if (!IsConnected())
                return;

            try
            {
                isScanning = true;
                await BPClient.StartScanningAsync();
            }
            catch (Exception ex)
            {
                isScanning = false;
                Logger.Error("Scanning issue. No 'Device Comm Managers' enabled on Intiface?");
                Logger.Error(ex.Message);
            }
        }

        public bool IsScanning() => isScanning;

        public async void StopScanningDevice()
        {
            if (BPClient != null)
            {
                if (IsConnected())
                {
                    try
                    {
                        Logger.Debug("Sending stop scanning command!");
                        BPClient.StopScanningAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug("StopScanningDevice ignored: already stopped");
                    }
                }
            }

            isScanning = false;
        }

        private void BPClient_OnScanComplete(object? sender, EventArgs e)
        {
            Logger.Debug("Stop scanning...");
            isScanning = false;
        }

        private void BPClient_DeviceAdded(object? sender, DeviceAddedEventArgs arg)
        {
            try
            {
                mut.WaitOne();

                Device device = new Device(arg.Device);

                device.IsConnected = true;
                this.Devices.Add(device);
                DefaultInterpolatedStringHandler interpolatedStringHandler;
                if (!this.Profile.VISITED_DEVICES.ContainsKey(device.Name))
                {
                    this.Profile.VISITED_DEVICES[device.Name] = device;
                    Service.Configuration!.Save();
                    Logger.Debug($"Adding device to visited list {device})");
                }

                Logger.Debug($"Added {device})");
            }
            catch (Exception ex)
            {
                Logger.Error("DeviceController.BPClient_DeviceAdded: " + ex.Message);
            }
            finally
            {
                DevicesController.mut.ReleaseMutex();
            }
        }

        private void BPClient_DeviceRemoved(object? sender, DeviceRemovedEventArgs arg)
        {
            try
            {
                DevicesController.mut.WaitOne();
                int index = this.Devices.FindIndex((Predicate<FFXIV_Vibe_Plugin.Device.Device>)(device => (long)device.Id == (long)arg.Device.Index));
                if (index <= -1)
                    return;
                
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 1);
                interpolatedStringHandler.AppendLiteral("Removed ");
                interpolatedStringHandler.AppendFormatted<FFXIV_Vibe_Plugin.Device.Device>(this.Devices[index]);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                Logger.Debug(stringAndClear);
                FFXIV_Vibe_Plugin.Device.Device device1 = this.Devices[index];
                this.Devices.RemoveAt(index);
                device1.IsConnected = false;
            }
            catch (Exception ex)
            {
                Logger.Error("DeviceController.BPClient_DeviceRemoved: " + ex.Message);
            }
            finally
            {
                DevicesController.mut.ReleaseMutex();
            }
        }

        public async void Disconnect()
        {
            Logger.Debug("Disconnecting DeviceController");
            try
            {
                this.Devices.Clear();
            }
            catch (Exception ex)
            {
                Logger.Error("DeviceController.Disconnect: " + ex.Message);
            }
            if (this.BPClient == null)
                return;
            if (!this.IsConnected())
                return;
            try
            {
                Thread.Sleep(100);
                if (this.BPClient != null)
                {
                    await this.BPClient.DisconnectAsync();
                    Logger.Log("Disconnecting! Bye... Waiting 2sec...");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error while disconnecting client", ex);
            }
            try
            {
                Logger.Debug("Disposing BPClient.");
                this.BPClient.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while disposing BPClient", ex);
            }
            this.BPClient = (ButtplugClient)null;
            this.isConnected = false;
        }

        public List<FFXIV_Vibe_Plugin.Device.Device> GetDevices() => this.Devices;

        public Dictionary<string, FFXIV_Vibe_Plugin.Device.Device> GetVisitedDevices()
        {
            return this.Profile.VISITED_DEVICES;
        }

        private void StartBatteryUpdaterThread()
        {
            new Thread((ThreadStart)(() =>
            {
                while (!this.shouldExit)
                {
                    Thread.Sleep(5000);
                    if (this.IsConnected())
                    {
                        Logger.Verbose("Updating battery levels!");
                        this.UpdateAllBatteryLevel();
                    }
                }
            }))
            {
                Name = "batteryUpdaterThread"
            }.Start();
        }

        public void UpdateAllBatteryLevel()
        {
            try
            {
                foreach (FFXIV_Vibe_Plugin.Device.Device device in this.GetDevices())
                    device.UpdateBatteryLevel();
            }
            catch (Exception ex)
            {
                Logger.Error("DeviceController.UpdateAllBatteryLevel: " + ex.Message);
            }
        }

        public void StopAll()
        {
            foreach (FFXIV_Vibe_Plugin.Device.Device device in this.GetDevices())
            {
                try
                {
                    device.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Error("DeviceContoller.StopAll: " + ex.Message);
                }
            }
        }

        public void SendTrigger(Trigger trigger, int threshold = 100)
        {
            if (!this.IsConnected())
            {
                
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 1);
                interpolatedStringHandler.AppendLiteral("Not connected, cannot send $");
                interpolatedStringHandler.AppendFormatted<Trigger>(trigger);
                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                Logger.Debug(stringAndClear);
            }
            else
            {
                
                DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 2);
                interpolatedStringHandler.AppendLiteral("Sending trigger ");
                interpolatedStringHandler.AppendFormatted<Trigger>(trigger);
                interpolatedStringHandler.AppendLiteral(" (priority=");
                interpolatedStringHandler.AppendFormatted<int>(trigger.Priority);
                interpolatedStringHandler.AppendLiteral(")");
                string stringAndClear1 = interpolatedStringHandler.ToStringAndClear();
                Logger.Debug(stringAndClear1);
                if (this.CurrentPlayingTrigger == null)
                    this.CurrentPlayingTrigger = trigger;
                if (trigger.Priority < this.CurrentPlayingTrigger.Priority)
                {
                    
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 2);
                    interpolatedStringHandler.AppendLiteral("Ignoring trigger because lower priority => ");
                    interpolatedStringHandler.AppendFormatted<Trigger>(trigger);
                    interpolatedStringHandler.AppendLiteral(" < ");
                    interpolatedStringHandler.AppendFormatted<Trigger>(this.CurrentPlayingTrigger);
                    string stringAndClear2 = interpolatedStringHandler.ToStringAndClear();
                    Logger.Debug(stringAndClear2);
                }
                else
                {
                    this.CurrentPlayingTrigger = trigger;
                    foreach (TriggerDevice device1 in trigger.Devices)
                    {
                        FFXIV_Vibe_Plugin.Device.Device device2 = this.FindDevice(device1.Name);
                        if (device2 != null && device1 != null)
                        {
                            int? length;
                            if (device1.ShouldVibrate)
                            {
                                int motorId = 0;
                                while (true)
                                {
                                    int num1 = motorId;
                                    length = device1.VibrateSelectedMotors?.Length;
                                    int valueOrDefault = length.GetValueOrDefault();
                                    if (num1 < valueOrDefault & length.HasValue)
                                    {
                                        if (device1.VibrateSelectedMotors != null && device1.VibrateMotorsThreshold != null)
                                        {
                                            int num2 = device1.VibrateSelectedMotors[motorId] ? 1 : 0;
                                            int threshold1 = device1.VibrateMotorsThreshold[motorId] * threshold / 100;
                                            int patternId = device1.VibrateMotorsPattern[motorId];
                                            float startAfter = trigger.StartAfter;
                                            float stopAfter = trigger.StopAfter;
                                            if (num2 != 0)
                                            {
                                                
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(58, 4);
                                                interpolatedStringHandler.AppendLiteral("Sending ");
                                                interpolatedStringHandler.AppendFormatted(device2.Name);
                                                interpolatedStringHandler.AppendLiteral(" vibration to motor: ");
                                                interpolatedStringHandler.AppendFormatted<int>(motorId);
                                                interpolatedStringHandler.AppendLiteral(" patternId=");
                                                interpolatedStringHandler.AppendFormatted<int>(patternId);
                                                interpolatedStringHandler.AppendLiteral(" with threshold: ");
                                                interpolatedStringHandler.AppendFormatted<int>(threshold1);
                                                interpolatedStringHandler.AppendLiteral("!");
                                                string stringAndClear3 = interpolatedStringHandler.ToStringAndClear();
                                                Logger.Debug(stringAndClear3);
                                                this.Send("vibrate", device2, threshold1, motorId, patternId, startAfter, stopAfter);
                                            }
                                        }
                                        ++motorId;
                                    }
                                    else
                                        break;
                                }
                            }
                            if (device1.ShouldRotate)
                            {
                                int motorId = 0;
                                while (true)
                                {
                                    int num3 = motorId;
                                    length = device1.RotateSelectedMotors?.Length;
                                    int valueOrDefault = length.GetValueOrDefault();
                                    if (num3 < valueOrDefault & length.HasValue)
                                    {
                                        if (device1.RotateSelectedMotors != null && device1.RotateMotorsThreshold != null)
                                        {
                                            int num4 = device1.RotateSelectedMotors[motorId] ? 1 : 0;
                                            int threshold2 = device1.RotateMotorsThreshold[motorId] * threshold / 100;
                                            int patternId = device1.RotateMotorsPattern[motorId];
                                            float startAfter = trigger.StartAfter;
                                            float stopAfter = trigger.StopAfter;
                                            if (num4 != 0)
                                            {
                                                
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 4);
                                                interpolatedStringHandler.AppendLiteral("Sending ");
                                                interpolatedStringHandler.AppendFormatted(device2.Name);
                                                interpolatedStringHandler.AppendLiteral(" rotation to motor: ");
                                                interpolatedStringHandler.AppendFormatted<int>(motorId);
                                                interpolatedStringHandler.AppendLiteral(" patternId=");
                                                interpolatedStringHandler.AppendFormatted<int>(patternId);
                                                interpolatedStringHandler.AppendLiteral(" with threshold: ");
                                                interpolatedStringHandler.AppendFormatted<int>(threshold2);
                                                interpolatedStringHandler.AppendLiteral("!");
                                                string stringAndClear4 = interpolatedStringHandler.ToStringAndClear();
                                                Logger.Debug(stringAndClear4);
                                                this.Send("rotate", device2, threshold2, motorId, patternId, startAfter, stopAfter);
                                            }
                                        }
                                        ++motorId;
                                    }
                                    else
                                        break;
                                }
                            }
                            if (device1.ShouldLinear)
                            {
                                int motorId = 0;
                                while (true)
                                {
                                    int num5 = motorId;
                                    length = device1.LinearSelectedMotors?.Length;
                                    int valueOrDefault = length.GetValueOrDefault();
                                    if (num5 < valueOrDefault & length.HasValue)
                                    {
                                        if (device1.LinearSelectedMotors != null && device1.LinearMotorsThreshold != null)
                                        {
                                            int num6 = device1.LinearSelectedMotors[motorId] ? 1 : 0;
                                            int threshold3 = device1.LinearMotorsThreshold[motorId] * threshold / 100;
                                            int patternId = device1.LinearMotorsPattern[motorId];
                                            float startAfter = trigger.StartAfter;
                                            float stopAfter = trigger.StopAfter;
                                            if (num6 != 0)
                                            {
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 4);
                                                interpolatedStringHandler.AppendLiteral("Sending ");
                                                interpolatedStringHandler.AppendFormatted(device2.Name);
                                                interpolatedStringHandler.AppendLiteral(" linear to motor: ");
                                                interpolatedStringHandler.AppendFormatted<int>(motorId);
                                                interpolatedStringHandler.AppendLiteral(" patternId=");
                                                interpolatedStringHandler.AppendFormatted<int>(patternId);
                                                interpolatedStringHandler.AppendLiteral(" with threshold: ");
                                                interpolatedStringHandler.AppendFormatted<int>(threshold3);
                                                interpolatedStringHandler.AppendLiteral("!");
                                                string stringAndClear5 = interpolatedStringHandler.ToStringAndClear();
                                                Logger.Debug(stringAndClear5);
                                                this.Send("linear", device2, threshold3, motorId, patternId, startAfter, stopAfter);
                                            }
                                        }
                                        ++motorId;
                                    }
                                    else
                                        break;
                                }
                            }
                            if (device1.ShouldOscillate)
                            {
                                int motorId = 0;
                                while (true)
                                {
                                    int num7 = motorId;
                                    length = device1.OscillateSelectedMotors?.Length;
                                    int valueOrDefault = length.GetValueOrDefault();
                                    if (num7 < valueOrDefault & length.HasValue)
                                    {
                                        if (device1.OscillateSelectedMotors != null && device1.OscillateMotorsThreshold != null)
                                        {
                                            int num8 = device1.OscillateSelectedMotors[motorId] ? 1 : 0;
                                            int threshold4 = device1.OscillateMotorsThreshold[motorId] * threshold / 100;
                                            int patternId = device1.OscillateMotorsPattern[motorId];
                                            float startAfter = trigger.StartAfter;
                                            float stopAfter = trigger.StopAfter;
                                            if (num8 != 0)
                                            {
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(58, 4);
                                                interpolatedStringHandler.AppendLiteral("Sending ");
                                                interpolatedStringHandler.AppendFormatted(device2.Name);
                                                interpolatedStringHandler.AppendLiteral(" oscillate to motor: ");
                                                interpolatedStringHandler.AppendFormatted<int>(motorId);
                                                interpolatedStringHandler.AppendLiteral(" patternId=");
                                                interpolatedStringHandler.AppendFormatted<int>(patternId);
                                                interpolatedStringHandler.AppendLiteral(" with threshold: ");
                                                interpolatedStringHandler.AppendFormatted<int>(threshold4);
                                                interpolatedStringHandler.AppendLiteral("!");
                                                string stringAndClear6 = interpolatedStringHandler.ToStringAndClear();
                                                Logger.Debug(stringAndClear6);
                                                this.Send("oscillate", device2, threshold4, motorId, patternId, startAfter, stopAfter);
                                            }
                                        }
                                        ++motorId;
                                    }
                                    else
                                        break;
                                }
                            }
                            if (device1.ShouldStop)
                            {
                                Logger.Debug("Sending stop to " + device2.Name + "!");
                                DevicesController.SendStop(device2);
                            }
                        }
                    }
                }
            }
        }

        public FFXIV_Vibe_Plugin.Device.Device? FindDevice(string text)
        {
            FFXIV_Vibe_Plugin.Device.Device device1 = (FFXIV_Vibe_Plugin.Device.Device)null;
            try
            {
                foreach (FFXIV_Vibe_Plugin.Device.Device device2 in this.Devices)
                {
                    if (device2.Name.Contains(text) && device2 != null)
                        device1 = device2;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            return device1;
        }

        public void SendVibeToAll(int intensity)
        {
            if (!this.IsConnected() || this.BPClient == null)
                return;
            foreach (FFXIV_Vibe_Plugin.Device.Device device in this.Devices)
            {
                device.SendVibrate(intensity, threshold: this.Profile.MAX_VIBE_THRESHOLD);
                device.SendRotate(intensity, threshold: this.Profile.MAX_VIBE_THRESHOLD);
                device.SendLinear(intensity, threshold: this.Profile.MAX_VIBE_THRESHOLD);
                device.SendOscillate(intensity, threshold: this.Profile.MAX_VIBE_THRESHOLD);
            }
        }

        public void Send(
          string command,
          FFXIV_Vibe_Plugin.Device.Device device,
          int threshold,
          int motorId = -1,
          int patternId = 0,
          float StartAfter = 0.0f,
          float StopAfter = 0.0f)
        {
            DefaultInterpolatedStringHandler interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(1, 2);
            interpolatedStringHandler1.AppendFormatted(device.Name);
            interpolatedStringHandler1.AppendLiteral(":");
            interpolatedStringHandler1.AppendFormatted<int>(motorId);
            string deviceAndMotorId = interpolatedStringHandler1.ToStringAndClear();
            this.SaveCurrentMotorAndDevicePlayingState(device, motorId);
            Pattern patternById = this.Patterns.GetPatternById(patternId);
            string[] patternSegments = patternById.Value.Split("|");
            
            interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(80, 8);
            interpolatedStringHandler1.AppendLiteral("SendPattern '");
            interpolatedStringHandler1.AppendFormatted(command);
            interpolatedStringHandler1.AppendLiteral("' pattern=");
            interpolatedStringHandler1.AppendFormatted(patternById.Name);
            interpolatedStringHandler1.AppendLiteral(" (");
            interpolatedStringHandler1.AppendFormatted<int>(patternSegments.Length);
            interpolatedStringHandler1.AppendLiteral(" segments) to ");
            interpolatedStringHandler1.AppendFormatted<FFXIV_Vibe_Plugin.Device.Device>(device);
            interpolatedStringHandler1.AppendLiteral(" motor=");
            interpolatedStringHandler1.AppendFormatted<int>(motorId);
            interpolatedStringHandler1.AppendLiteral(" startAfter=");
            interpolatedStringHandler1.AppendFormatted<float>(StartAfter);
            interpolatedStringHandler1.AppendLiteral(" stopAfter=");
            interpolatedStringHandler1.AppendFormatted<float>(StopAfter);
            interpolatedStringHandler1.AppendLiteral(" threshold=");
            interpolatedStringHandler1.AppendFormatted<int>(threshold);
            string stringAndClear1 = interpolatedStringHandler1.ToStringAndClear();
            Logger.Log(stringAndClear1);
            int startedUnixTime = this.CurrentDeviceAndMotorPlaying[deviceAndMotorId];
            bool forceStop = false;
            new Thread((ThreadStart)(() =>
            {
                if ((double)StopAfter == 0.0)
                    return;
                Thread.Sleep((int)((double)StopAfter * 1000.0));
                if (startedUnixTime != this.CurrentDeviceAndMotorPlaying[deviceAndMotorId])
                    return;
                forceStop = true;
                DefaultInterpolatedStringHandler interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(37, 2);
                interpolatedStringHandler2.AppendLiteral("Force stopping ");
                interpolatedStringHandler2.AppendFormatted(deviceAndMotorId);
                interpolatedStringHandler2.AppendLiteral(" because of StopAfter=");
                interpolatedStringHandler2.AppendFormatted<float>(StopAfter);
                string stringAndClear2 = interpolatedStringHandler2.ToStringAndClear();
                Logger.Debug(stringAndClear2);
                this.SendCommand(command, device, 0, motorId);
                this.CurrentPlayingTrigger = (Trigger)null;
            })).Start();
            new Thread((ThreadStart)(() =>
            {
                Thread.Sleep((int)((double)StartAfter * 1000.0));
                if (startedUnixTime != this.CurrentDeviceAndMotorPlaying[deviceAndMotorId])
                    return;
                for (int index = 0; index < patternSegments.Length && startedUnixTime == this.CurrentDeviceAndMotorPlaying[deviceAndMotorId]; ++index)
                {
                    string[] strArray = patternSegments[index].Split(":");
                    int intensity = Helpers.ClampIntensity(int.Parse(strArray[0]), threshold);
                    int num = int.Parse(strArray[1]);
                    DefaultInterpolatedStringHandler interpolatedStringHandler3 = new DefaultInterpolatedStringHandler(55, 4);
                    interpolatedStringHandler3.AppendLiteral("SENDING SEGMENT: command=");
                    interpolatedStringHandler3.AppendFormatted(command);
                    interpolatedStringHandler3.AppendLiteral(" intensity=");
                    interpolatedStringHandler3.AppendFormatted<int>(intensity);
                    interpolatedStringHandler3.AppendLiteral(" duration=");
                    interpolatedStringHandler3.AppendFormatted<int>(num);
                    interpolatedStringHandler3.AppendLiteral(" motorId=");
                    interpolatedStringHandler3.AppendFormatted<int>(motorId);
                    string stringAndClear3 = interpolatedStringHandler3.ToStringAndClear();
                    Logger.Debug(stringAndClear3);
                    this.SendCommand(command, device, intensity, motorId, num);
                    if (forceStop || (double)StopAfter > 0.0 && (double)StopAfter * 1000.0 + (double)startedUnixTime < (double)Helpers.GetUnix())
                    {
                        interpolatedStringHandler3 = new DefaultInterpolatedStringHandler(60, 4);
                        interpolatedStringHandler3.AppendLiteral("SENDING SEGMENT ZERO: command=");
                        interpolatedStringHandler3.AppendFormatted(command);
                        interpolatedStringHandler3.AppendLiteral(" intensity=");
                        interpolatedStringHandler3.AppendFormatted<int>(intensity);
                        interpolatedStringHandler3.AppendLiteral(" duration=");
                        interpolatedStringHandler3.AppendFormatted<int>(num);
                        interpolatedStringHandler3.AppendLiteral(" motorId=");
                        interpolatedStringHandler3.AppendFormatted<int>(motorId);
                        string stringAndClear4 = interpolatedStringHandler3.ToStringAndClear();
                        Logger.Debug(stringAndClear4);
                        this.SendCommand(command, device, 0, motorId, num);
                        break;
                    }
                    Thread.Sleep(num);
                }
            })).Start();
        }

        public void SendCommand(
          string command,
          FFXIV_Vibe_Plugin.Device.Device device,
          int intensity,
          int motorId,
          int duration = 500)
        {
            switch (command)
            {
                case "vibrate":
                    this.SendVibrate(device, intensity, motorId);
                    break;
                case "rotate":
                    this.SendRotate(device, intensity, motorId);
                    break;
                case "linear":
                    this.SendLinear(device, intensity, motorId, duration);
                    break;
                case "oscillate":
                    this.SendOscillate(device, intensity, motorId, duration);
                    break;
            }
        }

        public void SendVibrate(FFXIV_Vibe_Plugin.Device.Device device, int intensity, int motorId = -1)
        {
            device.SendVibrate(intensity, motorId, this.Profile.MAX_VIBE_THRESHOLD);
        }

        public void SendRotate(FFXIV_Vibe_Plugin.Device.Device device, int intensity, int motorId = -1, bool clockwise = true)
        {
            device.SendRotate(intensity, clockwise, motorId, this.Profile.MAX_VIBE_THRESHOLD);
        }

        public void SendLinear(FFXIV_Vibe_Plugin.Device.Device device, int intensity, int motorId = -1, int duration = 500)
        {
            device.SendLinear(intensity, duration, motorId, this.Profile.MAX_VIBE_THRESHOLD);
        }

        public void SendOscillate(FFXIV_Vibe_Plugin.Device.Device device, int intensity, int motorId = -1, int duration = 500)
        {
            device.SendOscillate(intensity, duration, motorId, this.Profile.MAX_VIBE_THRESHOLD);
        }

        public static void SendStop(FFXIV_Vibe_Plugin.Device.Device device) => device.Stop();

        private void SaveCurrentMotorAndDevicePlayingState(FFXIV_Vibe_Plugin.Device.Device device, int motorId)
        {
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
            interpolatedStringHandler.AppendFormatted(device.Name);
            interpolatedStringHandler.AppendLiteral(":");
            interpolatedStringHandler.AppendFormatted<int>(motorId);
            this.CurrentDeviceAndMotorPlaying[interpolatedStringHandler.ToStringAndClear()] = Helpers.GetUnix();
        }
    }
}
