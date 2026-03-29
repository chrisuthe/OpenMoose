using System.Collections.Generic;
using Microsoft.Win32;

namespace J2534;

public static class J2534Detect
{
	private const string PASSTHRU_REGISTRY_PATH = "Software\\PassThruSupport.04.04";

	private const string PASSTHRU_REGISTRY_PATH_6432 = "Software\\Wow6432Node\\PassThruSupport.04.04";

	public static List<J2534Device> ListDevices()
	{
		List<J2534Device> list = new List<J2534Device>();
		RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("Software\\PassThruSupport.04.04", writable: false);
		if (registryKey == null)
		{
			registryKey = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\PassThruSupport.04.04", writable: false);
			if (registryKey == null)
			{
				return list;
			}
		}
		string[] subKeyNames = registryKey.GetSubKeyNames();
		foreach (string name in subKeyNames)
		{
			J2534Device j2534Device = new J2534Device();
			RegistryKey registryKey2 = registryKey.OpenSubKey(name);
			if (registryKey2 != null)
			{
				j2534Device.Vendor = (string)registryKey2.GetValue("Vendor", "");
				j2534Device.Name = (string)registryKey2.GetValue("Name", "");
				j2534Device.ConfigApplication = (string)registryKey2.GetValue("ConfigApplication", "");
				j2534Device.FunctionLibrary = (string)registryKey2.GetValue("FunctionLibrary", "");
				j2534Device.CANChannels = (int)registryKey2.GetValue("CAN", 0);
				j2534Device.ISO15765Channels = (int)registryKey2.GetValue("ISO15765", 0);
				j2534Device.J1850PWMChannels = (int)registryKey2.GetValue("J1850PWM", 0);
				j2534Device.J1850VPWChannels = (int)registryKey2.GetValue("J1850VPW", 0);
				j2534Device.ISO9141Channels = (int)registryKey2.GetValue("ISO9141", 0);
				j2534Device.ISO14230Channels = (int)registryKey2.GetValue("ISO14230", 0);
				j2534Device.SCI_A_ENGINEChannels = (int)registryKey2.GetValue("SCI_A_ENGINE", 0);
				j2534Device.SCI_A_TRANSChannels = (int)registryKey2.GetValue("SCI_A_TRANS", 0);
				j2534Device.SCI_B_ENGINEChannels = (int)registryKey2.GetValue("SCI_B_ENGINE", 0);
				j2534Device.SCI_B_TRANSChannels = (int)registryKey2.GetValue("SCI_B_TRANS", 0);
				list.Add(j2534Device);
			}
		}
		return list;
	}
}
