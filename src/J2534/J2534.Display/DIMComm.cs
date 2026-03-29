using System;
using System.Threading;

namespace J2534.Display;

public class DIMComm
{
	private ToolComm dice;

	public DIMComm(ToolComm dice, bool activateDIMOnInit)
	{
		this.dice = dice;
		if (activateDIMOnInit)
		{
			activateDIM();
		}
	}

	public void activateDIM()
	{
		dice.sendMsg(DIMDisplayCommands.msgActivateDIM, CANChannel.MS);
		Thread.Sleep(30);
		dice.sendMsg(DIMDisplayCommands.msgActivateDIM2, CANChannel.MS);
		Thread.Sleep(30);
		dice.sendMsg(DIMDisplayCommands.msgDisableCursor, CANChannel.MS);
		Thread.Sleep(100);
		dice.sendMsg(DIMDisplayCommands.msgClearScreen, CANChannel.MS);
	}

	public void deactivateDIM()
	{
		dice.sendMsg(DIMDisplayCommands.msgClearScreen, CANChannel.MS);
		Thread.Sleep(30);
		dice.sendMsg(DIMDisplayCommands.msgDeactivateDIM, CANChannel.MS);
		Thread.Sleep(30);
		dice.sendMsg(DIMDisplayCommands.msgDeactivateDIM2, CANChannel.MS);
	}

	public void sendMessage(string msg)
	{
		Thread.Sleep(100);
		dice.sendDIMMsg(DIMDisplayCommands.msgShowMsg(msg, 0), CANChannel.MS);
	}

	public bool resetSRI()
	{
		uint msgid = 0u;
		dice.startPeriodicMsg(DIMDisplayCommands.msgCANTesterPresent, ref msgid, 1000u, CANChannel.MS);
		Thread.Sleep(2000);
		bool result = dice.sendMsgCheckDiagResponse(DIMDisplayCommands.msgCANResetSRI, CANChannel.MS, 242);
		dice.stopPeriodicMsg(msgid, CANChannel.MS);
		return result;
	}

	public void setTime()
	{
		int num = (int)Math.Round(((double)DateTime.Now.Hour + (double)DateTime.Now.Minute / 60.0) * 60.0);
		byte b = (byte)(num >> 8);
		byte b2 = (byte)num;
		CANPacket cANPacket = new CANPacket(DIMDisplayCommands.msgCANSetTime);
		cANPacket.setTimeData(new byte[2] { b, b2 });
		dice.sendMsgCheckDiagResponse(cANPacket, CANChannel.MS, 240);
	}
}
