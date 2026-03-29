using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace J2534;

public class ToolComm
{
	public J2534Proxy dice;

	private string deviceName;

	private uint deviceID;

	private uint kLineChannelID;

	private uint msCANChannel;

	private uint hsCANChannel;

	private bool xonxoff;

	private BaudRate hsBaudRate;

	public bool DeviceOpen { get; set; }

	public ToolComm()
	{
	}

	public ToolComm(J2534Device d, BaudRate hsBaudRate)
	{
		dice = new J2534Proxy(d);
		this.hsBaudRate = hsBaudRate;
	}

	public void startPeriodicMsg(CANPacket msg, ref uint msgid, uint interval, CANChannel channelType)
	{
		if (!xonxoff)
		{
			msg.setProtocolType(PROTOCOL_TYPE.CAN);
		}
		if (channelType == CANChannel.HS)
		{
			PASSTHRU_MSG passThruMsg = msg.getPassThruMsg(CAN_FLAGS.CAN_EXTENDED);
			dice.PassThruStartPeriodicMsg(hsCANChannel, passThruMsg, ref msgid, interval);
		}
		else
		{
			PASSTHRU_MSG passThruMsg2 = msg.getPassThruMsg((CAN_FLAGS)536871168u);
			dice.PassThruStartPeriodicMsg(msCANChannel, passThruMsg2, ref msgid, interval);
		}
	}

	public void stopPeriodicMsg(uint msgid, CANChannel channelType)
	{
		if (channelType == CANChannel.HS)
		{
			dice.PassThruStopPeriodicMsg(hsCANChannel, msgid);
		}
		else
		{
			dice.PassThruStopPeriodicMsg(msCANChannel, msgid);
		}
	}

	public void sendMsg(CANPacket CANMsg, CANChannel channelType)
	{
		sendMsgHelper(CANMsg, channelType, clearRXBuffer: false, 240000u);
	}

	public void sendMsg(List<CANPacket> qMsg, CANChannel channelType)
	{
		CANPacket cANMsg = new CANPacket(qMsg);
		sendMsgHelper(cANMsg, channelType, clearRXBuffer: false, 240000u);
	}

	public void sendMsg(CANPacket CANMsg, CANChannel channelType, uint timeout)
	{
		sendMsgHelper(CANMsg, channelType, clearRXBuffer: false, timeout);
	}

	public bool sendMsgCheckResponse(CANPacket CANMsg, CANChannel channelType, byte responseByte)
	{
		uint numMsgs = 1u;
		return sendMsgCheckResponse(CANMsg, channelType, responseByte, ref numMsgs);
	}

