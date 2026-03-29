using System;

namespace J2534;

public class VehComException : ApplicationException
{
	public VehComException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	public VehComException(string message)
		: base(message)
	{
	}

	public VehComException()
	{
	}
}
