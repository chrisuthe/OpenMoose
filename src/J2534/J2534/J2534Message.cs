using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace J2534;

public class J2534Message : IList, ICollection, IEnumerable
{
	private class MessageEnumerator : IEnumerator
	{
		private int position = -1;

		private J2534Message message_;

		public object Current => message_[position];

		public MessageEnumerator(J2534Message message)
		{
			message_ = message;
		}

		public bool MoveNext()
		{
			if (position < message_.Length - 1)
			{
				position++;
				return true;
			}
			return false;
		}

		public void Reset()
		{
			position = -1;
		}
	}

	private IntPtr data_ = IntPtr.Zero;

	private int size_;

	private readonly int maxSize_;

	public IntPtr Pointer => data_;

	public int Length
	{
		get
		{
			return size_;
		}
		set
		{
			if (value > maxSize_)
			{
				throw new Exception("Size error");
			}
			size_ = value;
		}
	}

	public int MaxLength => maxSize_;

	public virtual PASSTHRU_MSG this[int index]
	{
		get
		{
			if (index > size_)
			{
				throw new Exception("Index error");
			}
			int num = Marshal.SizeOf(typeof(PASSTHRU_MSG));
			PASSTHRU_MSG pASSTHRU_MSG = new PASSTHRU_MSG();
			pASSTHRU_MSG.ProtocolID = (PROTOCOL_TYPE)Marshal.ReadInt32(data_, index * num);
			pASSTHRU_MSG.RxStatus = (uint)Marshal.ReadInt32(data_, index * num + 4);
			pASSTHRU_MSG.TxFlags = (uint)Marshal.ReadInt32(data_, index * num + 8);
			pASSTHRU_MSG.Timestamp = (uint)Marshal.ReadInt32(data_, index * num + 12);
			pASSTHRU_MSG.DataSize = (uint)Marshal.ReadInt32(data_, index * num + 16);
			pASSTHRU_MSG.ExtraDataIndex = (uint)Marshal.ReadInt32(data_, index * num + 20);
			int num2 = Math.Min((int)pASSTHRU_MSG.DataSize, 4128);
			for (int i = 0; i < num2; i++)
			{
				pASSTHRU_MSG.Data[i] = Marshal.ReadByte(data_, index * num + 24 + i);
			}
			return pASSTHRU_MSG;
		}
		set
		{
			if (index > size_)
			{
				throw new Exception("Index error");
			}
			int num = Marshal.SizeOf(typeof(PASSTHRU_MSG));
			Marshal.WriteInt32(data_, index * num, (int)value.ProtocolID);
			Marshal.WriteInt32(data_, index * num + 4, (int)value.RxStatus);
			Marshal.WriteInt32(data_, index * num + 8, (int)value.TxFlags);
			Marshal.WriteInt32(data_, index * num + 12, (int)value.Timestamp);
			Marshal.WriteInt32(data_, index * num + 16, (int)value.DataSize);
			Marshal.WriteInt32(data_, index * num + 20, (int)value.ExtraDataIndex);
			int num2 = Math.Min((int)value.DataSize, 4128);
			for (int i = 0; i < num2; i++)
			{
				Marshal.WriteByte(data_, index * num + 24 + i, value.Data[i]);
			}
		}
	}

	public bool IsReadOnly => false;

	object IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			this[index] = (PASSTHRU_MSG)value;
		}
	}

	public bool IsFixedSize => false;

	public bool IsSynchronized => false;

	public int Count => Length;

	public object SyncRoot
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public J2534Message(IntPtr data, int size)
	{
		data_ = data;
		size_ = size;
		maxSize_ = size + 1;
		if (size_ > maxSize_)
		{
			throw new Exception("Size error");
		}
	}

	public J2534Message(IntPtr data, int size, int maxSize)
	{
		data_ = data;
		size_ = size;
		maxSize_ = maxSize;
		if (size_ > maxSize_)
		{
			throw new Exception("Size error");
		}
	}

	public void RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	public void Insert(int index, object value)
	{
		throw new NotSupportedException();
	}

	public void Remove(object value)
	{
		throw new NotSupportedException();
	}

	public bool Contains(object value)
	{
		return IndexOf(value) != -1;
	}

	public void Clear()
	{
		throw new NotSupportedException();
	}

	public int IndexOf(object value)
	{
		PASSTHRU_MSG pASSTHRU_MSG = (PASSTHRU_MSG)value;
		int num = Marshal.SizeOf(typeof(PASSTHRU_MSG));
		for (int i = 0; i < Length; i++)
		{
			if (pASSTHRU_MSG.ProtocolID != (PROTOCOL_TYPE)Marshal.ReadInt32(data_, i * num) || pASSTHRU_MSG.RxStatus != (uint)Marshal.ReadInt32(data_, i * num + 4) || pASSTHRU_MSG.TxFlags != (uint)Marshal.ReadInt32(data_, i * num + 8) || pASSTHRU_MSG.Timestamp != (uint)Marshal.ReadInt32(data_, i * num + 12) || pASSTHRU_MSG.DataSize != (uint)Marshal.ReadInt32(data_, i * num + 16) || pASSTHRU_MSG.ExtraDataIndex != (uint)Marshal.ReadInt32(data_, i * num + 20))
			{
				continue;
			}
			int num2 = Math.Min((int)pASSTHRU_MSG.DataSize, 4128);
			for (int j = 0; j < num2; j++)
			{
				pASSTHRU_MSG.Data[j] = Marshal.ReadByte(data_, i * num + 24 + j);
			}
			bool flag = true;
			for (int k = 0; k < Length; k++)
			{
				if (pASSTHRU_MSG.Data[k] != Marshal.ReadByte(data_, i * num + 24 + k))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return i;
			}
		}
		return -1;
	}

	public int Add(object value)
	{
		throw new NotSupportedException();
	}

	public void CopyTo(Array array, int index)
	{
		throw new NotSupportedException();
	}

	public IEnumerator GetEnumerator()
	{
		return new MessageEnumerator(this);
	}
}
