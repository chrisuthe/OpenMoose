using System;
using System.Collections.Generic;
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
		int totalLen = ecuParams.getTotalDataLength();
		if (totalLen <= 3)
			return 1;
		return (uint)Math.Ceiling((double)(totalLen - 3) / 7.0) + 1;
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

			string[] parts = text.Split(new[] { "7AE6F000" }, StringSplitOptions.None);
			if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
			{
				dataAvailable = false;
				return false;
			}
			text = parts[1];

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
				catch (Exception)
				{
					ecuVar.value = 0;
				}
			}
			dataAvailable = true;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			dataAvailable = false;
		}
		return dataAvailable;
	}

	/// <summary>
	/// Registers all ECU variables for logging. Retries each variable up to 10 times.
	/// Throws InvalidOperationException if the ECU does not acknowledge a variable.
	/// </summary>
	public void sendReqs()
	{
		clearReqs();
		foreach (ECUVariable ecuVar in ecuParams.ecuVars)
		{
			CANPacket cANMsg = new CANPacket(ecuVar.getRequestData());
			bool ack = false;
			for (int attempt = 0; attempt < 10 && !ack; attempt++)
			{
				ack = dice.sendMsgCheckDiagResponse(cANMsg, CANChannel.HS, 234);
			}
			if (!ack)
				throw new InvalidOperationException(
					$"ECU did not acknowledge registration of variable '{ecuVar.name}' after 10 attempts.");
		}
	}

	/// <summary>
	/// Clears all registered logging variables. Retries up to 5 times.
	/// </summary>
	public bool clearReqs()
	{
		for (int attempt = 0; attempt < 5; attempt++)
		{
			if (dice.sendMsgCheckDiagResponse(ECULoggingCommands.msgCANClearRecordReqs, CANChannel.HS, 234))
				return true;
		}
		return false;
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

	/// <summary>
	/// Sends a single data request and parses the multi-frame response.
	/// Returns true if data was successfully received, with the hex payload in result.
	/// </summary>
	public bool requestRecords(ref string result)
	{
		CANPacket cANMsg = new CANPacket(ECULoggingCommands.msgCANRequestRecordSetOnce);
		uint maxNumMsgs = getNumMsgs();
		uint expectedMsgs = maxNumMsgs;
		uint timeout = 1000u;
		List<CANPacket> cl = dice.sendMsgReadResponse(cANMsg, CANChannel.HS, ref maxNumMsgs, timeout);

		if (cl.Count != expectedMsgs)
		{
			result = "";
			return false;
		}

		if (!VerifyLogList(ref cl, expectedMsgs, 8))
			return false;

		string raw = "";
		foreach (CANPacket item in cl)
			raw += item.getLoggingDataString();

		string[] parts = raw.Split(new[] { "7AE6F000" }, StringSplitOptions.None);
		if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
			return false;

		result = parts[1];
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
		if (cl.Count != numMsgs)
		{
			result = "";
			return false;
		}
		if (!VerifyLogList(ref cl, numMsgs, 8))
		{
			result = "";
			return false;
		}
		string raw = "";
		foreach (CANPacket item in cl)
			raw += item.getLoggingDataString();

		string[] parts = raw.Split(new[] { "7AE6F000" }, StringSplitOptions.None);
		if (parts.Length < 2 || string.IsNullOrEmpty(parts[1]))
			return false;

		result = parts[1];
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
