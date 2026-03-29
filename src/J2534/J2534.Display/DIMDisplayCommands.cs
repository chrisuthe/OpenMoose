using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace J2534.Display;

public class DIMDisplayCommands
{
	public static readonly int PHM_ID = 44049422;

	public static readonly int PHM_ID_SERIAL = 25165832;

	public static readonly CANPacket msgActivateDIM = new CANPacket(new byte[8] { 0, 0, 0, 0, 0, 0, 0, 5 }, PHM_ID);

	public static readonly CANPacket msgActivateDIM2 = new CANPacket(new byte[8] { 0, 0, 0, 0, 0, 0, 0, 1 }, PHM_ID);

	public static readonly CANPacket msgDeactivateDIM = new CANPacket(new byte[8] { 64, 0, 0, 0, 0, 32, 0, 100 }, PHM_ID);

	public static readonly CANPacket msgDeactivateDIM2 = new CANPacket(new byte[8] { 64, 0, 0, 0, 0, 0, 0, 0 }, PHM_ID);

	public static readonly CANPacket msgClearScreen = new CANPacket(new byte[8] { 225, 255, 0, 0, 0, 0, 0, 0 }, PHM_ID_SERIAL);

	public static readonly CANPacket msgDisableCursor = new CANPacket(new byte[8] { 225, 254, 0, 0, 0, 0, 0, 0 }, PHM_ID_SERIAL);

	public static readonly CANPacket msgCANSetTime = new CANPacket(new byte[8] { 207, 81, 176, 7, 1, 255, 0, 0 });

	public static readonly CANPacket msgCANResetSRI = new CANPacket(new byte[8] { 203, 81, 178, 1, 1, 0, 0, 0 });

	public static readonly CANPacket msgCANTesterPresent = new CANPacket(new byte[8] { 216, 0, 0, 0, 0, 0, 0, 0 });

	public static CANPacket msgSetCursorPosition(byte cursorPosition)
	{
		return new CANPacket(new byte[8] { 225, cursorPosition, 0, 0, 0, 0, 0, 0 }, PHM_ID_SERIAL);
	}

	public static List<CANPacket> msgShowMsg(string msg, byte cursorPosition)
	{
		int length = msg.Length;
		byte b = 0;
		List<CANPacket> list = new List<CANPacket>();
		if (length <= 6)
		{
			byte[] array = new byte[8];
			byte[] array2 = new byte[2]
			{
				(byte)(225 + length),
				cursorPosition
			}.Concat(Encoding.ASCII.GetBytes(msg)).ToArray();
			Array.Copy(array2, array, array2.Length);
			list.Add(new CANPacket(array, PHM_ID_SERIAL));
		}
		else
		{
			Stack<byte> stack = new Stack<byte>(Encoding.ASCII.GetBytes(msg).Reverse());
			byte[] array3 = new byte[8] { 167, cursorPosition, 0, 0, 0, 0, 0, 0 };
			for (int i = 2; i < 8; i++)
			{
				array3[i] = stack.Pop();
			}
			list.Add(new CANPacket(array3, PHM_ID_SERIAL));
			b++;
			int num = 6;
			while (stack.Count > 0)
			{
				if (length - num <= 7)
				{
					array3[0] = (byte)(96 + length - num);
				}
				else
				{
					array3[0] = (byte)(32 + b);
				}
				for (int j = 1; j < 8; j++)
				{
					if (stack.Count > 0)
					{
						array3[j] = stack.Pop();
						num++;
					}
					else
					{
						array3[j] = 0;
					}
				}
				list.Add(new CANPacket(array3, PHM_ID_SERIAL));
				b++;
			}
		}
		return list;
	}
}
