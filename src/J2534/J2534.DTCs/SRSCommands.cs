namespace J2534.DTCs;

public static class SRSCommands
{
	public static readonly CANPacket msgCANReadCodes = new CANPacket(new byte[8] { 203, 88, 174, 17, 0, 0, 0, 0 });

	public static readonly CANPacket msgCANClearCodes = new CANPacket(new byte[8] { 203, 88, 175, 17, 0, 0, 0, 0 });
}
