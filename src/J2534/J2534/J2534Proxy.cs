#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace J2534;

public class J2534Proxy : IDisposable
{
	public enum FILTER_TYPE : uint
	{
		PASS_FILTER = 1u,
		BLOCK_FILTER,
		FLOW_CONTROL_FILTER
	}

	public enum IOCTL_TYPE : uint
	{
		GET_CONFIG = 1u,
		SET_CONFIG = 2u,
		READ_VBATT = 3u,
		FIVE_BAUD_INIT = 4u,
		FAST_INIT = 5u,
		CLEAR_TX_BUFFER = 7u,
		CLEAR_RX_BUFFER = 8u,
		CLEAR_PERIODIC_MSGS = 9u,
		CLEAR_MSG_FILTERS = 10u,
		CLEAR_FUNCT_MSG_LOOKUP_TABLE = 11u,
		ADD_TO_FUNCT_MSG_LOOKUP_TABLE = 12u,
		DELETE_FROM_FUNCT_MSG_LOOKUP_TABLE = 13u,
		READ_PROG_VOLTAGE = 14u,
		GET_DEVICE_INFO = 32780u,
		GET_PROTOCOL_INFO = 32781u,
		TEST_CARB_PLUG_CONNECTED = 268435457u,
		TEST_EEPROM = 268435458u,
		TEST_FLASH_INTERNAL = 268435459u,
		TEST_FLASH_EXTERNAL = 268435460u,
		TEST_RAM_INTERNAL = 268435461u,
		TEST_RAM_EXTERNAL = 268435462u,
		TEST_LED = 268435463u,
		TEST_PC_BT_LOOPBACK = 268435464u,
		TEST_K_PIN7_TO_L_LOOPBACK = 268435465u,
		TEST_K_PIN11_TO_L_LOOPBACK = 268435466u,
		TEST_L_TO_K_PIN7_LOOPBACK = 268435467u,
		TEST_L_TO_K_PIN11_LOOPBACK = 268435468u,
		TEST_CAN_MS_CAN_HS_LOOPBACK = 268435469u,
		TEST_CAN_HS_CAN_MS_LOOPBACK = 268435470u,
		CAN_XON_FILTER = 536870913u,
		CAN_XON_FILTER_ACTIVE = 536870914u,
		CLEAR_LOG_FILE = 268439555u,
		LOGGING_ACTIVE = 268439556u,
		WARRANTY_CLOCK_GET = 268439553u,
		GET_HW_VERSION = 268439554u
	}

	public class IOCTL_CONFIG_TYPE
	{
		public const uint DATA_RATE = 1u;

		public const uint LOOPBACK = 3u;

		public const uint PARITY = 22u;

		public const uint BIT_SAMPLE_POINT = 23u;

		public const uint ISO15765_BS = 30u;

		public const uint ISO15765_STMIN = 31u;

		public const uint ISO15765_SWDL_FABRICATE = 268435458u;

		public const uint P1_MIN = 6u;

		public const uint P1_MAX = 7u;

		public const uint P2_MIN = 8u;

		public const uint P2_MAX = 9u;

		public const uint P3_MIN = 10u;

		public const uint P3_MAX = 11u;

		public const uint P4_MIN = 12u;

		public const uint P4_MAX = 13u;

		public const uint W0 = 25u;

		public const uint W1 = 14u;

		public const uint W2 = 15u;

		public const uint W3 = 16u;

		public const uint W4 = 17u;

		public const uint W5 = 18u;

		public const uint J1962_PINS = 32769u;
	}

	[StructLayout(LayoutKind.Sequential)]
	public class S_TEST_RESULT
	{
		public uint testOk;
	}

	[StructLayout(LayoutKind.Sequential)]
	public class S_TEST_BAUDRATE
	{
		public TEST_BAUDRATE baudRate;
	}

	[StructLayout(LayoutKind.Sequential)]
	public class S_XON_XOFF_FILTER
	{
		public PASSTHRU_MSG xonMask;

		public PASSTHRU_MSG xonPattern;

		public PASSTHRU_MSG xoffMask;

		public PASSTHRU_MSG xoffPattern;

		public PASSTHRU_MSG errorMask;

		public PASSTHRU_MSG errorPattern;

