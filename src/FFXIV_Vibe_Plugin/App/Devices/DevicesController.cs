using Buttplug.Client;
using FFXIV_Vibe_Plugin.App;
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Triggers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FFXIV_Vibe_Plugin.Device;

public class DevicesController
{
    private ConfigurationProfile Profile;
    private readonly Patterns Patterns;
    private Trigger? CurrentPlayingTrigger;
    public bool isConnected;
    public bool shouldExit;
    private readonly Dictionary<string, int> CurrentDeviceAndMotorPlaying = new Dictionary<string, int>();
    private ButtplugClient? BPClient;
    private readonly List<Device> Devices = new List<Device>();
    private bool isScanning;
    private static readonly Mutex mut = new Mutex();

    public DevicesController(ConfigurationProfile profile, Patterns patterns)
    {
        Profile = profile;
        Patterns = patterns;
    }

    public void Dispose()
    {
        shouldExit = true;
        Disconnect();
    }

    public void SetProfile(ConfigurationProfile profile) => Profile = profile;

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

        ButtplugWebsocketConnector? connector = null;

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
            await devicesController.BPClient.ConnectAsync(connector);
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
            Devices.Add(device);

            if (!Profile.VISITED_DEVICES.ContainsKey(device.Name))
            {
                Profile.VISITED_DEVICES[device.Name] = device;
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
            mut.ReleaseMutex();
        }
    }

    private void BPClient_DeviceRemoved(object? sender, DeviceRemovedEventArgs arg)
    {
        try
        {
            mut.WaitOne();
            int index = Devices.FindIndex(device => device.Id == arg.Device.Index);

            if (index <= -1)
                return;
            
            Logger.Debug($"Removed {Devices[index]}");

            Device device1 = Devices[index];
            Devices.RemoveAt(index);
            device1.IsConnected = false;
        }
        catch (Exception ex)
        {
            Logger.Error("DeviceController.BPClient_DeviceRemoved: " + ex.Message);
        }
        finally
        {
            mut.ReleaseMutex();
        }
    }

    public async void Disconnect()
    {
        Logger.Debug("Disconnecting DeviceController");

        try
        {
            Devices.Clear();
        }
        catch (Exception ex)
        {
            Logger.Error("DeviceController.Disconnect: " + ex.Message);
        }

        if (BPClient == null || !IsConnected())
            return;

        try
        {
            Thread.Sleep(100);

            if (BPClient != null)
            {
                await BPClient.DisconnectAsync();
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
            BPClient.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Error("Error while disposing BPClient", ex);
        }

        BPClient = null;
        isConnected = false;
    }

    public List<Device> GetDevices() => Devices;

    public Dictionary<string, Device> GetVisitedDevices() => Profile.VISITED_DEVICES;

    private void StartBatteryUpdaterThread()
    {
        new Thread(() =>
        {
            while (!shouldExit)
            {
                Thread.Sleep(5000);

                if (IsConnected())
                {
                    Logger.Verbose("Updating battery levels!");
                    UpdateAllBatteryLevel();
                }
            }
        })
        {
            Name = "batteryUpdaterThread"
        }.Start();
    }

    public void UpdateAllBatteryLevel()
    {
        try
        {
            foreach (Device device in GetDevices())
                device.UpdateBatteryLevel();
        }
        catch (Exception ex)
        {
            Logger.Error("DeviceController.UpdateAllBatteryLevel: " + ex.Message);
        }
    }

    public void StopAll()
    {
        foreach (Device device in GetDevices())
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
        if (!IsConnected())
            Logger.Debug($"Not connected, cannot send ${trigger}");
        else
        {
            Logger.Debug($"Sending trigger {trigger} (priority={trigger.Priority})");

            if (CurrentPlayingTrigger == null)
                CurrentPlayingTrigger = trigger;

            if (trigger.Priority < CurrentPlayingTrigger.Priority)
                Logger.Debug($"Ignoring trigger because lower priority => {trigger} < {CurrentPlayingTrigger}");
            else
            {
                CurrentPlayingTrigger = trigger;

                foreach (TriggerDevice device1 in trigger.Devices)
                {
                    Device device2 = FindDevice(device1.Name);

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
                                            Logger.Debug($"Sending {device2.Name} vibration to motor: {motorId} patternId={patternId} with threshold: {threshold1}!");
                                            Send("vibrate", device2, threshold1, motorId, patternId, startAfter, stopAfter);
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
                                            Logger.Debug($"Sending {device2.Name} rotation to motor: {motorId} patternId={patternId} with threshold: {threshold2}!");
                                            Send("rotate", device2, threshold2, motorId, patternId, startAfter, stopAfter);
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
                                            Logger.Debug($"Sending {device2.Name} linear to motor: {motorId} patternId={patternId} with threshold: {threshold3}!");
                                            Send("linear", device2, threshold3, motorId, patternId, startAfter, stopAfter);
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
                                            Logger.Debug($"Sending {device2.Name} oscillate to motor: {motorId} patternId={patternId} with threshold: {threshold4}!");
                                            Send("oscillate", device2, threshold4, motorId, patternId, startAfter, stopAfter);
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
                            SendStop(device2);
                        }
                    }
                }
            }
        }
    }

    public Device? FindDevice(string text)
    {
        Device? device1 = null;

        try
        {
            foreach (Device device2 in Devices)
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
        if (!IsConnected() || BPClient == null)
            return;

        foreach (Device device in Devices)
        {
            device.SendVibrate(intensity, threshold: Profile.MAX_VIBE_THRESHOLD);
            device.SendRotate(intensity, threshold: Profile.MAX_VIBE_THRESHOLD);
            device.SendLinear(intensity, threshold: Profile.MAX_VIBE_THRESHOLD);
            device.SendOscillate(intensity, threshold: Profile.MAX_VIBE_THRESHOLD);
        }
    }

    public void Send(
      string command,
      Device device,
      int threshold,
      int motorId = -1,
      int patternId = 0,
      float StartAfter = 0.0f,
      float StopAfter = 0.0f)
    {
        string deviceAndMotorId = $"{device.Name}:{motorId}";

        SaveCurrentMotorAndDevicePlayingState(device, motorId);

        Pattern patternById = Patterns.GetPatternById(patternId);
        string[] patternSegments = patternById.Value.Split("|");

        Logger.Log($"SendPattern '{command}' pattern={patternById.Name} ({patternSegments.Length} segments) to {device} motor={motorId} startAfter={StartAfter} stopAfter={StopAfter} threshold={threshold}");

        int startedUnixTime = CurrentDeviceAndMotorPlaying[deviceAndMotorId];
        bool forceStop = false;

        new Thread(() =>
        {
            if ((double)StopAfter == 0.0)
                return;

            Thread.Sleep((int)((double)StopAfter * 1000.0));

            if (startedUnixTime != CurrentDeviceAndMotorPlaying[deviceAndMotorId])
                return;

            forceStop = true;

            Logger.Debug($"Force stopping {deviceAndMotorId} because of StopAfter={StopAfter}");

            SendCommand(command, device, 0, motorId);
            CurrentPlayingTrigger = null;
        }).Start();

        new Thread(() =>
        {
            Thread.Sleep((int)((double)StartAfter * 1000.0));

            if (startedUnixTime != CurrentDeviceAndMotorPlaying[deviceAndMotorId])
                return;

            for (int index = 0; index < patternSegments.Length && startedUnixTime == CurrentDeviceAndMotorPlaying[deviceAndMotorId]; ++index)
            {
                string[] strArray = patternSegments[index].Split(":");
                int intensity = Helpers.ClampIntensity(int.Parse(strArray[0]), threshold);
                int num = int.Parse(strArray[1]);

                Logger.Debug($"SENDING SEGMENT: command={command} intensity={intensity} duration={num} motorId={motorId}");

                SendCommand(command, device, intensity, motorId, num);

                if (forceStop || (double)StopAfter > 0.0 && (double)StopAfter * 1000.0 + startedUnixTime < Helpers.GetUnix())
                {
                    Logger.Debug($"SENDING SEGMENT ZERO: command={command} intensity={intensity} duration={num} motorId={motorId}");

                    SendCommand(command, device, 0, motorId, num);
                    break;
                }
                Thread.Sleep(num);
            }
        }).Start();
    }

    public void SendCommand(
      string command,
      Device device,
      int intensity,
      int motorId,
      int duration = 500)
    {
        switch (command)
        {
            case "vibrate":
                SendVibrate(device, intensity, motorId);
                break;
            case "rotate":
                SendRotate(device, intensity, motorId);
                break;
            case "linear":
                SendLinear(device, intensity, motorId, duration);
                break;
            case "oscillate":
                SendOscillate(device, intensity, motorId, duration);
                break;
        }
    }

    public void SendVibrate(Device device, int intensity, int motorId = -1)
    {
        device.SendVibrate(intensity, motorId, Profile.MAX_VIBE_THRESHOLD);
    }

    public void SendRotate(Device device, int intensity, int motorId = -1, bool clockwise = true)
    {
        device.SendRotate(intensity, clockwise, motorId, Profile.MAX_VIBE_THRESHOLD);
    }

    public void SendLinear(Device device, int intensity, int motorId = -1, int duration = 500)
    {
        device.SendLinear(intensity, duration, motorId, Profile.MAX_VIBE_THRESHOLD);
    }

    public void SendOscillate(Device device, int intensity, int motorId = -1, int duration = 500)
    {
        device.SendOscillate(intensity, duration, motorId, Profile.MAX_VIBE_THRESHOLD);
    }

    public static void SendStop(Device device) => device.Stop();

    private void SaveCurrentMotorAndDevicePlayingState(Device device, int motorId)
    {
        CurrentDeviceAndMotorPlaying[$"{device.Name}:{motorId}"] = Helpers.GetUnix();
    }
}
