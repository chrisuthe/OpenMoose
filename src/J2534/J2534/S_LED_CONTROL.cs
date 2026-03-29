using System.Runtime.InteropServices;

namespace J2534;

[StructLayout(LayoutKind.Sequential)]
public class S_LED_CONTROL
{
	public LED_NO ledNo;

	public LED_STATE ledState;

	public uint ledBlinkPeriod;
}
