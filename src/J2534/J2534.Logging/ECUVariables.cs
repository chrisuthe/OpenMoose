using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace J2534.Logging;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
[CollectionDataContract(Namespace = "http://www.ecm-tech.co.uk")]
public class ECUVariables : List<ECUVariable>
{
}
