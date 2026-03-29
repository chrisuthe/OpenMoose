using System;
using System.Runtime.InteropServices;

namespace J2534;

public class J2534Delegates
{
	public delegate ERROR_CODES PassThruOpen([MarshalAs(UnmanagedType.LPStr)] string DeviceName, ref uint DeviceID);

	public delegate ERROR_CODES PassThruClose(uint DeviceID);

	public delegate ERROR_CODES PassThruConnect(uint DeviceId, uint ProtocolID, uint Flags, uint BaudRate, ref uint ChannelID);

	public delegate ERROR_CODES PassThruDisconnect(uint ChannelID);

	public delegate ERROR_CODES PassThruReadVersion(uint DeviceID, [MarshalAs(UnmanagedType.VBByRefStr)] ref string FirmwareVersion, [MarshalAs(UnmanagedType.VBByRefStr)] ref string DllVersion, [MarshalAs(UnmanagedType.VBByRefStr)] ref string ApiVersion);

	public delegate ERROR_CODES PassThruGetLastError([MarshalAs(UnmanagedType.VBByRefStr)] ref string ErrorDescription);

	public delegate ERROR_CODES PassThruReadMsgs(uint ChannelID, IntPtr pMsg, ref uint pNumMsgs, uint Timeout);

	public delegate ERROR_CODES PassThruWriteMsgs(uint ChannelID, IntPtr pMsg, ref uint pNumMsgs, uint Timeout);

	public delegate ERROR_CODES PassThruStartPeriodicMsg(uint ChannelID, [In] PASSTHRU_MSG pMsg, ref uint pNumMsgs, uint Timeout);

	public delegate ERROR_CODES PassThruStopPeriodicMsg(uint ChannelID, uint MsgID);

	public delegate ERROR_CODES PassThruStartMsgFilter(uint ChannelID, uint FilterType, [In] PASSTHRU_MSG pMaskMsg, [In] PASSTHRU_MSG pPatternMsg, IntPtr ptr, ref uint pMsgID);

	public delegate ERROR_CODES PassThruStartMsgFilterFlowControl(uint ChannelID, uint FilterType, [In] PASSTHRU_MSG pMaskMsg, [In] PASSTHRU_MSG pPatternMsg, [In] PASSTHRU_MSG pFlowControlMsg, ref uint pMsgID);

	public delegate ERROR_CODES PassThruStopMsgFilter(uint ChannelID, uint MsgID);

	public delegate ERROR_CODES PassThruIoctl_1(uint ID, uint IoctlID, IntPtr pInput, IntPtr pOutput);

	public delegate ERROR_CODES PassThruIoctl_2(uint ID, uint IoctlID, IntPtr pInput, ref uint valueOut);

	public delegate ERROR_CODES PassThruIoctl_3(uint ID, uint IoctlID, ref uint valueIn, IntPtr pOutput);

	public delegate ERROR_CODES PassThruIoctl_4(uint ID, uint IoctlID, ref uint valueIn, ref uint valueOut);

	public delegate ERROR_CODES PassThruIoctl_5(uint ID, uint IoctlID, IntPtr pInput, [MarshalAs(UnmanagedType.VBByRefStr)] ref string valueOut);

	public delegate ERROR_CODES PassThruIoctl_CreateXonXoffFilter(uint ChannelID, uint IoctlID, J2534Proxy.S_XON_XOFF_FILTER filter, IntPtr pOutput);

	public delegate ERROR_CODES PassThruIoctl_ControlLed(uint DeviceID, uint IoctlID, S_LED_CONTROL led, IntPtr pOutput);
}
