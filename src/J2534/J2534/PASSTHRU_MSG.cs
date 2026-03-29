using System.Runtime.InteropServices;

namespace J2534;

[StructLayout(LayoutKind.Sequential)]
public class PASSTHRU_MSG
{
	public const int PASSTHRU_DATA_SIZE = 4128;

	public PROTOCOL_TYPE ProtocolID;

	public uint RxStatus;

	public uint TxFlags;

	public uint Timestamp;

	public uint DataSize;

	public uint ExtraDataIndex;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4128)]
	public byte[] Data = new byte[4128];
}