	public bool sendMsgCheckResponse(CANPacket CANMsg, CANChannel channelType, byte responseByte, ref uint numMsgs)
	{
		uint channelID = ((channelType == CANChannel.HS) ? hsCANChannel : msCANChannel);
		uint timeout = 90000u;
		sendMsgHelper(CANMsg, channelType, clearRXBuffer: true, 240000u);
		IntPtr intPtr = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(PASSTHRU_MSG)) * numMsgs));
		dice.PassThruReadMsgs(deviceID, channelID, intPtr, ref numMsgs, timeout);
		J2534Message j2534Message = new J2534Message(intPtr, (int)numMsgs, (int)(numMsgs + 1));
		for (int i = 0; i < numMsgs; i++)
		{
			if (new CANPacket(j2534Message[i]).getResponseByte() == responseByte)
			{
				return true;
			}
		}
		Marshal.FreeHGlobal(j2534Message.Pointer);
		return false;
	}

	public bool sendMsgCheckDiagResponse(CANPacket CANMsg, CANChannel channelType, byte responseByte)
	{
		uint numMsgs = 1u;
		return sendMsgCheckDiagResponse(CANMsg, channelType, responseByte, ref numMsgs);
	}

	public bool sendMsgCheckDiagResponse(CANPacket CANMsg, CANChannel channelType, byte responseByte, ref uint numMsgs)
	{
		uint channelID = ((channelType == CANChannel.HS) ? hsCANChannel : msCANChannel);
		uint timeout = 900u;
		sendMsgHelper(CANMsg, channelType, clearRXBuffer: true, 900u);
		IntPtr intPtr = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(PASSTHRU_MSG)) * numMsgs));
		dice.PassThruReadMsgs(deviceID, channelID, intPtr, ref numMsgs, timeout);
		J2534Message j2534Message = new J2534Message(intPtr, (int)numMsgs, (int)(numMsgs + 1));
		for (int i = 0; i < numMsgs; i++)
		{
			if (new CANPacket(j2534Message[i]).getDiagResponseByte() == responseByte)
			{
				return true;
			}
		}
		Marshal.FreeHGlobal(j2534Message.Pointer);
		return false;
	}

	public CANPacket sendMsgReadResponse(CANPacket CANMsg, CANChannel channelType)
	{
		uint numMsgs = 1u;
		List<CANPacket> list = sendMsgReadResponse(CANMsg, channelType, ref numMsgs);
		if (list.Count <= 0)
		{
			return null;
		}
		return list[0];
	}

	public List<CANPacket> sendMsgReadResponse(CANPacket CANMsg, CANChannel channelType, ref uint numMsgs)
	{
		return sendMsgReadResponse(CANMsg, channelType, ref numMsgs, 900u);
	}

	public List<CANPacket> sendMsgReadResponse(CANPacket CANMsg, CANChannel channelType, ref uint maxNumMsgs, uint timeout)
	{
		uint channelID = ((channelType == CANChannel.HS) ? hsCANChannel : msCANChannel);
		sendMsgHelper(CANMsg, channelType, clearRXBuffer: true, 1000u);
		IntPtr intPtr = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(PASSTHRU_MSG)) * maxNumMsgs));
		List<CANPacket> list = new List<CANPacket>();
		dice.PassThruReadMsgs(deviceID, channelID, intPtr, ref maxNumMsgs, timeout);
		J2534Message j2534Message = new J2534Message(intPtr, (int)maxNumMsgs, (int)(maxNumMsgs + 1));
		for (int i = 0; i < maxNumMsgs; i++)
		{
			CANPacket item = new CANPacket(j2534Message[i]);
			list.Add(item);
		}
		Marshal.FreeHGlobal(intPtr);
		return list;
	}

	public List<CANPacket> ReadResponse(CANChannel channelType, ref uint maxNumMsgs, uint timeout)
	{
		uint channelID = ((channelType == CANChannel.HS) ? hsCANChannel : msCANChannel);
		IntPtr intPtr = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(PASSTHRU_MSG)) * maxNumMsgs));
		List<CANPacket> list = new List<CANPacket>();
		dice.PassThruReadMsgs(deviceID, channelID, intPtr, ref maxNumMsgs, timeout);
		J2534Message j2534Message = new J2534Message(intPtr, (int)maxNumMsgs, (int)(maxNumMsgs + 1));
		for (int i = 0; i < maxNumMsgs; i++)
		{
			CANPacket item = new CANPacket(j2534Message[i]);
			list.Add(item);
		}
		Marshal.FreeHGlobal(intPtr);
		return list;
	}

	public CANPacket sendMsgReadSBLResponse(CANPacket CANMsg, CANChannel channelType)
	{
		uint pNumMsgs = 1u;
		uint timeout = 2000u;
		CANPacket cANMsg = new CANPacket(CANMsg);
		uint channelID = ((channelType == CANChannel.HS) ? hsCANChannel : msCANChannel);
		sendMsgHelper(CANMsg, channelType, clearRXBuffer: true, 240000u);
		IntPtr intPtr = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(PASSTHRU_MSG)) * pNumMsgs));
		ERROR_CODES eRROR_CODES = ERROR_CODES.STATUS_NOERROR;
		List<CANPacket> list = new List<CANPacket>();
		eRROR_CODES = dice.PassThruReadMsgs(deviceID, channelID, intPtr, ref pNumMsgs, timeout);
		if (eRROR_CODES == ERROR_CODES.ERR_BUFFER_EMPTY || eRROR_CODES == ERROR_CODES.ERR_TIMEOUT)
		{
			sendMsgHelper(cANMsg, channelType, clearRXBuffer: true, 1000u);
			eRROR_CODES = dice.PassThruReadMsgs(deviceID, channelID, intPtr, ref pNumMsgs, timeout);
		}
		J2534Message j2534Message = new J2534Message(intPtr, (int)pNumMsgs, (int)(pNumMsgs + 1));
		for (int i = 0; i < pNumMsgs; i++)
		{
			CANPacket item = new CANPacket(j2534Message[i]);
			list.Add(item);
		}
		Marshal.FreeHGlobal(intPtr);
		if (list.Count <= 0)
		{
			return null;
		}
		return list[0];
	}

	public bool waitForMsg(CANPacket CANMsg, CANChannel channelType, uint maxNumMsgs, uint timeout)
	{
		uint channelID = ((channelType == CANChannel.HS) ? hsCANChannel : msCANChannel);
		IntPtr intPtr = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(PASSTHRU_MSG)) * maxNumMsgs));
		new List<CANPacket>();
		bool result = false;
		dice.PassThruReadMsgs(deviceID, channelID, intPtr, ref maxNumMsgs, timeout);
		if (maxNumMsgs != 0)
		{
			J2534Message j2534Message = new J2534Message(intPtr, (int)maxNumMsgs, (int)(maxNumMsgs + 1));
			for (int i = 0; i < maxNumMsgs; i++)
			{
				if (new CANPacket(j2534Message[i]).Equals(CANMsg))
				{
					result = true;
				}
			}
		}
		Marshal.FreeHGlobal(intPtr);
		return result;
	}

	private void sendMsgHelper(CANPacket CANMsg, CANChannel channelType, bool clearRXBuffer, uint timeout)
	{
		if (!xonxoff)
		{
			CANMsg.setProtocolType(PROTOCOL_TYPE.CAN);
		}
		CAN_FLAGS txFlags = ((channelType == CANChannel.HS) ? CAN_FLAGS.CAN_EXTENDED : ((CAN_FLAGS)536871168u));
		uint channelID = ((channelType == CANChannel.HS) ? hsCANChannel : msCANChannel);
		J2534Message j2534Msgs = CANMsg.getJ2534Msgs(txFlags);
		dice.PassThruWriteMsgs(deviceID, channelID, j2534Msgs.Pointer, (uint)j2534Msgs.Length, timeout, clearRXBuffer);
		Marshal.FreeHGlobal(j2534Msgs.Pointer);
	}

	public void sendDIMMsg(List<CANPacket> cl, CANChannel channelType)
	{
		foreach (CANPacket item in cl)
		{
			sendMsgHelper(item, channelType, clearRXBuffer: false, 240000u);
			Thread.Sleep(30);
		}
	}

	public bool connect()
	{
		return connect(xonxoff: true);
	}

	public bool connect(bool xonxoff)
	{
		this.xonxoff = xonxoff;
		if (!DeviceOpen)
		{
			dice.PassThruOpen(ref deviceName, ref deviceID, deviceIsDiCECompatible: true);
			bool flag = connectCANHS() && connectCANMS();
			if (hsBaudRate == BaudRate.CAN_250000)
			{
				flag &= connectKLINE();
			}
			DeviceOpen = true;
			return flag;
		}
		return false;
	}

	public bool connectCANHS()
	{
		try
		{
			CreateCanChannel((uint)hsBaudRate, ref hsCANChannel);
			StartTesterFilter(hsCANChannel, CAN_FLAGS.CAN_EXTENDED);
			if (xonxoff)
			{
				StartXonXoffFilter(hsCANChannel, CAN_FLAGS.CAN_EXTENDED);
				StMin(hsCANChannel, 0u);
			}
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			return false;
		}
	}

	private bool connectCANMS()
	{
		try
		{
			CreateCanChannel(125000u, ref msCANChannel);
			StartTesterFilter(msCANChannel, (CAN_FLAGS)536871168u);
			if (xonxoff)
			{
				StartXonXoffFilter(msCANChannel, (CAN_FLAGS)536871168u);
				StMin(msCANChannel, 0u);
			}
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			return false;
		}
	}

	public bool connectKLINE()
	{
		uint pMsgID = 0u;
		CreateIso9141Channel(10400u, ref kLineChannelID);
		PASSTHRU_MSG pASSTHRU_MSG = new PASSTHRU_MSG();
		pASSTHRU_MSG.Data[0] = 132;
		pASSTHRU_MSG.Data[1] = 64;
		pASSTHRU_MSG.Data[2] = 19;
		pASSTHRU_MSG.Data[3] = 178;
		pASSTHRU_MSG.Data[4] = 240;
		pASSTHRU_MSG.Data[5] = 3;
		pASSTHRU_MSG.DataSize = 6u;
		pASSTHRU_MSG.ProtocolID = PROTOCOL_TYPE.ISO9141;
		pASSTHRU_MSG.TxFlags = 4096u;
		dice.PassThruStartPeriodicMsg(kLineChannelID, pASSTHRU_MSG, ref pMsgID, 2000u);
		return true;
	}

	public bool disconnect()
	{
		if (DeviceOpen)
		{
			if (hsCANChannel != 0 || msCANChannel != 0 || kLineChannelID != 0)
			{
				if (hsCANChannel != 0)
				{
					dice.PassThruDisconnect(hsCANChannel);
				}
				if (msCANChannel != 0)
				{
					dice.PassThruDisconnect(msCANChannel);
				}
				if (kLineChannelID != 0)
				{
					dice.PassThruDisconnect(kLineChannelID);
				}
				dice.PassThruClose(deviceID);
				DeviceOpen = false;
				return true;
			}
			return false;
		}
		return false;
	}

	public bool disconnectFinal()
	{
		if (DeviceOpen)
		{
			if (hsCANChannel != 0 || msCANChannel != 0 || kLineChannelID != 0)
			{
				if (hsCANChannel != 0)
				{
					dice.PassThruDisconnect(hsCANChannel);
				}
				if (msCANChannel != 0)
				{
					dice.PassThruDisconnect(msCANChannel);
				}
				if (kLineChannelID != 0)
				{
					dice.PassThruDisconnect(kLineChannelID);
				}
				dice.PassThruClose(deviceID);
				dice.UnloadDll();
				DeviceOpen = false;
				return true;
			}
			return false;
		}
		return false;
	}

	public bool disconnectCANHS()
	{
		if (DeviceOpen)
		{
			if (hsCANChannel != 0)
			{
				if (hsCANChannel != 0)
				{
					dice.PassThruDisconnect(hsCANChannel);
				}
				return true;
			}
			return false;
		}
		return false;
	}

	public BaudRate getHSBaud()
	{
		return hsBaudRate;
	}

	public bool setHSBaud(BaudRate hsBaudRate)
	{
		if (!DeviceOpen)
		{
			this.hsBaudRate = hsBaudRate;
			return true;
		}
		return false;
	}

	public bool setJ2534Device(J2534Device d)
	{
		if (!DeviceOpen)
		{
			dice = new J2534Proxy(d);
			return true;
		}
		return false;
	}

	private void CreateCanChannel(uint baudRate, ref uint channel)
	{
		uint num = 256u;
		uint num2 = 68u;
		if (baudRate == 125000)
		{
			num |= 0x20000000;
		}
		if (dice.PassThruConnect(deviceID, xonxoff ? PROTOCOL_TYPE.CAN_XON_XOFF : PROTOCOL_TYPE.CAN, num, baudRate, ref channel) != ERROR_CODES.STATUS_NOERROR)
		{
			throw new CommToolException("J2534 hardware could not create CAN channel");
		}
		if (baudRate == 500000)
		{
			num2 = 80u;
		}
		uint[,] obj = new uint[3, 2]
		{
			{ 1u, 0u },
			{ 3u, 0u },
			{ 23u, 0u }
		};
		obj[0, 1] = baudRate;
		obj[2, 1] = num2;
		uint[,] parameterAndValues = obj;
		if (dice.PassThruIoctl_SET_CONFIG(channel, parameterAndValues) != ERROR_CODES.STATUS_NOERROR)
		{
			dice.PassThruDisconnect(channel);
			throw new CommToolException("J2534 hardware could not configure CAN_XON_XOFF channel");
		}
	}

	private void CreateIso9141Channel(uint baudRate, ref uint channel)
	{
		if (dice.PassThruConnect(deviceID, PROTOCOL_TYPE.ISO9141, 4096u, baudRate, ref channel) == ERROR_CODES.STATUS_NOERROR)
		{
			uint[,] parameterAndValues = new uint[4, 2]
			{
				{ 22u, 0u },
				{ 25u, 60u },
				{ 14u, 600u },
				{ 12u, 0u }
			};
			if (dice.PassThruIoctl_SET_CONFIG(channel, parameterAndValues) != ERROR_CODES.STATUS_NOERROR)
			{
				dice.PassThruDisconnect(channel);
				throw new CommToolException("J2534 hardware could not configure ISO9141 channel");
			}
		}
	}

	private bool CreateXonXoffFilter(uint channel, PASSTHRU_MSG xonMask, PASSTHRU_MSG xonPattern, PASSTHRU_MSG xoffMask, PASSTHRU_MSG xoffPattern, PASSTHRU_MSG errorMask, PASSTHRU_MSG errorPattern)
	{
		ERROR_CODES eRROR_CODES = dice.PassThruIoctl_CreateXonXoffFilter(channel, xonMask, xonPattern, xoffMask, xoffPattern, errorMask, errorPattern);
		if (eRROR_CODES == ERROR_CODES.ERR_DEVICE_NOT_CONNECTED)
		{
			DeviceOpen = false;
		}
		return FilterHardwareErrors(eRROR_CODES);
	}

	private bool FilterHardwareErrors(ERROR_CODES J2534ReturnCode)
	{
		return J2534ReturnCode switch
		{
			ERROR_CODES.STATUS_NOERROR => true, 
			ERROR_CODES.ERR_INVALID_CHANNEL_ID => throw new CommToolException("J2534 hardware error: invalid channel"), 
			ERROR_CODES.ERR_FAILED => throw new CommToolException("J2534 hardware error: failed"), 
			ERROR_CODES.ERR_DEVICE_NOT_CONNECTED => throw new CommToolException("J2534 hardware error: device not connected"), 
			ERROR_CODES.ERR_BUFFER_EMPTY => true, 
			_ => false, 
		};
	}

	private void StartTesterFilter(uint channelID, CAN_FLAGS txFlags)
	{
		byte[] array = new byte[4] { 0, 0, 0, 1 };
		byte[] array2 = new byte[4] { 0, 0, 0, 1 };
		PASSTHRU_MSG pASSTHRU_MSG = new PASSTHRU_MSG();
		pASSTHRU_MSG.ProtocolID = (xonxoff ? PROTOCOL_TYPE.CAN_XON_XOFF : PROTOCOL_TYPE.CAN);
		pASSTHRU_MSG.RxStatus = 0u;
		pASSTHRU_MSG.TxFlags = (uint)txFlags;
		pASSTHRU_MSG.Timestamp = 0u;
		pASSTHRU_MSG.ExtraDataIndex = 0u;
		array.CopyTo(pASSTHRU_MSG.Data, 0);
		pASSTHRU_MSG.DataSize = (uint)array.Length;
		PASSTHRU_MSG pASSTHRU_MSG2 = new PASSTHRU_MSG();
		pASSTHRU_MSG2.ProtocolID = (xonxoff ? PROTOCOL_TYPE.CAN_XON_XOFF : PROTOCOL_TYPE.CAN);
		pASSTHRU_MSG2.RxStatus = 0u;
		pASSTHRU_MSG2.TxFlags = (uint)txFlags;
		pASSTHRU_MSG2.Timestamp = 0u;
		pASSTHRU_MSG2.ExtraDataIndex = 0u;
		array2.CopyTo(pASSTHRU_MSG2.Data, 0);
		pASSTHRU_MSG2.DataSize = (uint)array2.Length;
		uint pMsgID = 0u;
		dice.PassThruStartMsgFilter(channelID, J2534Proxy.FILTER_TYPE.PASS_FILTER, pASSTHRU_MSG, pASSTHRU_MSG2, ref pMsgID);
	}

	private void StartXonXoffFilter(uint channelID, CAN_FLAGS txFlags)
	{
		byte[] array = new byte[8] { 0, 0, 0, 1, 0, 255, 255, 0 };
		byte[] array2 = new byte[8] { 0, 0, 0, 1, 0, 169, 0, 0 };
		byte[] array3 = new byte[8] { 0, 0, 0, 1, 0, 255, 255, 0 };
		byte[] array4 = new byte[8] { 0, 0, 0, 1, 0, 169, 1, 0 };
		byte[] array5 = new byte[8] { 0, 0, 0, 1, 0, 255, 255, 0 };
		byte[] array6 = new byte[8] { 0, 0, 0, 1, 0, 169, 2, 0 };
		PASSTHRU_MSG pASSTHRU_MSG = new PASSTHRU_MSG();
		pASSTHRU_MSG.ProtocolID = PROTOCOL_TYPE.CAN_XON_XOFF;
		pASSTHRU_MSG.RxStatus = 0u;
		pASSTHRU_MSG.TxFlags = (uint)txFlags;
		pASSTHRU_MSG.Timestamp = 0u;
		pASSTHRU_MSG.ExtraDataIndex = 0u;
		array.CopyTo(pASSTHRU_MSG.Data, 0);
		pASSTHRU_MSG.DataSize = (uint)array.Length;
		PASSTHRU_MSG pASSTHRU_MSG2 = new PASSTHRU_MSG();
		pASSTHRU_MSG2.ProtocolID = PROTOCOL_TYPE.CAN_XON_XOFF;
		pASSTHRU_MSG2.RxStatus = 0u;
		pASSTHRU_MSG2.TxFlags = (uint)txFlags;
		pASSTHRU_MSG2.Timestamp = 0u;
		pASSTHRU_MSG2.ExtraDataIndex = 0u;
		array2.CopyTo(pASSTHRU_MSG2.Data, 0);
		pASSTHRU_MSG2.DataSize = (uint)array2.Length;
		PASSTHRU_MSG pASSTHRU_MSG3 = new PASSTHRU_MSG();
		pASSTHRU_MSG3.ProtocolID = PROTOCOL_TYPE.CAN_XON_XOFF;
		pASSTHRU_MSG3.RxStatus = 0u;
		pASSTHRU_MSG3.TxFlags = (uint)txFlags;
		pASSTHRU_MSG3.Timestamp = 0u;
		pASSTHRU_MSG3.ExtraDataIndex = 0u;
		array3.CopyTo(pASSTHRU_MSG3.Data, 0);
		pASSTHRU_MSG3.DataSize = (uint)array3.Length;
		PASSTHRU_MSG pASSTHRU_MSG4 = new PASSTHRU_MSG();
		pASSTHRU_MSG4.ProtocolID = PROTOCOL_TYPE.CAN_XON_XOFF;
		pASSTHRU_MSG4.RxStatus = 0u;
		pASSTHRU_MSG4.TxFlags = (uint)txFlags;
		pASSTHRU_MSG4.Timestamp = 0u;
		pASSTHRU_MSG4.ExtraDataIndex = 0u;
		array4.CopyTo(pASSTHRU_MSG4.Data, 0);
		pASSTHRU_MSG4.DataSize = (uint)array4.Length;
		PASSTHRU_MSG pASSTHRU_MSG5 = new PASSTHRU_MSG();
		pASSTHRU_MSG5.ProtocolID = PROTOCOL_TYPE.CAN_XON_XOFF;
		pASSTHRU_MSG5.RxStatus = 0u;
		pASSTHRU_MSG5.TxFlags = (uint)txFlags;
		pASSTHRU_MSG5.Timestamp = 0u;
		pASSTHRU_MSG5.ExtraDataIndex = 0u;
		array5.CopyTo(pASSTHRU_MSG5.Data, 0);
		pASSTHRU_MSG5.DataSize = (uint)array5.Length;
		PASSTHRU_MSG pASSTHRU_MSG6 = new PASSTHRU_MSG();
		pASSTHRU_MSG6.ProtocolID = PROTOCOL_TYPE.CAN_XON_XOFF;
		pASSTHRU_MSG6.RxStatus = 0u;
		pASSTHRU_MSG6.TxFlags = (uint)txFlags;
		pASSTHRU_MSG6.Timestamp = 0u;
		pASSTHRU_MSG6.ExtraDataIndex = 0u;
		array6.CopyTo(pASSTHRU_MSG6.Data, 0);
		pASSTHRU_MSG6.DataSize = (uint)array6.Length;
		CreateXonXoffFilter(channelID, pASSTHRU_MSG, pASSTHRU_MSG2, pASSTHRU_MSG3, pASSTHRU_MSG4, pASSTHRU_MSG5, pASSTHRU_MSG6);
	}

	private bool StMin(uint channel, uint canXONXOFFStMin)
	{
		uint[,] parameterAndValues = new uint[1, 2] { { 268435457u, canXONXOFFStMin } };
		ERROR_CODES eRROR_CODES = dice.PassThruIoctl_SET_CONFIG(channel, parameterAndValues);
		if (eRROR_CODES == ERROR_CODES.ERR_DEVICE_NOT_CONNECTED)
		{
			DeviceOpen = false;
		}
		return FilterHardwareErrors(eRROR_CODES);
	}

	public static List<J2534Device> getInstalledDevices()
	{
		return J2534Detect.ListDevices();
	}
}