		public S_XON_XOFF_FILTER(PASSTHRU_MSG xonMask, PASSTHRU_MSG xonPattern, PASSTHRU_MSG xoffMask, PASSTHRU_MSG xoffPattern, PASSTHRU_MSG errorMask, PASSTHRU_MSG errorPattern)
		{
			this.xonMask = xonMask;
			this.xonPattern = xonPattern;
			this.xoffMask = xoffMask;
			this.xoffPattern = xoffPattern;
			this.errorMask = errorMask;
			this.errorPattern = errorPattern;
		}
	}

	private class CFunctions
	{
		public J2534Delegates.PassThruOpen PassThruOpen;

		public J2534Delegates.PassThruClose PassThruClose;

		public J2534Delegates.PassThruConnect PassThruConnect;

		public J2534Delegates.PassThruDisconnect PassThruDisconnect;

		public J2534Delegates.PassThruReadVersion PassThruReadVersion;

		public J2534Delegates.PassThruGetLastError PassThruGetLastError;

		public J2534Delegates.PassThruReadMsgs PassThruReadMsgs;

		public J2534Delegates.PassThruWriteMsgs PassThruWriteMsgs;

		public J2534Delegates.PassThruStartPeriodicMsg PassThruStartPeriodicMsg;

		public J2534Delegates.PassThruStopPeriodicMsg PassThruStopPeriodicMsg;

		public J2534Delegates.PassThruStartMsgFilter PassThruStartMsgFilter;

		public J2534Delegates.PassThruStartMsgFilterFlowControl PassThruStartMsgFilterFlowControl;

		public J2534Delegates.PassThruStopMsgFilter PassThruStopMsgFilter;

		public J2534Delegates.PassThruIoctl_1 PassThruIoctl_1;

		public J2534Delegates.PassThruIoctl_2 PassThruIoctl_2;

		public J2534Delegates.PassThruIoctl_3 PassThruIoctl_3;

		public J2534Delegates.PassThruIoctl_4 PassThruIoctl_4;

		public J2534Delegates.PassThruIoctl_5 PassThruIoctl_5;

		public J2534Delegates.PassThruIoctl_ControlLed PassThruIoctl_ControlLed;

		public J2534Delegates.PassThruIoctl_CreateXonXoffFilter CreateXonXoffFilter;
	}

	private interface IUnmanagedObject
	{
		int UnmanagedSize { get; }

		IntPtr CopyToUnmanagedMemory(IntPtr memPtr);

		IntPtr CopyFromUnmanagedMemory(IntPtr memPtr);
	}

	private abstract class UnmanagedObjectList
	{
		protected List<IUnmanagedObject> _list = new List<IUnmanagedObject>();

		private int _headSize = 4 + IntPtr.Size;

		public virtual IntPtr AllocateAndCopy()
		{
			IntPtr intPtr = Allocate();
			IntPtr intPtr2 = new IntPtr(intPtr.ToInt32() + _headSize);
			Marshal.WriteInt32(intPtr, 0, _list.Count);
			Marshal.WriteIntPtr(intPtr, 4, intPtr2);
			for (int i = 0; i < _list.Count; i++)
			{
				intPtr2 = _list[i].CopyToUnmanagedMemory(intPtr2);
			}
			return intPtr;
		}

		private IntPtr Allocate()
		{
			int num = _headSize;
			for (int i = 0; i < _list.Count; i++)
			{
				num += _list[i].UnmanagedSize;
			}
			return Marshal.AllocCoTaskMem(num);
		}

		public virtual void CopyFromPtr(IntPtr memPtr)
		{
			IntPtr memPtr2 = new IntPtr(memPtr.ToInt32() + _headSize);
			for (int i = 0; i < _list.Count; i++)
			{
				memPtr2 = _list[i].CopyFromUnmanagedMemory(memPtr2);
			}
		}
	}

	private class SCONFIG : IUnmanagedObject
	{
		public uint Parameter;

		public uint Value;

		public int UnmanagedSize => 8;

		public SCONFIG(uint parameter, uint value)
		{
			Parameter = parameter;
			Value = value;
		}

		public IntPtr CopyToUnmanagedMemory(IntPtr memPtr)
		{
			Marshal.WriteInt32(memPtr, 0, (int)Parameter);
			Marshal.WriteInt32(memPtr, 4, (int)Value);
			return new IntPtr(memPtr.ToInt32() + UnmanagedSize);
		}

