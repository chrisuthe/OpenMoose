using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace J2534.Logging;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
[DataContract(Namespace = "http://www.ecm-tech.co.uk")]
public class ECUVariable
{
	[DataMember]
	public string address { get; set; }

	[DataMember]
	public string desc { get; set; }

	[DataMember]
	public double factor { get; set; }

	[DataMember]
	public string name { get; set; }

	[DataMember]
	public double offset { get; set; }

	[DataMember]
	public int precision { get; set; }

	[DataMember]
	public bool signed { get; set; }

	[DataMember]
	public string units { get; set; }

	[DataMember]
	public bool word { get; set; }

	public ushort value { get; set; }

	public string result { get; set; }

	public ECUVariable(string name, string desc, string address, string units, bool word, bool signed, double factor, double offset, int precision)
	{
		this.name = name;
		this.desc = desc;
		this.address = address;
		this.units = units;
		this.word = word;
		this.signed = signed;
		this.factor = factor;
		this.offset = offset;
		this.precision = precision;
	}

	public byte[] getRequestData()
	{
		byte[] array = new byte[3];
		if (address.Contains('x'))
		{
			address = address.Split('x')[1];
		}
		if (address.Length == 4)
		{
			byte[] addressFromString = getAddressFromString(address);
			array[0] = 0;
			array[1] = addressFromString[0];
			array[2] = addressFromString[1];
		}
		else
		{
			if (address.Length != 6)
			{
				throw new Exception();
			}
			byte[] addressFromString2 = getAddressFromString(address);
			array[0] = addressFromString2[0];
			array[1] = addressFromString2[1];
			array[2] = addressFromString2[2];
		}
		byte b = (byte)((!word) ? 1u : 2u);
		byte[] obj = new byte[8] { 207, 122, 170, 80, 0, 0, 0, 0 };
		obj[4] = array[0];
		obj[5] = array[1];
		obj[6] = array[2];
		obj[7] = b;
		return obj;
	}

	public ushort getHexValueFromString(string input)
	{
		ushort num = 0;
		if (input.Length == 2)
		{
			return getAddressFromString(input)[0];
		}
		if (input.Length == 4)
		{
			byte[] addressFromString = getAddressFromString(input);
			num = addressFromString[1];
			return (ushort)(num + (ushort)(addressFromString[0] * 256));
		}
		throw new Exception();
	}

	private byte[] getAddressFromString(string input)
	{
		if (input.Contains('x'))
		{
			input = input.Split('x')[1];
		}
		byte[] array = new byte[input.Length / 2];
		char[] array2 = input.ToCharArray();
		for (int i = 0; i < array2.Length; i += 2)
		{
			array[i / 2] = (byte)(16 * getByteFromChar(array2[i]));
			array[i / 2] += getByteFromChar(array2[i + 1]);
		}
		return array;
	}

	private byte getByteFromChar(char c)
	{
		switch (c)
		{
		case '0':
			return 0;
		case '1':
			return 1;
		case '2':
			return 2;
		case '3':
			return 3;
		case '4':
			return 4;
		case '5':
			return 5;
		case '6':
			return 6;
		case '7':
			return 7;
		case '8':
			return 8;
		case '9':
			return 9;
		case 'A':
		case 'a':
			return 10;
		case 'B':
		case 'b':
			return 11;
		case 'C':
		case 'c':
			return 12;
		case 'D':
		case 'd':
			return 13;
		case 'E':
		case 'e':
			return 14;
		case 'F':
		case 'f':
			return 15;
		default:
			return 0;
		}
	}
}
