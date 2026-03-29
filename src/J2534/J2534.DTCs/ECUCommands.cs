namespace J2534.DTCs;

public static class ECUCommands
{
	public static readonly CANPacket msgCANReadCodes = new CANPacket(new byte[8] { 203, 122, 174, 17, 0, 0, 0, 0 });

	public static readonly CANPacket msgCANClearCodes = new CANPacket(new byte[8] { 203, 122, 175, 17, 0, 0, 0, 0 });

	public static readonly CANPacket msgCheckME7ECUPresent = new CANPacket(new byte[8] { 203, 122, 185, 240, 0, 0, 0, 0 });
}