		public IntPtr CopyFromUnmanagedMemory(IntPtr memPtr)
		{
			Parameter = (uint)Marshal.ReadInt32(memPtr, 0);
			Value = (uint)Marshal.ReadInt32(memPtr, 4);
			return new IntPtr(memPtr.ToInt32() + UnmanagedSize);
		}
	}

	private class SCONFIG_LIST : UnmanagedObjectList
	{
		private uint[,] _parameterAndValues;

		public SCONFIG_LIST(uint[,] parameterAndValues)
		{
			_parameterAndValues = parameterAndValues;
			for (int i = 0; i < _parameterAndValues.GetLength(0); i++)
			{
				uint parameter = _parameterAndValues[i, 0];
				uint value = _parameterAndValues[i, 1];
				_list.Add(new SCONFIG(parameter, value));
			}
		}

		public override void CopyFromPtr(IntPtr memPtr)
		{
			base.CopyFromPtr(memPtr);
			for (int i = 0; i < _list.Count; i++)
			{
				SCONFIG sCONFIG = _list[i] as SCONFIG;
				_parameterAndValues[i, 0] = sCONFIG.Parameter;
				_parameterAndValues[i, 1] = sCONFIG.Value;
			}
		}
	}

	private class SBYTE_ARRAY
	{
		private byte[] _array;

		private int _headSize = 4 + IntPtr.Size;

		public byte this[int i]
		{
			get
			{
				return _array[i];
			}
			set
			{
				_array[i] = value;
			}
		}

		public SBYTE_ARRAY(int size)
		{
			_array = new byte[size];
		}

		public IntPtr AllocateAndCopy()
		{
			IntPtr intPtr = Marshal.AllocCoTaskMem(_headSize + _array.Length);
			IntPtr intPtr2 = new IntPtr(intPtr.ToInt32() + _headSize);
			Marshal.WriteInt32(intPtr, 0, _array.Length);
			Marshal.WriteIntPtr(intPtr, 4, intPtr2);
			for (int i = 0; i < _array.Length; i++)
			{
				Marshal.WriteByte(intPtr2, i, _array[i]);
			}
			return intPtr;
		}

		public void CopyFromPtr(IntPtr memPtr)
		{
			IntPtr ptr = new IntPtr(memPtr.ToInt32() + _headSize);
			for (int i = 0; i < _array.Length; i++)
			{
				_array[i] = Marshal.ReadByte(ptr, i);
			}
		}
	}

	public const int PASSTHRU_STRING_SIZE = 80;

	private IntPtr _dllHandle = IntPtr.Zero;

	private string _dllName;

	private bool _deviceIsDiCECompatible;

	private CFunctions _cFunctions;

	private object _lockObject = new object();

