using System;
using System.Collections.Generic;
using System.Threading;
using J2534.Display;

namespace J2534.Flash;

public abstract class ModuleProgrammer
{
	protected ToolComm dice;

	protected byte[] sbl;

	protected byte[] binFile;

	protected bool p80;

	private static readonly CANPacket msgCANSilence = new CANPacket(new byte[8] { 255, 134, 0, 0, 0, 0, 0, 0 });

	private static readonly CANPacket msgCANReset = new CANPacket(new byte[8] { 255, 200, 0, 0, 0, 0, 0, 0 });

	public static readonly CANPacket msgCANTesterPresent = new CANPacket(new byte[8] { 216, 0, 0, 0, 0, 0, 0, 0 });

	public static readonly CANPacket msgCANReadCEMConfig = new CANPacket(new byte[8] { 203, 80, 185, 251, 0, 0, 0, 0 });

	public static readonly CANPacket msgCANReadFaultCodes = new CANPacket(new byte[8] { 203, 0, 174, 17, 0, 0, 0, 0 });

	public static readonly CANPacket msgCANClearFaultCodes = new CANPacket(new byte[8] { 203, 0, 175, 17, 0, 0, 0, 0 });

	public static readonly CANPacket msgCANCrashBentSBL = new CANPacket(new byte[8] { 122, 156, 0, 0, 1, 0, 0, 0 });

	public static readonly CANPacket msgCANSetTime = new CANPacket(new byte[8] { 207, 81, 176, 7, 1, 255, 0, 0 });

	public int progress { get; set; }

	public bool doneFlashing { get; protected set; }

	public ModuleProgrammer(bool p80)
	{
		this.p80 = p80;
	}

	public abstract bool clearFaultCodes();

	public void startFlash()
	{
		startThread();
	}

	protected abstract void flashModule();

	public abstract void sendModuleReset();

	public abstract void startPBL();

	public abstract void startSBL(byte[] sbl, bool a0Response);

	protected abstract void eraseFlash();

	protected abstract void sendData(byte[] binFile);

	protected List<long> getIndex(byte[] value, byte[] pattern)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (pattern == null)
		{
			throw new ArgumentNullException("pattern");
		}
		long num = value.LongLength;
		long num2 = pattern.LongLength;
		if (num == 0L || num2 == 0L || num2 > num)
		{
			return new List<long>();
		}
		long[] array = new long[256];
		for (long num3 = 0L; num3 < 256; num3++)
		{
			array[num3] = num2;
		}
		long num4 = num2 - 1;
		for (long num5 = 0L; num5 < num4; num5++)
		{
			array[pattern[num5]] = num4 - num5;
		}
		long num6 = 0L;
		List<long> list = new List<long>();
		for (; num6 <= num - num2; num6 += array[value[num6 + num4]])
		{
			long num7 = num4;
			while (value[num6 + num7] == pattern[num7])
			{
				if (num7 == 0L)
				{
					list.Add(num6);
					break;
				}
				num7--;
			}
		}
		return list;
	}

	public void sendReset()
	{
		dice.sendMsg(msgCANReset, CANChannel.HS);
		if (!p80)
		{
			dice.sendMsg(msgCANReset, CANChannel.MS);
		}
	}

	public void sendSilence()
	{
		uint msgid = 0u;
		uint msgid2 = 0u;
		dice.startPeriodicMsg(msgCANSilence, ref msgid, 5u, CANChannel.HS);
		if (!p80)
		{
			dice.startPeriodicMsg(msgCANSilence, ref msgid2, 5u, CANChannel.MS);
		}
		Thread.Sleep(3000);
		if (!p80)
		{
			dice.stopPeriodicMsg(msgid2, CANChannel.MS);
		}
		dice.stopPeriodicMsg(msgid, CANChannel.HS);
	}

	public void setDIMTime()
	{
		if (!p80)
		{
			new DIMComm(dice, activateDIMOnInit: false).setTime();
		}
	}

	protected Thread startThread()
	{
		Thread thread = new Thread(flashModule);
		thread.Start();
		return thread;
	}
}
