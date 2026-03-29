using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace J2534;

public class CANPacket
{
	private PROTOCOL_TYPE protocolType;

	public byte[] data { get; set; }

	public CANPacket(byte[] data)
	{
		this.data = new byte[data.Length + 4];
		this.data[0] = 0;
		this.data[1] = 15;
		this.data[2] = byte.MaxValue;
		this.data[3] = 254;
		for (int i = 0; i < data.Length; i++)
		{
			this.data[i + 4] = data[i];
		}
		protocolType = PROTOCOL_TYPE.CAN_XON_XOFF;
	}

	public CANPacket(byte[] data, int eid)
	{
		byte[] array = new byte[4];
		array[3] = (byte)eid;
		array[2] = (byte)(eid >> 8);
		array[1] = (byte)(eid >> 16);
		array[0] = (byte)(eid >> 24);
		setupCANPacketEID(data, array);
	}

	public CANPacket(byte[] data, byte[] eid)
	{
		setupCANPacketEID(data, eid);
	}

	private void setupCANPacketEID(byte[] data, byte[] eid)
	{
		this.data = new byte[data.Length + eid.Length];
		for (int i = 0; i < eid.Length; i++)
		{
			this.data[i] = eid[i];
		}
		for (int j = 0; j < data.Length; j++)
		{
			this.data[j + 4] = data[j];
		}
		protocolType = PROTOCOL_TYPE.CAN_XON_XOFF;
	}

	public CANPacket(CANPacket pMsg)
	{
		data = new byte[pMsg.data.Length];
		for (int i = 0; i < data.Length; i++)
		{
			data[i] = pMsg.data[i];
		}
		protocolType = PROTOCOL_TYPE.CAN_XON_XOFF;
	}

	public CANPacket(PASSTHRU_MSG pMsg)
	{
		data = new byte[pMsg.DataSize];
		for (int i = 0; i < data.Length; i++)
		{
			data[i] = pMsg.Data[i];
		}
		protocolType = PROTOCOL_TYPE.CAN_XON_XOFF;
	}

	public CANPacket(List<CANPacket> qMsg)
	{
		if (qMsg.Count < 1)
		{
			throw new Exception();
		}
		byte[] array = new byte[4 + qMsg.Count * (qMsg[0].data.Length - 4)];
		for (int i = 0; i < 4; i++)
		{
			array[i] = qMsg[0].data[i];
		}
		Queue<CANPacket> queue = new Queue<CANPacket>(qMsg);
		int num = 4;
		while (queue.Count > 0)
		{
			byte[] array2 = queue.Dequeue().data;
			for (int j = 4; j < array2.Length; j++)
			{
				array[num++] = array2[j];
			}
		}
		data = array;
		protocolType = PROTOCOL_TYPE.CAN_XON_XOFF;
	}

	public void setMsgData(byte[] mData)
	{
		for (int i = 0; i < mData.Length; i++)
		{
			data[i + 6] = mData[i];
		}
	}

	public void setTimeData(byte[] mData)
	{
		for (int i = 0; i < mData.Length; i++)
		{
			data[i + 10] = mData[i];
		}
	}

	public void setAddr(int addr)
	{
		if (addr > 65535)
		{
			data[7] = (byte)((0xFF0000 & addr) >> 16);
			data[8] = (byte)((0xFF00 & addr) >> 8);
			data[9] = (byte)(0xFF & addr);
		}
		else
		{
			data[7] = 0;
			data[8] = (byte)((0xFF00 & addr) >> 8);
			data[9] = (byte)(0xFF & addr);
		}
	}

	public void setDiagModuleAddress(byte addr)
	{
		data[5] = addr;
	}

	public void setDiagData(byte[] diagData)
	{
		int num = ((data.Length > diagData.Length) ? diagData.Length : data.Length);
		for (int i = 0; i < num; i++)
		{
			data[i + 8] = diagData[i];
		}
	}

	public byte getResponseByte()
	{
		return data[5];
	}

	public byte getDiagResponseByte()
	{
		return data[6];
	}

	public PASSTHRU_MSG getPassThruMsg(CAN_FLAGS txFlags)
	{
		PASSTHRU_MSG pASSTHRU_MSG = new PASSTHRU_MSG();
		pASSTHRU_MSG.DataSize = (uint)data.Length;
		pASSTHRU_MSG.ProtocolID = protocolType;
		pASSTHRU_MSG.TxFlags = (uint)txFlags;
		if (data.Length > 4128)
		{
			throw new IncorrectDataSizeException();
		}
		for (int i = 0; i < data.Length; i++)
		{
			pASSTHRU_MSG.Data[i] = data[i];
		}
		return pASSTHRU_MSG;
	}

	public J2534Message getJ2534Msgs(CAN_FLAGS txFlags)
	{
		PASSTHRU_MSG[] array = new PASSTHRU_MSG[data.Length / 4100 + 1];
		J2534Message j2534Message = new J2534Message(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PASSTHRU_MSG)) * array.Length), array.Length, array.Length + 1);
		int num = 0;
		int num2 = 4;
		while (num2 < data.Length)
		{
			byte[] array2 = new byte[4128];
			int num3 = num;
			array[num3] = new PASSTHRU_MSG();
			array[num3].ProtocolID = protocolType;
			array[num3].TxFlags = (uint)txFlags;
			int num4 = 0;
			for (int i = 0; i < 4; i++)
			{
				array2[i] = data[i];
			}
			for (int j = 4; j < Math.Min(4100, data.Length); j++)
			{
				array2[j] = data[num2++];
				if (num2 == data.Length)
				{
					j++;
					num4 = j + (j - 4) % 8;
					break;
				}
			}
			array[num3].Data = array2;
			array[num3].DataSize = (uint)((data.Length > 4100 && num4 != 0) ? num4 : Math.Min(4100, data.Length));
			j2534Message[num3] = array[num3];
			num++;
		}
		return j2534Message;
	}

	public string getLoggingDataString()
	{
		string text = "";
		for (int i = 5; i < data.Length; i++)
		{
			if (data[i] < 16)
			{
				text += "0";
			}
			text += $"{data[i]:X}";
		}
		return text;
	}

	public override string ToString()
	{
		string text = "";
		for (int i = 0; i < data.Length; i++)
		{
			text = text + "0x" + $"{data[i]:X}" + " ";
		}
		return text;
	}

	public void setProtocolType(PROTOCOL_TYPE protocolType)
	{
		this.protocolType = protocolType;
	}

	public override bool Equals(object obj)
	{
		CANPacket cANPacket = (CANPacket)obj;
		bool flag = true;
		if (cANPacket.data.Length == data.Length)
		{
			for (int i = 0; i < data.Length; i++)
			{
				flag &= cANPacket.data[i] == data[i];
			}
			return flag;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
