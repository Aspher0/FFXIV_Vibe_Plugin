namespace FFXIV_Vibe_Plugin.Triggers;

public class TriggerDevice
{
    public string Name = "";
    public bool IsEnabled;
    public bool ShouldVibrate;
    public bool ShouldRotate;
    public bool ShouldLinear;
    public bool ShouldOscillate;
    public bool ShouldStop;
    public Device.Device? Device;
    public bool[] VibrateSelectedMotors;
    public int[] VibrateMotorsThreshold;
    public int[] VibrateMotorsPattern;
    public bool[] RotateSelectedMotors;
    public int[] RotateMotorsThreshold;
    public int[] RotateMotorsPattern;
    public bool[] LinearSelectedMotors;
    public int[] LinearMotorsThreshold;
    public int[] LinearMotorsPattern;
    public bool[] OscillateSelectedMotors;
    public int[] OscillateMotorsThreshold;
    public int[] OscillateMotorsPattern;

    public TriggerDevice(Device.Device device)
    {
        Name = device.Name;
        Device = device;
        VibrateSelectedMotors = new bool[device.CanVibrate ? device.VibrateMotors : 0];
        VibrateMotorsThreshold = new int[device.CanVibrate ? device.VibrateMotors : 0];
        VibrateMotorsPattern = new int[device.CanVibrate ? device.VibrateMotors : 0];
        RotateSelectedMotors = new bool[device.CanRotate ? device.RotateMotors : 0];
        RotateMotorsThreshold = new int[device.CanRotate ? device.RotateMotors : 0];
        RotateMotorsPattern = new int[device.CanRotate ? device.RotateMotors : 0];
        LinearSelectedMotors = new bool[device.CanLinear ? device.LinearMotors : 0];
        LinearMotorsThreshold = new int[device.CanLinear ? device.LinearMotors : 0];
        LinearMotorsPattern = new int[device.CanLinear ? device.LinearMotors : 0];
        OscillateSelectedMotors = new bool[device.CanOscillate ? device.OscillateMotors : 0];
        OscillateMotorsThreshold = new int[device.CanOscillate ? device.OscillateMotors : 0];
        OscillateMotorsPattern = new int[device.CanOscillate ? device.OscillateMotors : 0];
    }

    public override string ToString() => "TRIGGER_DEVICE " + Name;
}
