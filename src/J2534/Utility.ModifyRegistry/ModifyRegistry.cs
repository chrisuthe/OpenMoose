using System;
using Microsoft.Win32;

namespace Utility.ModifyRegistry;

public class ModifyRegistry
{
	private bool showError;

	private string subKey = "SOFTWARE\\x";

	private RegistryKey baseRegistryKey = Registry.LocalMachine;

	public bool ShowError
	{
		get
		{
			return showError;
		}
		set
		{
			showError = value;
		}
	}

	public string SubKey
	{
		get
		{
			return subKey;
		}
		set
		{
			subKey = value;
		}
	}

	public RegistryKey BaseRegistryKey
	{
		get
		{
			return baseRegistryKey;
		}
		set
		{
			baseRegistryKey = value;
		}
	}

	public string Read(string KeyName)
	{
		RegistryKey registryKey = baseRegistryKey.OpenSubKey(subKey);
		if (registryKey == null)
		{
			return null;
		}
		try
		{
			return (string)registryKey.GetValue(KeyName.ToUpper());
		}
		catch (Exception e)
		{
			ShowErrorMessage(e, "Reading registry " + KeyName.ToUpper());
			return null;
		}
	}

	public byte[] ReadByte(string KeyName)
	{
		RegistryKey registryKey = baseRegistryKey.OpenSubKey(subKey);
		if (registryKey == null)
		{
			return null;
		}
		try
		{
			return (byte[])registryKey.GetValue(KeyName.ToUpper());
		}
		catch (Exception e)
		{
			ShowErrorMessage(e, "Reading registry " + KeyName.ToUpper());
			return null;
		}
	}

	public bool Write(string KeyName, object Value)
	{
		try
		{
			baseRegistryKey.CreateSubKey(subKey).SetValue(KeyName.ToUpper(), Value);
			return true;
		}
		catch (Exception e)
		{
			ShowErrorMessage(e, "Writing registry " + KeyName.ToUpper());
			return false;
		}
	}

	public bool Write(string KeyName, object Value, RegistryValueKind k)
	{
		try
		{
			baseRegistryKey.CreateSubKey(subKey).SetValue(KeyName.ToUpper(), Value, k);
			return true;
		}
		catch (Exception e)
		{
			ShowErrorMessage(e, "Writing registry " + KeyName.ToUpper());
			return false;
		}
	}

	public bool DeleteKey(string KeyName)
	{
		try
		{
			RegistryKey registryKey = baseRegistryKey.CreateSubKey(subKey);
			if (registryKey == null)
			{
				return true;
			}
			registryKey.DeleteValue(KeyName);
			return true;
		}
		catch (Exception e)
		{
			ShowErrorMessage(e, "Deleting SubKey " + subKey);
			return false;
		}
	}

	public bool DeleteSubKeyTree()
	{
		try
		{
			RegistryKey registryKey = baseRegistryKey;
			if (registryKey.OpenSubKey(subKey) != null)
			{
				registryKey.DeleteSubKeyTree(subKey);
			}
			return true;
		}
		catch (Exception e)
		{
			ShowErrorMessage(e, "Deleting SubKey " + subKey);
			return false;
		}
	}

	public int SubKeyCount()
	{
		try
		{
			return baseRegistryKey.OpenSubKey(subKey)?.SubKeyCount ?? 0;
		}
		catch (Exception e)
		{
			ShowErrorMessage(e, "Retriving subkeys of " + subKey);
			return 0;
		}
	}

	public int ValueCount()
	{
		try
		{
			return baseRegistryKey.OpenSubKey(subKey)?.ValueCount ?? 0;
		}
		catch (Exception e)
		{
			ShowErrorMessage(e, "Retriving keys of " + subKey);
			return 0;
		}
	}

	private void ShowErrorMessage(Exception e, string Title)
	{
		if (showError)
		{
			Console.WriteLine(e.Message);
		}
	}
}
