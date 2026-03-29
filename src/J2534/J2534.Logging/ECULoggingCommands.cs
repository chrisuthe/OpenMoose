namespace J2534.Logging;

public static class ECULoggingCommands
{
	public static readonly CANPacket msgCANClearRecordReqs = new CANPacket(new byte[8] { 203, 122, 170, 0, 0, 0, 0, 0 });

	public static readonly CANPacket msgCANRequestRecordSetOnce = new CANPacket(new byte[8] { 205, 122, 166, 240, 0, 1, 0, 0 });

	public static readonly CANPacket msgCANRequestRecordSetFast = new CANPacket(new byte[8] { 205, 122, 166, 240, 0, 4, 0, 0 });

	public static readonly CANPacket msgCANRequestRecordSetStop = new CANPacket(new byte[8] { 205, 122, 166, 240, 0, 0, 0, 0 });

	public static readonly CANPacket msgCANTesterPresent = new CANPacket(new byte[8] { 216, 0, 0, 0, 0, 0, 0, 0 });

	public static readonly CANPacket msgCEMEngineState = new CANPacket(new byte[8] { 205, 80, 166, 26, 5, 1, 0, 0 });
}
