namespace J2534.P2.DIM;

public static class VolvoDIMCommands
{
	public static readonly CANPacket msgCANSetTime = new CANPacket(new byte[8] { 207, 81, 176, 7, 1, 255, 0, 0 });
}
