using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace J2534.Logging;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
[DataContract(Namespace = "http://www.ecm-tech.co.uk")]
public class ECUParameters
{
	[DataMember]
	public string swNumber;

	[DataMember]
	public bool displayTime;

	[DataMember(Name = "ECUVariables")]
	public ECUVariables ecuVars { get; set; }

	public ECUParameters()
	{
		ecuVars = new ECUVariables();
	}

	public static ECUParameters ReadObject(string fileName)
	{
		FileStream fileStream = new FileStream(fileName, FileMode.Open);
		XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateTextReader(fileStream, new XmlDictionaryReaderQuotas());
		ECUParameters result = (ECUParameters)new DataContractSerializer(typeof(ECUParameters)).ReadObject(xmlDictionaryReader, verifyObjectName: true);
		xmlDictionaryReader.Close();
		fileStream.Close();
		return result;
	}

	public int getTotalDataLength()
	{
		int num = 0;
		int num2 = 0;
		foreach (ECUVariable ecuVar in ecuVars)
		{
			if (ecuVar.word)
			{
				num++;
			}
			else
			{
				num2++;
			}
		}
		return num * 2 + num2;
	}
}
