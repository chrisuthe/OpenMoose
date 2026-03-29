using System;
using System.Collections.Generic;
using System.IO;

namespace J2534.Checksums;

public class VolvoChecksumUpdater
{
	private string binFilePath;

	private byte[] binFile;

	private bool me75;

	private bool b5254rt;

	private bool flash512;

	public VolvoChecksumUpdater(string binFilePath)
	{
		this.binFilePath = binFilePath;
		string[] array = binFilePath.Split('.');
		long num = new FileInfo(binFilePath).Length / 1024;
		if ((num != 512 && num != 1024) || !(array[array.Length - 1].ToLower() == "bin"))
		{
			throw new Exception();
		}
		binFile = File.ReadAllBytes(binFilePath);
		if (num == 512)
		{
			flash512 = true;
		}
		else
		{
			flash512 = false;
		}
		me75 = false;
		b5254rt = false;
		determineEngine();
		determineVersion();
	}

	public VolvoChecksumUpdater(byte[] binFile)
	{
		binFilePath = "";
		long num = binFile.Length / 1024;
		if (num != 512 && num != 1024)
		{
			throw new Exception();
		}
		this.binFile = binFile;
		if (num == 512)
		{
			flash512 = true;
		}
		else
		{
			flash512 = false;
		}
		me75 = false;
		b5254rt = false;
		determineEngine();
		determineVersion();
	}

	public bool updateChecksums(bool checkOnly)
	{
		if (binFilePath.Equals(""))
		{
			checkOnly = true;
		}
		bool flag = true;
		flag &= middleChecksumUpdate(checkOnly);
		if (!flash512)
		{
			flag &= endChecksumUpdate(checkOnly);
		}
		return flag;
	}

	private bool endChecksumUpdate(bool checkOnly)
	{
		bool flag = false;
		uint num = 0u;
		uint num2 = 0u;
		uint num3 = 0u;
		uint num4 = 0u;
		uint num5 = 0u;
		uint num6 = 0u;
		uint num7 = 0u;
		uint num8 = 0u;
		if (me75)
		{
			num = 49152u;
			num2 = 57344u;
			num3 = 68352u;
			num4 = 129024u;
			num5 = 130048u;
			num6 = 1048560u;
			num7 = 0u;
			num8 = 0u;
		}
		else
		{
			if (me75 || !b5254rt)
			{
				return true;
			}
			num = 49152u;
			num2 = 57344u;
			num3 = 67840u;
			num4 = 129024u;
			num5 = 130048u;
			num6 = 851968u;
			num7 = 0u;
			num8 = 0u;
		}
		uint num9 = 0u;
		uint num10 = 0u;
		uint num11 = 0u;
		byte b = 0;
		byte b2 = 0;
		byte b3 = 0;
		byte b4 = 0;
		if (num != 0 && num2 != 0)
		{
			for (uint num12 = num; num12 < num2; num12 += 2)
			{
				num9 += binFile[num12];
				num10 += binFile[num12 + 1];
			}
		}
		if (num3 != 0 && num4 != 0)
		{
			for (uint num13 = num3; num13 < num4; num13 += 2)
			{
				num9 += binFile[num13];
				num10 += binFile[num13 + 1];
			}
		}
		if (num5 != 0 && num6 != 0)
		{
			for (uint num14 = num5; num14 < num6; num14 += 2)
			{
				num9 += binFile[num14];
				num10 += binFile[num14 + 1];
			}
		}
		if (num7 != 0 && num8 != 0)
		{
			for (uint num15 = num7; num15 < num8; num15 += 2)
			{
				num9 += binFile[num15];
				num10 += binFile[num15 + 1];
			}
		}
		num10 = (num9 >> 8) + num10;
		uint num16 = num10 >> 8;
		num11 = num16 >> 8;
		b = (byte)num9;
		b2 = (byte)num10;
		b3 = (byte)num16;
		b4 = (byte)num11;
		if (!checkOnly)
		{
			try
			{
				savebytetobinary(1048560, b, binFilePath);
				savebytetobinary(1048561, b2, binFilePath);
				savebytetobinary(1048562, b3, binFilePath);
				savebytetobinary(1048563, b4, binFilePath);
				savebytetobinary(1048564, (byte)(~b), binFilePath);
				savebytetobinary(1048565, (byte)(~b2), binFilePath);
				savebytetobinary(1048566, (byte)(~b3), binFilePath);
				savebytetobinary(1048567, (byte)(~b4), binFilePath);
				return true;
			}
			catch (Exception ex)
			{
				ex.ToString();
				return false;
			}
		}
		if (b == binFile[1048560] && b2 == binFile[1048561] && b3 == binFile[1048562] && b4 == binFile[1048563])
		{
			return true;
		}
		return false;
	}

