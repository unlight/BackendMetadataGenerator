using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace BackendMetadataGenerator
{
	[DataContract]
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
		public UdfChild Child { get; set; }
	}

	public class UdfChild : Dictionary<string, object>
	{
	}

	[DataContract]
	public class UdfConfig
	{
		[DataMember(Order = 1)]
		public string Version { get; set; }

		[DataMember(Order = 2)]
		public Dictionary<string, object> Request { get; set; }

		[DataMember(Order = 3)]
		public Dictionary<string, object> Error { get; set; }

		[DataMember(Order = 4)]
		public UdfResponse Response { get; set; }
	}
}