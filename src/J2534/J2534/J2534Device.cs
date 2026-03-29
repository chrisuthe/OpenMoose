namespace J2534;

public class J2534Device
{
	public string Vendor { get; set; }

	public string Name { get; set; }

	public string FunctionLibrary { get; set; }

	public string ConfigApplication { get; set; }

	public int CANChannels { get; set; }

	public int ISO15765Channels { get; set; }

	public int J1850PWMChannels { get; set; }

	public int J1850VPWChannels { get; set; }

	public int ISO9141Channels { get; set; }

	public int ISO14230Channels { get; set; }

	public int SCI_A_ENGINEChannels { get; set; }

	public int SCI_A_TRANSChannels { get; set; }

	public int SCI_B_ENGINEChannels { get; set; }

	public int SCI_B_TRANSChannels { get; set; }

	public bool IsCANSupported
	{
		get
		{
			if (CANChannels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsISO15765Supported
	{
		get
		{
			if (ISO15765Channels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsJ1850PWMSupported
	{
		get
		{
			if (J1850PWMChannels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsJ1850VPWSupported
	{
		get
		{
			if (J1850VPWChannels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsISO9141Supported
	{
		get
		{
			if (ISO9141Channels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsISO14230Supported
	{
		get
		{
			if (ISO14230Channels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsSCI_A_ENGINESupported
	{
		get
		{
			if (SCI_A_ENGINEChannels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsSCI_A_TRANSSupported
	{
		get
		{
			if (SCI_A_TRANSChannels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsSCI_B_ENGINESupported
	{
		get
		{
			if (SCI_B_ENGINEChannels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsSCI_B_TRANSSupported
	{
		get
		{
			if (SCI_B_TRANSChannels <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public override string ToString()
	{
		return Name;
	}
}
