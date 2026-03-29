using System;

namespace J2534.Flash;

public class MessageNotReceivedException : Exception
{
	public CANPacket p;

	public MessageNotReceivedException()
	{
	}

	public MessageNotReceivedException(CANPacket p)
	{
		this.p = p;
	}
}