	public string LibraryName
	{
		get
		{
			lock (_lockObject)
			{
				return _dllName;
			}
		}
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr LoadLibrary(string dllname);

	[DllImport("kernel32.dll", SetLastError = true)]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool FreeLibrary(IntPtr hModule);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetProcAddress(IntPtr hModule, string procname);

	public J2534Proxy(string name)
	{
		try
		{
			LoadDll(name);
		}
		catch (DllNotFoundException)
		{
			throw;
		}
		catch (Exception)
		{
			UnloadDll();
			throw;
		}
	}

	public J2534Proxy(J2534Device device)
	{
		string functionLibrary = device.FunctionLibrary;
		try
		{
			LoadDll(functionLibrary);
		}
		catch (DllNotFoundException)
		{
			throw;
		}
		catch (Exception)
		{
			UnloadDll();
			throw;
		}
	}

	public void Dispose()
	{
		UnloadDll();
	}

	private void LoadDll(string dllName)
	{
		if (!(_dllHandle != IntPtr.Zero))
		{
			_dllHandle = LoadLibrary(dllName);
			if (_dllHandle == IntPtr.Zero)
			{
				throw new DllNotFoundException("J2534 driver '" + dllName + "' could not be loaded");
			}
			_dllName = dllName;
			_cFunctions = new CFunctions();
			IntPtr procAddress = GetProcAddress(_dllHandle, "PassThruOpen");
			_cFunctions.PassThruOpen = (J2534Delegates.PassThruOpen)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruOpen));
			procAddress = GetProcAddress(_dllHandle, "PassThruClose");
			_cFunctions.PassThruClose = (J2534Delegates.PassThruClose)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruClose));
			procAddress = GetProcAddress(_dllHandle, "PassThruReadVersion");
			_cFunctions.PassThruReadVersion = (J2534Delegates.PassThruReadVersion)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruReadVersion));
			procAddress = GetProcAddress(_dllHandle, "PassThruGetLastError");
			_cFunctions.PassThruGetLastError = (J2534Delegates.PassThruGetLastError)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruGetLastError));
			procAddress = GetProcAddress(_dllHandle, "PassThruConnect");
			_cFunctions.PassThruConnect = (J2534Delegates.PassThruConnect)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruConnect));
			procAddress = GetProcAddress(_dllHandle, "PassThruDisconnect");
			_cFunctions.PassThruDisconnect = (J2534Delegates.PassThruDisconnect)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruDisconnect));
			procAddress = GetProcAddress(_dllHandle, "PassThruIoctl");
			_cFunctions.PassThruIoctl_1 = (J2534Delegates.PassThruIoctl_1)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruIoctl_1));
			_cFunctions.PassThruIoctl_2 = (J2534Delegates.PassThruIoctl_2)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruIoctl_2));
			_cFunctions.PassThruIoctl_3 = (J2534Delegates.PassThruIoctl_3)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruIoctl_3));
			_cFunctions.PassThruIoctl_4 = (J2534Delegates.PassThruIoctl_4)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruIoctl_4));
			_cFunctions.PassThruIoctl_5 = (J2534Delegates.PassThruIoctl_5)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruIoctl_5));
			_cFunctions.PassThruIoctl_ControlLed = (J2534Delegates.PassThruIoctl_ControlLed)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruIoctl_ControlLed));
			_cFunctions.CreateXonXoffFilter = (J2534Delegates.PassThruIoctl_CreateXonXoffFilter)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruIoctl_CreateXonXoffFilter));
			procAddress = GetProcAddress(_dllHandle, "PassThruReadMsgs");
			_cFunctions.PassThruReadMsgs = (J2534Delegates.PassThruReadMsgs)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruReadMsgs));
			procAddress = GetProcAddress(_dllHandle, "PassThruWriteMsgs");
			_cFunctions.PassThruWriteMsgs = (J2534Delegates.PassThruWriteMsgs)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruWriteMsgs));
			procAddress = GetProcAddress(_dllHandle, "PassThruStartMsgFilter");
			_cFunctions.PassThruStartMsgFilter = (J2534Delegates.PassThruStartMsgFilter)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruStartMsgFilter));
			_cFunctions.PassThruStartMsgFilterFlowControl = (J2534Delegates.PassThruStartMsgFilterFlowControl)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruStartMsgFilterFlowControl));
			procAddress = GetProcAddress(_dllHandle, "PassThruStopMsgFilter");
			_cFunctions.PassThruStopMsgFilter = (J2534Delegates.PassThruStopMsgFilter)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruStopMsgFilter));
			procAddress = GetProcAddress(_dllHandle, "PassThruStartPeriodicMsg");
			_cFunctions.PassThruStartPeriodicMsg = (J2534Delegates.PassThruStartPeriodicMsg)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruStartPeriodicMsg));
			procAddress = GetProcAddress(_dllHandle, "PassThruStopPeriodicMsg");
			_cFunctions.PassThruStopPeriodicMsg = (J2534Delegates.PassThruStopPeriodicMsg)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(J2534Delegates.PassThruStopPeriodicMsg));
		}
	}

	public void UnloadDll()
	{
		if (_dllHandle != IntPtr.Zero)
		{
			FreeLibrary(_dllHandle);
			_dllHandle = IntPtr.Zero;
			_dllName = string.Empty;
			_cFunctions = null;
		}
	}

	public ERROR_CODES PassThruOpen(ref string pName, ref uint pDeviceId, bool deviceIsDiCECompatible)
	{
		lock (_lockObject)
		{
			_deviceIsDiCECompatible = deviceIsDiCECompatible;
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruOpen(pName, ref pDeviceId);
			if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
			{
				pName = "";
				string ErrorDescription = "";
				PassThruGetLastError(out ErrorDescription);
				LogJ2534Error("PassThruOpen", eRROR_CODES, ErrorDescription);
			}
			return eRROR_CODES;
		}
	}

	public ERROR_CODES PassThruClose(uint deviceId)
	{
		lock (_lockObject)
		{
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruClose(deviceId);
			if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
			{
				string ErrorDescription = "";
				PassThruGetLastError(out ErrorDescription);
				LogJ2534Error("PassThruClose", eRROR_CODES, ErrorDescription);
			}
			return eRROR_CODES;
		}
	}

	public ERROR_CODES PassThruConnect(uint deviceId, PROTOCOL_TYPE protocolID, uint flags, uint baudRate, ref uint channelID)
	{
		lock (_lockObject)
		{
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruConnect(deviceId, (uint)protocolID, flags, baudRate, ref channelID);
			if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
			{
				string ErrorDescription = "";
				PassThruGetLastError(out ErrorDescription);
				LogJ2534Error("PassThruConnect", eRROR_CODES, ErrorDescription);
			}
			return eRROR_CODES;
		}
	}

	public ERROR_CODES PassThruDisconnect(uint channelID)
	{
		lock (_lockObject)
		{
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruDisconnect(channelID);
			if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
			{
				string ErrorDescription = "";
				PassThruGetLastError(out ErrorDescription);
				LogJ2534Error("PassThruDisconnect", eRROR_CODES, ErrorDescription);
			}
			return eRROR_CODES;
		}
	}

	public ERROR_CODES PassThruReadMsgs(uint deviceId, uint channelID, IntPtr pMsg, ref uint pNumMsgs, uint Timeout)
	{
		Trace.Assert(pMsg != IntPtr.Zero);
		ERROR_CODES eRROR_CODES = _cFunctions.PassThruReadMsgs(channelID, pMsg, ref pNumMsgs, Timeout);
		if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
		{
			string ErrorDescription = "";
			PassThruGetLastError(out ErrorDescription);
			LogJ2534Error("PassThruReadMsgs", eRROR_CODES, ErrorDescription);
		}
		return eRROR_CODES;
	}

	private void LoggingState(uint DeviceID, bool Active)
	{
		lock (_lockObject)
		{
			if (_deviceIsDiCECompatible)
			{
				uint valueIn = (Active ? 1u : 0u);
				_cFunctions.PassThruIoctl_3(DeviceID, 268439556u, ref valueIn, IntPtr.Zero);
			}
		}
	}

	public ERROR_CODES PassThruWriteMsgs(uint deviceId, uint channelID, IntPtr pMsg, uint pNumMsgs, uint Timeout, bool ClearRXBuffer)
	{
		ERROR_CODES eRROR_CODES;
		if (ClearRXBuffer)
		{
			eRROR_CODES = PassThruIoctl_CLEAR_RX_BUFFER(channelID);
			if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
			{
				string ErrorDescription = "";
				PassThruGetLastError(out ErrorDescription);
				LogJ2534Error("PassThruIoctl_CLEAR_RX_BUFFER", eRROR_CODES, ErrorDescription);
				return eRROR_CODES;
			}
		}
		eRROR_CODES = _cFunctions.PassThruWriteMsgs(channelID, pMsg, ref pNumMsgs, Timeout);
		if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
		{
			string ErrorDescription2 = "";
			PassThruGetLastError(out ErrorDescription2);
			LogJ2534Error("PassThruWriteMsgs", eRROR_CODES, ErrorDescription2);
		}
		return eRROR_CODES;
	}

	public ERROR_CODES PassThruIoctl_CreateXonXoffFilter(uint channelID, PASSTHRU_MSG xonMask, PASSTHRU_MSG xonPattern, PASSTHRU_MSG xoffMask, PASSTHRU_MSG xoffPattern, PASSTHRU_MSG errorMask, PASSTHRU_MSG errorPattern)
	{
		S_XON_XOFF_FILTER filter = new S_XON_XOFF_FILTER(xonMask, xonPattern, xoffMask, xoffPattern, errorMask, errorPattern);
		ERROR_CODES eRROR_CODES = _cFunctions.CreateXonXoffFilter(channelID, 536870913u, filter, IntPtr.Zero);
		if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
		{
			return eRROR_CODES;
		}
		uint valueIn = 1u;
		return _cFunctions.PassThruIoctl_3(channelID, 536870914u, ref valueIn, IntPtr.Zero);
	}

	public ERROR_CODES PassThruStartPeriodicMsg(uint channelID, PASSTHRU_MSG pMsg, ref uint pMsgID, uint TimeInterval)
	{
		ERROR_CODES eRROR_CODES = _cFunctions.PassThruStartPeriodicMsg(channelID, pMsg, ref pMsgID, TimeInterval);
		if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
		{
			string ErrorDescription = "";
			PassThruGetLastError(out ErrorDescription);
			LogJ2534Error("PassThruStartPeriodicMsg", eRROR_CODES, ErrorDescription);
		}
		return eRROR_CODES;
	}

	public ERROR_CODES PassThruStopPeriodicMsg(uint channelID, uint MsgID)
	{
		try
		{
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruStopPeriodicMsg(channelID, MsgID);
			if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
			{
				string ErrorDescription = "";
				PassThruGetLastError(out ErrorDescription);
				LogJ2534Error("PassThruStopPeriodicMsg", eRROR_CODES, ErrorDescription);
			}
			return eRROR_CODES;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			return ERROR_CODES.STATUS_NOERROR;
		}
	}

	public ERROR_CODES PassThruStartMsgFilter(uint channelID, FILTER_TYPE FilterType, PASSTHRU_MSG pMaskMsg, PASSTHRU_MSG pPatternMsg, PASSTHRU_MSG pFlowControlMsg, ref uint pMsgID)
	{
		ERROR_CODES eRROR_CODES = _cFunctions.PassThruStartMsgFilterFlowControl(channelID, (uint)FilterType, pMaskMsg, pPatternMsg, pFlowControlMsg, ref pMsgID);
		if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
		{
			string ErrorDescription = "";
			PassThruGetLastError(out ErrorDescription);
			LogJ2534Error("PassThruStartMsgFilter", eRROR_CODES, ErrorDescription);
		}
		return eRROR_CODES;
	}

	public ERROR_CODES PassThruStartMsgFilter(uint channelID, FILTER_TYPE FilterType, PASSTHRU_MSG pMaskMsg, PASSTHRU_MSG pPatternMsg, ref uint pMsgID)
	{
		ERROR_CODES eRROR_CODES = _cFunctions.PassThruStartMsgFilter(channelID, (uint)FilterType, pMaskMsg, pPatternMsg, IntPtr.Zero, ref pMsgID);
		if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
		{
			string ErrorDescription = "";
			PassThruGetLastError(out ErrorDescription);
			LogJ2534Error("PassThruStartMsgFilter", eRROR_CODES, ErrorDescription);
		}
		return eRROR_CODES;
	}

	public ERROR_CODES PassThruStopMsgFilter(uint channelID, uint MsgID)
	{
		ERROR_CODES eRROR_CODES = _cFunctions.PassThruStopMsgFilter(channelID, MsgID);
		if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
		{
			string ErrorDescription = "";
			PassThruGetLastError(out ErrorDescription);
			LogJ2534Error("PassThruStopMsgFilter", eRROR_CODES, ErrorDescription);
		}
		return eRROR_CODES;
	}

	public ERROR_CODES PassThruReadVersion(uint DeviceID, out string FirmwareVersion, out string DllVersion, out string ApiVersion)
	{
		lock (_lockObject)
		{
			string FirmwareVersion2 = new string(' ', 80);
			string DllVersion2 = new string(' ', 80);
			string ApiVersion2 = new string(' ', 80);
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruReadVersion(DeviceID, ref FirmwareVersion2, ref DllVersion2, ref ApiVersion2);
			if (eRROR_CODES == ERROR_CODES.STATUS_NOERROR)
			{
				string text = FirmwareVersion2.TrimEnd(' ');
				char[] trimChars = new char[1];
				FirmwareVersion = text.TrimEnd(trimChars);
				string text2 = DllVersion2.TrimEnd(' ');
				char[] trimChars2 = new char[1];
				DllVersion = text2.TrimEnd(trimChars2);
				string text3 = ApiVersion2.TrimEnd(' ');
				char[] trimChars3 = new char[1];
				ApiVersion = text3.TrimEnd(trimChars3);
			}
			else
			{
				FirmwareVersion = string.Empty;
				DllVersion = string.Empty;
				ApiVersion = string.Empty;
				string ErrorDescription = "";
				PassThruGetLastError(out ErrorDescription);
				LogJ2534Error("PassThruReadVersion", eRROR_CODES, ErrorDescription);
			}
			return eRROR_CODES;
		}
	}

	public ERROR_CODES PassThruGetLastError(out string ErrorDescription)
	{
		lock (_lockObject)
		{
			ErrorDescription = string.Empty;
			string ErrorDescription2 = new string(' ', 80);
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruGetLastError(ref ErrorDescription2);
			if (eRROR_CODES == ERROR_CODES.STATUS_NOERROR)
			{
				string text = ErrorDescription2.TrimEnd(' ');
				char[] trimChars = new char[1];
				ErrorDescription = text.TrimEnd(trimChars);
			}
			else
			{
				ErrorDescription = "PassThruGetLastError returned " + eRROR_CODES;
			}
			return eRROR_CODES;
		}
	}

	public ERROR_CODES PassThruIoctl_SET_CONFIG(uint ChannelID, uint[,] parameterAndValues)
	{
		lock (_lockObject)
		{
			SCONFIG_LIST list = new SCONFIG_LIST(parameterAndValues);
			return PassThruIoctl_SET_CONFIG(ChannelID, list);
		}
	}

	private ERROR_CODES PassThruIoctl_SET_CONFIG(uint ChannelID, SCONFIG_LIST list)
	{
		lock (_lockObject)
		{
			IntPtr intPtr = list.AllocateAndCopy();
			try
			{
				return _cFunctions.PassThruIoctl_1(ChannelID, 2u, intPtr, IntPtr.Zero);
			}
			finally
			{
				Marshal.FreeCoTaskMem(intPtr);
			}
		}
	}

	private ERROR_CODES PassThruIoctl_GET_CONFIG(uint ChannelID, SCONFIG_LIST list)
	{
		lock (_lockObject)
		{
			IntPtr intPtr = list.AllocateAndCopy();
			try
			{
				ERROR_CODES num = _cFunctions.PassThruIoctl_1(ChannelID, 1u, intPtr, IntPtr.Zero);
				if (num == ERROR_CODES.STATUS_NOERROR)
				{
					list.CopyFromPtr(intPtr);
				}
				return num;
			}
			finally
			{
				Marshal.FreeCoTaskMem(intPtr);
			}
		}
	}

	public ERROR_CODES PassThruIoctl_GET_CONFIG(uint ChannelID, uint[,] parameterAndValue)
	{
		lock (_lockObject)
		{
			SCONFIG_LIST list = new SCONFIG_LIST(parameterAndValue);
			return PassThruIoctl_GET_CONFIG(ChannelID, list);
		}
	}

	public ERROR_CODES PassThruIoctl_CLEAR_RX_BUFFER(uint ChannelID)
	{
		lock (_lockObject)
		{
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruIoctl_1(ChannelID, 8u, IntPtr.Zero, IntPtr.Zero);
			if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
			{
				string ErrorDescription = "";
				PassThruGetLastError(out ErrorDescription);
				LogJ2534Error("PassThruStopMsgFilter", eRROR_CODES, ErrorDescription);
			}
			return eRROR_CODES;
		}
	}

	public ERROR_CODES PassThruIoctl_FIVE_BAUD_INIT(uint ChannelID, byte targetAddress, ref byte keyWord1, ref byte keyWord2)
	{
		lock (_lockObject)
		{
			SBYTE_ARRAY sBYTE_ARRAY = new SBYTE_ARRAY(1);
			SBYTE_ARRAY sBYTE_ARRAY2 = new SBYTE_ARRAY(2);
			sBYTE_ARRAY[0] = targetAddress;
			IntPtr intPtr = sBYTE_ARRAY.AllocateAndCopy();
			IntPtr intPtr2 = sBYTE_ARRAY2.AllocateAndCopy();
			try
			{
				ERROR_CODES num = _cFunctions.PassThruIoctl_1(ChannelID, 4u, intPtr, intPtr2);
				if (num == ERROR_CODES.STATUS_NOERROR)
				{
					sBYTE_ARRAY2.CopyFromPtr(intPtr2);
					keyWord1 = sBYTE_ARRAY2[0];
					keyWord2 = sBYTE_ARRAY2[1];
				}
				return num;
			}
			finally
			{
				Marshal.FreeCoTaskMem(intPtr);
				Marshal.FreeCoTaskMem(intPtr2);
			}
		}
	}

	public ERROR_CODES PassThruIoctl_DiceControlLed(uint DeviceID, S_LED_CONTROL LEDInfo)
	{
		lock (_lockObject)
		{
			return _cFunctions.PassThruIoctl_ControlLed(DeviceID, 268435463u, LEDInfo, IntPtr.Zero);
		}
	}

	public ERROR_CODES PassThruIoctl_DiceGetHardwareVersion(uint DeviceID, ref string HardwareVersion)
	{
		lock (_lockObject)
		{
			string valueOut = new string(' ', 80);
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruIoctl_5(DeviceID, 268439554u, IntPtr.Zero, ref valueOut);
			if (eRROR_CODES == ERROR_CODES.STATUS_NOERROR)
			{
				string text = valueOut.TrimEnd(' ');
				char[] trimChars = new char[1];
				HardwareVersion = text.TrimEnd(trimChars);
			}
			else
			{
				HardwareVersion = string.Empty;
			}
			return eRROR_CODES;
		}
	}

	public ERROR_CODES PassThruIoctl_DiceGetWarrantyDate(uint DeviceID, ref string WarrantyDate)
	{
		lock (_lockObject)
		{
			string valueOut = new string(' ', 80);
			ERROR_CODES eRROR_CODES = _cFunctions.PassThruIoctl_5(DeviceID, 268439553u, IntPtr.Zero, ref valueOut);
			if (eRROR_CODES == ERROR_CODES.STATUS_NOERROR)
			{
				string text = valueOut.TrimEnd(' ');
				char[] trimChars = new char[1];
				WarrantyDate = text.TrimEnd(trimChars);
			}
			else
			{
				WarrantyDate = string.Empty;
			}
			return eRROR_CODES;
		}
	}

	public ERROR_CODES PassThruIoctl_ReadVBatt(uint DeviceID, ref uint batteryVoltage)
	{
		ERROR_CODES eRROR_CODES = _cFunctions.PassThruIoctl_2(DeviceID, 3u, IntPtr.Zero, ref batteryVoltage);
		if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
		{
			batteryVoltage = 0u;
			string ErrorDescription = "";
			PassThruGetLastError(out ErrorDescription);
			LogJ2534Error("PassThruIoctl_ReadVBatt", eRROR_CODES, ErrorDescription);
		}
		return eRROR_CODES;
	}

	public ERROR_CODES PassThruIoctl_ConnectionStrength(uint DeviceID, ref uint BTConnectionStrength)
	{
		ERROR_CODES eRROR_CODES = _cFunctions.PassThruIoctl_2(DeviceID, 268435464u, IntPtr.Zero, ref BTConnectionStrength);
		if (eRROR_CODES != ERROR_CODES.STATUS_NOERROR)
		{
			string ErrorDescription = "";
			PassThruGetLastError(out ErrorDescription);
			LogJ2534Error("PassThruIoctl_ConnectionStrength", eRROR_CODES, ErrorDescription);
		}
		return eRROR_CODES;
	}

	public ERROR_CODES PassThruIoctl_DiceTest(uint deviceID, uint testID, ref uint testResult)
	{
		lock (_lockObject)
		{
			return _cFunctions.PassThruIoctl_2(deviceID, testID, IntPtr.Zero, ref testResult);
		}
	}

	public ERROR_CODES PassThruIoctl_DiceTest(uint deviceID, uint testID, ref uint testData, ref uint testResult)
	{
		lock (_lockObject)
		{
			return _cFunctions.PassThruIoctl_4(deviceID, testID, ref testData, ref testResult);
		}
	}

	private void LogJ2534Error(string functionBlock, ERROR_CODES e, string desc)
	{
		Console.WriteLine(functionBlock);
		Console.WriteLine(e.ToString());
		Console.WriteLine(desc);
	}
}
