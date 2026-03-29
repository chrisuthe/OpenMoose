using System;

namespace J2534;

public class CommToolException : VehComException
{
	public CommToolException(string message)
		: base(message)
	{
	}

	public static bool ContainsCommToolException(Exception ex)
	{
		while (ex != null)
		{
			if (ex is CommToolException)
			{
				return true;
			}
			ex = ex.InnerException;
		}
		return false;
	}
}