	private bool middleChecksumUpdate(bool checkOnly)
	{
		uint num = 129024u;
		uint num2 = 0u;
		for (uint num3 = num; num3 < 130304; num3 += 16)
		{
			if (binFile[num3 + 8] != (byte)(~binFile[num3 + 12]) || binFile[num3 + 9] != (byte)(~binFile[num3 + 13]) || binFile[num3 + 10] != (byte)(~binFile[num3 + 14]) || binFile[num3 + 11] != (byte)(~binFile[num3 + 15]))
			{
				num2 = num3;
				break;
			}
		}
		bool result = true;
		do
		{
			int num4 = (binFile[num + 3] << 24) + (binFile[num + 2] << 16) + (binFile[num + 1] << 8) + binFile[num];
			uint num5 = (uint)((binFile[num + 7] << 24) + (binFile[num + 6] << 16) + (binFile[num + 5] << 8) + binFile[num + 4]);
			uint num6 = 0u;
			for (uint num7 = (uint)num4; num7 < num5; num7 += 2)
			{
				num6 += (uint)((binFile[num7 + 1] << 8) + binFile[num7]);
			}
			uint num8 = (uint)((binFile[num + 11] << 24) + (binFile[num + 10] << 16) + (binFile[num + 9] << 8) + binFile[num + 8]);
			int num9 = (binFile[num + 15] << 24) + (binFile[num + 14] << 16) + (binFile[num + 13] << 8) + binFile[num + 12];
			if (num6 != num8)
			{
				result = false;
			}
			uint num10 = ~num6;
			if (num9 != (int)num10)
			{
				result = false;
			}
			if (!checkOnly)
			{
				try
				{
					savebytetobinary((int)(num + 8), (byte)(num6 & 0xFF), binFilePath);
					savebytetobinary((int)(num + 9), (byte)((num6 & 0xFF00) >> 8), binFilePath);
					savebytetobinary((int)(num + 10), (byte)((num6 & 0xFF0000) >> 16), binFilePath);
					savebytetobinary((int)(num + 11), (byte)((num6 & 0xFF000000u) >> 24), binFilePath);
					num6 = ~num6;
					savebytetobinary((int)(num + 12), (byte)(num6 & 0xFF), binFilePath);
					savebytetobinary((int)(num + 13), (byte)((num6 & 0xFF00) >> 8), binFilePath);
					savebytetobinary((int)(num + 14), (byte)((num6 & 0xFF0000) >> 16), binFilePath);
					savebytetobinary((int)(num + 15), (byte)((num6 & 0xFF000000u) >> 24), binFilePath);
					result = true;
				}
				catch (Exception ex)
				{
					ex.ToString();
					result = false;
					break;
				}
			}
			num += 16;
		}
		while (num < num2);
		return result;
	}

	private static void savebytetobinary(int address, byte data, string filename)
	{
		FileStream fileStream = File.OpenWrite(filename);
		BinaryWriter binaryWriter = new BinaryWriter(fileStream);
		fileStream.Position = address;
		binaryWriter.Write(data);
		fileStream.Flush();
		binaryWriter.Close();
		fileStream.Close();
		fileStream.Dispose();
	}

	private void determineVersion()
	{
		byte[] pattern = new byte[7] { 77, 69, 55, 95, 53, 48, 48 };
		foreach (long item in getIndex(binFile, pattern))
		{
			if (item > 98304 && item < 101376)
			{
				me75 = true;
			}
		}
	}

	private void determineEngine()
	{
		byte[] pattern = new byte[7] { 66, 53, 50, 53, 52, 82, 84 };
		foreach (long item in getIndex(binFile, pattern))
		{
			if (item > 98304 && item < 101376)
			{
				b5254rt = true;
			}
		}
	}

	private List<long> getIndex(byte[] value, byte[] pattern)
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
}
