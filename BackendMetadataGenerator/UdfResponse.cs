using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace BackendMetadataGenerator
{
	[DataContract]
	[Serializable]
	public class UdfResponse
	{
		[XmlIgnore]
		[ScriptIgnore]
		public string Name { get; set; }
		
		// ReSharper disable once InconsistentNaming
		[DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
		public string type { get; set; }

		// ReSharper disable once InconsistentNaming
		[DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
		public string datatype { get; set; }

		[DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
		public Dictionary<string, object> Child { get; set; }
	}

	public class UdfNode
	{
		// ReSharper disable once InconsistentNaming
		public string datatype { get; set; }
	}
}