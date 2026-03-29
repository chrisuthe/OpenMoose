using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace J2534.Logging;

public class ECULogger
{
	private ToolComm dice;

	private ECUParameters ecuParams;

	private uint diagMsgID;

	public bool hs_Logging = true;

	public bool recs_req;

	public bool dataAvailable { get; set; }

	public ECULogger(ToolComm dice, ECUParameters ecuParams)
	{
		this.dice = dice;
		this.ecuParams = ecuParams;
		diagMsgID = 0u;
		dice.startPeriodicMsg(ECULoggingCommands.msgCANTesterPresent, ref diagMsgID, 1000u, CANChannel.HS);
		Thread.Sleep(1000);
	}

	~ECULogger()
	{
		try
		{
			dice.stopPeriodicMsg(diagMsgID, CANChannel.HS);
		}
		catch (Exception)
		{
		}
	}

	public bool canLog()
	{
		uint msgid = 0u;
		bool result = false;
		dice.startPeriodicMsg(ECULoggingCommands.msgCANTesterPresent, ref msgid, 1000u, CANChannel.HS);
		Thread.Sleep(2000);
		if (dice.sendMsgReadResponse(ECULoggingCommands.msgCEMEngineState, CANChannel.HS).data[9] == 103)
		{
			result = true;
		}
		dice.stopPeriodicMsg(msgid, CANChannel.HS);
		return result;
	}

	private uint getNumMsgs()
	{
		return (uint)Math.Ceiling((double)(ecuParams.getTotalDataLength() - 3) / 7.0) + 1;
	}

	public bool updateRecordData()
	{
		try
		{
			CANPacket cANMsg = new CANPacket(ECULoggingCommands.msgCANRequestRecordSetOnce);
			uint maxNumMsgs = getNumMsgs();
			List<CANPacket> list = dice.sendMsgReadResponse(cANMsg, CANChannel.HS, ref maxNumMsgs, 2000u);
			string text = "";
			foreach (CANPacket item in list)
			{
				text += item.getLoggingDataString();
			}
			text = text.Split(new string[1] { "7AE6F000" }, StringSplitOptions.None)[1];
			foreach (ECUVariable ecuVar in ecuParams.ecuVars)
			{
				try
				{
					if (ecuVar.word)
					{
						string input = text.Substring(0, 4);
						text = text.Substring(4);
						ecuVar.value = ecuVar.getHexValueFromString(input);
					}
					else
					{
						string input2 = text.Substring(0, 2);
						text = text.Substring(2);
						ecuVar.value = ecuVar.getHexValueFromString(input2);
					}
				}
				catch (Exception ex)
				{
					ecuVar.value = 0;
					ex.ToString();
				}
			}
			dataAvailable = true;
		}
		catch (Exception ex2)
		{
			Console.WriteLine(ex2.ToString());
			dataAvailable = false;
		}
		return dataAvailable;
	}

	public static bool checkParamsValid(DateTime expDate)
	{
		return DateTime.Compare(getNetworkTime(), expDate) < 0;
	}

	private static DateTime getNetworkTime()
	{
		byte[] array = new byte[48];
		array[0] = 27;
		IPEndPoint remoteEP = new IPEndPoint(Dns.GetHostEntry("pool.ntp.org").AddressList[0], 123);
		Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		socket.Connect(remoteEP);
		socket.Send(array);
		socket.Receive(array);
		socket.Close();
		ulong num = ((ulong)array[40] << 24) | ((ulong)array[41] << 16) | ((ulong)array[42] << 8) | array[43];
		ulong num2 = ((ulong)array[44] << 24) | ((ulong)array[45] << 16) | ((ulong)array[46] << 8) | array[47];
		ulong num3 = num * 1000 + num2 * 1000 / 4294967296L;
		return new DateTime(1900, 1, 1).AddMilliseconds((long)num3);
	}

	public void sendReqs()
	{
		clearReqs();
		foreach (ECUVariable ecuVar in ecuParams.ecuVars)
		{
			CANPacket cANMsg = new CANPacket(ecuVar.getRequestData());
			bool flag = false;
			while (!flag)
			{
				flag |= dice.sendMsgCheckDiagResponse(cANMsg, CANChannel.HS, 234);
			}
		}
	}

	public bool clearReqs()
	{
		return dice.sendMsgCheckDiagResponse(ECULoggingCommands.msgCANClearRecordReqs, CANChannel.HS, 234);
	}

	public bool VerifyLogList(ref List<CANPacket> cl, uint numMsgs, int index)
	{
		int num = 0;
		foreach (CANPacket item in cl)
		{
			if (index == 16)
			{
				index = 8;
			}
			int num2 = item.data[4] >> 4;
			if (num2 == 4 || num2 == 8)
			{
				if (num2 == 4 && num == numMsgs - 1)
				{
					num++;
					index++;
					continue;
				}
				if (num2 == 8 && num == 0)
				{
					num++;
					index++;
					continue;
				}
			}
			if (item.data[4] != index)
			{
				return false;
			}
			index++;
			num++;
		}
		return true;
	}

	public bool requestRecords(ref string result)
	{
		CANPacket cANMsg = new CANPacket(ECULoggingCommands.msgCANRequestRecordSetOnce);
		uint maxNumMsgs = getNumMsgs();
		uint num = maxNumMsgs;
		uint timeout = 1000u;
		List<CANPacket> cl = dice.sendMsgReadResponse(cANMsg, CANChannel.HS, ref maxNumMsgs, timeout);
		Thread.Sleep(1);
		if (cl.Count != num)
		{
			result = "error";
			return false;
		}
		int index = 8;
		if (!VerifyLogList(ref cl, num, index))
		{
			return false;
		}
		foreach (CANPacket item in cl)
		{
			result += item.getLoggingDataString();
		}
		result = result.Split(new string[1] { "7AE6F000" }, StringSplitOptions.None)[1];
		return true;
	}

	public bool ListenForRecords(ref string result)
	{
		uint maxNumMsgs = getNumMsgs();
		uint numMsgs = getNumMsgs();
		uint timeout = 1000u;
		if (!recs_req)
		{
			CANPacket cANMsg = new CANPacket(ECULoggingCommands.msgCANRequestRecordSetFast);
			dice.sendMsg(cANMsg, CANChannel.HS);
			recs_req = true;
		}
		List<CANPacket> cl = dice.ReadResponse(CANChannel.HS, ref maxNumMsgs, timeout);
		Console.WriteLine("List count: " + cl.Count);
		if (cl.Count != numMsgs)
		{
			result = "error";
			return false;
		}
		int index = 8;
		if (!VerifyLogList(ref cl, numMsgs, index))
		{
			result = "bad list";
			return false;
		}
		foreach (CANPacket item in cl)
		{
			result += item.getLoggingDataString();
		}
		return true;
	}

	public string getDoublePrecision(double dbValue, int nDecimal)
	{
		string text = "0";
		if (nDecimal > 0)
		{
			text += ".";
			for (int i = 0; i < nDecimal; i++)
			{
				text += "0";
			}
		}
		return dbValue.ToString(text);
	}
}
