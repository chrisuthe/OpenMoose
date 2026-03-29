using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace J2534.Flash.ECU;

public class ECUProgrammer : ModuleProgrammer
{
	private bool flash512;

	public ECUProgrammer(ToolComm dice, byte[] sbl, byte[] binFile, bool flash512, bool p80)
		: base(p80)
	{
		base.dice = dice;
		base.sbl = sbl;
		base.binFile = binFile;
		this.flash512 = flash512;
	}

	public byte[] readECU(bool readRAM)
	{
		byte[] array = null;
		try
		{
			int num = 0;
			if (!readRAM)
			{
				array = ((!flash512) ? new byte[1048576] : new byte[524288]);
			}
			else
			{
				array = new byte[65536];
				num = 3145728;
			}
			int num2 = 1;
			if (!readRAM)
			{
				CANPacket cANPacket = new CANPacket(new byte[8] { 122, 188, 0, 15, 255, 0, 0, 0 });
				byte[] array2 = new byte[6];
				byte[] arr = new byte[6] { 79, 79, 82, 69, 65, 68 };
				List<CANPacket> list;
				do
				{
					uint maxNumMsgs = 1u;
					uint timeout = 1000u;
					list = dice.sendMsgReadResponse(cANPacket, CANChannel.HS, ref maxNumMsgs, timeout);
					num2++;
					if (num2 == 5)
					{
						throw new MessageNotReceivedException(cANPacket);
					}
				}
				while (list == null);
				int num3 = 0;
				foreach (CANPacket item in list)
				{
					for (int i = 6; i < item.data.Length; i++)
					{
						if (num3 < array.Length)
						{
							array2[num3++] = item.data[i];
						}
					}
				}
				if (checkArrayEq(array2, arr))
				{
					return new byte[4] { 68, 69, 78, 89 };
				}
			}
			try
			{
				int num4 = num;
				while (num4 < array.Length + num)
				{
					byte b = (byte)(num4 >> 16);
					byte b2 = (byte)(num4 >> 8);
					byte b3 = (byte)num4;
					CANPacket cANPacket2 = new CANPacket(new byte[8] { 122, 188, 0, b, b2, b3, 0, 0 });
					num2 = 1;
					List<CANPacket> list2;
					do
					{
						uint maxNumMsgs2 = 1u;
						uint timeout2 = 1000u;
						list2 = dice.sendMsgReadResponse(cANPacket2, CANChannel.HS, ref maxNumMsgs2, timeout2);
						num2++;
						if (num2 == 5)
						{
							throw new MessageNotReceivedException(cANPacket2);
						}
					}
					while (list2 == null);
					foreach (CANPacket item2 in list2)
					{
						for (int j = 6; j < item2.data.Length; j++)
						{
							if (num4 - num < array.Length)
							{
								array[num4++ - num] = item2.data[j];
							}
						}
					}
				}
			}
			catch (MessageNotReceivedException ex)
			{
				sendReset();
				Console.WriteLine(ex.ToString());
				return null;
			}
			catch (Exception ex2)
			{
				sendReset();
				Console.WriteLine(ex2.ToString());
				return null;
			}
		}
		catch (Exception ex3)
		{
			sendReset();
			Console.WriteLine(ex3.ToString());
		}
		finally
		{
			if (readRAM)
			{
				lock (new object())
				{
					base.doneFlashing = true;
				}
			}
		}
		return array;
	}

	public static bool checkArrayEq(byte[] arr1, byte[] arr2)
	{
		int num = ((arr1.Length > arr2.Length) ? arr2.Length : arr1.Length);
		bool flag = true;
		for (int i = 0; i < num; i++)
		{
			flag &= arr1[i] == arr2[i];
		}
		return flag;
	}

