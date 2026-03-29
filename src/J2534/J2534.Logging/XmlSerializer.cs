using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace J2534.Logging;

public class XmlSerializer
{
	public static ECUParameters ReadObject(string fileName)
	{
		FileStream fileStream = new FileStream(fileName, FileMode.Open);
		XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateTextReader(fileStream, new XmlDictionaryReaderQuotas());
		ECUParameters result = (ECUParameters)new DataContractSerializer(typeof(ECUParameters)).ReadObject(xmlDictionaryReader, verifyObjectName: true);
		xmlDictionaryReader.Close();
		fileStream.Close();
		return result;
	}

	public static ECUParameters ReadTextObject(string data)
	{
		DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(ECUParameters));
		using MemoryStream stream = GenerateStreamFromString(data);
		return (ECUParameters)dataContractSerializer.ReadObject(stream);
	}

	private static MemoryStream GenerateStreamFromString(string value)
	{
		return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
	}
}