	public void recodeECU(int unlockPin, int pinCode, long securityKey)
	{
		uint msgid = 0u;
		dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid, 1000u, CANChannel.HS);
		Thread.Sleep(2000);
		writeEEPROMPin(unlockPin, pinCode, securityKey);
		dice.stopPeriodicMsg(msgid, CANChannel.HS);
	}

	public void recodeECU(int unlockPin, long securityKey)
	{
		int num = 0;
		for (int i = 0; i < 24; i += 4)
		{
			num += (byte)((securityKey >> i + 16) & 0xF) << 20 - i;
		}
		uint msgid = 0u;
		dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid, 1000u, CANChannel.HS);
		Thread.Sleep(2000);
		writeEEPROMPin(unlockPin, num, securityKey);
		dice.stopPeriodicMsg(msgid, CANChannel.HS);
	}

	public override bool clearFaultCodes()
	{
		uint msgid = 0u;
		dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid, 1000u, CANChannel.HS);
		uint msgid2 = 0u;
		if (!p80)
		{
			dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid2, 1000u, CANChannel.MS);
		}
		Thread.Sleep(2000);
		bool flag = true;
		byte[] hsModuleAddresses = VolvoECUCommands.hsModuleAddresses;
		foreach (byte diagModuleAddress in hsModuleAddresses)
		{
			CANPacket cANPacket = new CANPacket(ModuleProgrammer.msgCANReadFaultCodes);
			cANPacket.setDiagModuleAddress(diagModuleAddress);
			dice.sendMsg(cANPacket, CANChannel.HS);
			Thread.Sleep(250);
			cANPacket = new CANPacket(ModuleProgrammer.msgCANClearFaultCodes);
			flag &= dice.sendMsgCheckDiagResponse(cANPacket, CANChannel.HS, 239);
		}
		if (!p80)
		{
			hsModuleAddresses = VolvoECUCommands.msModuleAddresses;
			foreach (byte diagModuleAddress2 in hsModuleAddresses)
			{
				CANPacket cANPacket2 = new CANPacket(ModuleProgrammer.msgCANReadFaultCodes);
				cANPacket2.setDiagModuleAddress(diagModuleAddress2);
				dice.sendMsg(cANPacket2, CANChannel.MS);
				Thread.Sleep(300);
				cANPacket2 = new CANPacket(ModuleProgrammer.msgCANClearFaultCodes);
				flag &= dice.sendMsgCheckDiagResponse(cANPacket2, CANChannel.MS, 239);
			}
		}
		if (!p80)
		{
			dice.stopPeriodicMsg(msgid2, CANChannel.MS);
		}
		dice.stopPeriodicMsg(msgid, CANChannel.HS);
		return flag;
	}

	public bool canFlash()
	{
		uint msgid = 0u;
		bool result = false;
		dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid, 1000u, CANChannel.HS);
		Thread.Sleep(2000);
		if (dice.sendMsgReadResponse(VolvoECUCommands.msgCEMEngineState, CANChannel.HS).data[9] == 102)
		{
			result = true;
		}
		dice.stopPeriodicMsg(msgid, CANChannel.HS);
		return result;
	}

	public int readCANConfigNumber()
	{
		uint msgid = 0u;
		uint numMsgs = 4u;
		dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid, 1000u, CANChannel.HS);
		Thread.Sleep(2000);
		List<CANPacket> list = dice.sendMsgReadResponse(VolvoECUCommands.msgCANReadECMConfig, CANChannel.HS, ref numMsgs);
		dice.stopPeriodicMsg(msgid, CANChannel.HS);
		byte[] array = new byte[28];
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = 5; j < 12; j++)
			{
				array[j - 5 + i * 7] = list[i].data[j];
			}
		}
		byte[] array2 = new byte[4];
		for (int k = 4; k < 8; k++)
		{
			array2[3 - (k - 4)] = array[k];
		}
		return BitConverter.ToInt32(array2, 0);
	}

	public int readSoftwareNumber()
	{
		uint msgid = 0u;
		uint numMsgs = 4u;
		dice.startPeriodicMsg(ModuleProgrammer.msgCANTesterPresent, ref msgid, 1000u, CANChannel.HS);
		Thread.Sleep(2000);
		List<CANPacket> list = dice.sendMsgReadResponse(VolvoECUCommands.msgCANReadECMConfig, CANChannel.HS, ref numMsgs);
		dice.stopPeriodicMsg(msgid, CANChannel.HS);
		byte[] array = new byte[28];
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = 5; j < 12; j++)
			{
				array[j - 5 + i * 7] = list[i].data[j];
			}
		}
		byte[] array2 = new byte[4];
		for (int k = 16; k < 20; k++)
		{
			array2[3 - (k - 16)] = array[k];
		}
		return BitConverter.ToInt32(array2, 0);
	}

	public bool checkBINCompatible()
	{
		readCANConfigNumber();
		return false;
	}

	protected override void flashModule()
	{
		try
		{
			sendModuleReset();
			sendSilence();
			startPBL();
			startSBL(sbl, a0Response: true);
			sendData(binFile);
			sendReset();
			if (!p80)
			{
				Thread.Sleep(2000);
				setDIMTime();
			}
		}
		catch (Exception ex)
		{
			sendReset();
			Console.WriteLine(ex.ToString());
		}
		finally
		{
			lock (new object())
			{
				base.doneFlashing = true;
			}
		}
	}

	private void writeEEPROMPin(int unlockPin, int immoPin, long cemPsk)
	{
		CANPacket cANPacket = new CANPacket(VolvoECUCommands.msgCANSendPin);
		byte[] bytes = BitConverter.GetBytes(unlockPin);
		Array.Reverse(bytes, 0, 3);
		cANPacket.setDiagData(bytes);
		if (unlockPin == 0)
		{
			dice.sendMsg(cANPacket, CANChannel.HS);
		}
		else if (!dice.sendMsgCheckDiagResponse(cANPacket, CANChannel.HS, 227))
		{
			Console.WriteLine("Pin unlock failed");
		}
		Thread.Sleep(500);
		cANPacket = new CANPacket(VolvoECUCommands.msgCANWritePin);
		bytes = BitConverter.GetBytes(immoPin);
		Array.Reverse(bytes, 0, 3);
		cANPacket.setDiagData(bytes);
		if (!dice.sendMsgCheckDiagResponse(cANPacket, CANChannel.HS, 248))
		{
			Console.WriteLine("New pin write failed");
		}
		Thread.Sleep(2000);
		cANPacket = new CANPacket(VolvoECUCommands.msgCANWriteKey);
		bytes = BitConverter.GetBytes(cemPsk);
		Array.Reverse(bytes, 0, 6);
		cANPacket.setDiagData(new byte[4]
		{
			bytes[0],
			bytes[1],
			bytes[2],
			bytes[3]
		});
		dice.sendMsg(cANPacket, CANChannel.HS);
		cANPacket = new CANPacket(VolvoECUCommands.msgCANBlank);
		cANPacket.data[4] = 74;
		cANPacket.data[5] = bytes[4];
		cANPacket.data[6] = bytes[5];
		if (!dice.sendMsgCheckDiagResponse(cANPacket, CANChannel.HS, 248))
		{
			Console.WriteLine("New key write failed");
		}
		Thread.Sleep(100);
		cANPacket = new CANPacket(VolvoECUCommands.msgCANRelockImmo);
		if (!dice.sendMsgCheckDiagResponse(cANPacket, CANChannel.HS, 227))
		{
			Console.WriteLine("Relock request failed");
		}
	}

	private List<CANPacket> createSBLList(byte[] sblArr)
	{
		List<CANPacket> list = new List<CANPacket>();
		for (int i = 0; i < sblArr.Length; i += 6)
		{
			CANPacket cANPacket = new CANPacket(VolvoECUCommands.msgCANSendDataPrefix);
			byte[] array = new byte[6];
			int num = sblArr.Length - i;
			for (int j = 0; j < 6; j++)
			{
				array[j] = (byte)((num >= j + 1) ? sblArr[i + j] : 0);
			}
			cANPacket.setMsgData(array);
			list.Add(cANPacket);
		}
		return list;
	}

	private List<CANPacket> createData8kList(byte[] binFile)
	{
		List<CANPacket> list = new List<CANPacket>();
		for (int i = 32768; i < 57344; i += 6)
		{
			CANPacket cANPacket = new CANPacket(VolvoECUCommands.msgCANSendDataPrefix);
			byte[] array = new byte[6];
			int num = binFile.Length - i;
			for (int j = 0; j < 6; j++)
			{
				array[j] = (byte)((num >= j + 1) ? binFile[i + j] : 0);
			}
			cANPacket.setMsgData(array);
			list.Add(cANPacket);
		}
		return list;
	}

	private List<CANPacket> createData10kList(byte[] binFile)
	{
		List<CANPacket> list = new List<CANPacket>();
		uint num = 0u;
		num = ((!flash512) ? 1048576u : 524288u);
		for (int i = 65536; i < num; i += 6)
		{
			CANPacket cANPacket = new CANPacket(VolvoECUCommands.msgCANSendDataPrefix);
			byte[] array = new byte[6];
			int num2 = binFile.Length - i;
			for (int j = 0; j < 6; j++)
			{
				array[j] = (byte)((num2 >= j + 1) ? binFile[i + j] : 0);
			}
			cANPacket.setMsgData(array);
			list.Add(cANPacket);
		}
		return list;
	}

	public override void sendModuleReset()
	{
		dice.sendMsg(VolvoECUCommands.msgCANECUReset, CANChannel.HS);
	}

	public override void startPBL()
	{
		if (!dice.sendMsgCheckResponse(VolvoECUCommands.msgCANStartPBL, CANChannel.HS, 198))
		{
			throw new MessageNotReceivedException();
		}
	}

	public override void startSBL(byte[] sblArr, bool a0Response)
	{
		List<CANPacket> qMsg = createSBLList(sblArr);
		sendJumpToSegment(3260416);
		dice.sendMsg(qMsg, CANChannel.HS);
		if (a0Response)
		{
			sendEndData();
		}
		sendJumpToSegment(3260416);
		if (a0Response)
		{
			sendCommitData(3260416 + sblArr.Length);
			sendJumpToSegment(3260416);
		}
		if (a0Response)
		{
			if (!dice.sendMsgCheckResponse(VolvoECUCommands.msgCANJumpToStart, CANChannel.HS, 160))
			{
				throw new MessageNotReceivedException();
			}
		}
		else
		{
			dice.sendMsg(VolvoECUCommands.msgCANJumpToStart, CANChannel.HS);
		}
	}

	protected override void eraseFlash()
	{
		sendJumpToSegment(32768);
		sendEraseSegment();
		Thread.Sleep(3000);
		sendJumpToSegment(65536);
		sendEraseSegment();
	}

	protected override void sendData(byte[] binFile)
	{
		bool flag = true;
		do
		{
			eraseFlash();
			byte b = 0;
			List<CANPacket> qMsg = createData8kList(binFile);
			sendJumpToSegment(32768);
			dice.sendMsg(qMsg, CANChannel.HS);
			sendJumpToSegment(32768);
			b = getFlashBlockChecksum(32768, 57344);
			flag &= sendCommitData(57344, b);
			List<CANPacket> qMsg2 = createData10kList(binFile);
			if (!flash512)
			{
				sendJumpToSegment(65536);
				dice.sendMsg(qMsg2, CANChannel.HS);
				sendJumpToSegment(65536);
				b = getFlashBlockChecksum(65536, 1048576);
				flag &= sendCommitData(1048576, b);
			}
			else
			{
				sendJumpToSegment(65536);
				dice.sendMsg(qMsg2, CANChannel.HS);
			}
			if (!flag)
			{
				DialogResult dialogResult = MessageBox.Show("ECM did not flash correctly. Click ok to try again...", "Flash Fail", MessageBoxButtons.RetryCancel, MessageBoxIcon.Hand);
				if (dialogResult == DialogResult.Abort)
				{
					break;
				}
				_ = 4;
			}
		}
		while (!flag);
	}

	private void sendJumpToSegment(int addr)
	{
		CANPacket cANPacket = new CANPacket(VolvoECUCommands.msgCANChooseSegmentPrefix);
		cANPacket.setAddr(addr);
		int num = 0;
		bool flag = false;
		do
		{
			flag = dice.sendMsgCheckResponse(cANPacket, CANChannel.HS, 156);
			num++;
			if (!flag)
			{
				Thread.Sleep(1000);
			}
			if (num == 10)
			{
				throw new MessageNotReceivedException();
			}
		}
		while (!flag);
	}

	private void sendEndData()
	{
		dice.sendMsg(VolvoECUCommands.msgCANEndData, CANChannel.HS);
	}

	private void sendEndData(bool waitForResponse)
	{
		if (!dice.sendMsgCheckResponse(VolvoECUCommands.msgCANEndData, CANChannel.HS, 169))
		{
			throw new MessageNotReceivedException();
		}
	}

	private void sendCommitData(int addr)
	{
		CANPacket cANPacket = new CANPacket(VolvoECUCommands.msgCANCommitSegmentPrefix);
		cANPacket.setAddr(addr);
		if (!dice.sendMsgCheckResponse(cANPacket, CANChannel.HS, 177))
		{
			throw new MessageNotReceivedException();
		}
	}

	private bool sendCommitData(int addr, byte chkSum)
	{
		CANPacket cANPacket = new CANPacket(VolvoECUCommands.msgCANCommitSegmentPrefix);
		cANPacket.setAddr(addr);
		uint maxNumMsgs = 1u;
		List<CANPacket> list = dice.sendMsgReadResponse(cANPacket, CANChannel.HS, ref maxNumMsgs, 9000u);
		if (list.Count > 0)
		{
			foreach (CANPacket item in list)
			{
				if (item.data[5] == 177 && item.data[6] == chkSum)
				{
					return true;
				}
			}
		}
		return false;
	}

	private byte getFlashBlockChecksum(int startAddr, int endAddr)
	{
		uint num = 0u;
		for (int i = startAddr; i < endAddr; i++)
		{
			num += binFile[i];
		}
		do
		{
			num = ((num >> 24) & 0xFF) + ((num >> 16) & 0xFF) + ((num >> 8) & 0xFF) + (num & 0xFF);
		}
		while (((num >> 8) & 0xFFFFFF) != 0);
		return (byte)num;
	}

	private void sendEraseSegment()
	{
		if (!dice.sendMsgCheckResponse(VolvoECUCommands.msgCANEraseSegment, CANChannel.HS, 249))
		{
			throw new MessageNotReceivedException();
		}
	}
}
