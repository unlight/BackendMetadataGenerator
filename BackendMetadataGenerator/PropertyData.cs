using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BackendMetadataGenerator
{
	[DebuggerDisplay("{Name}")]
	public class PropertyData
	{
		private List<Attribute> _customAttributes;
		
		public string Name { get; set; }

		public List<Attribute> CustomAttributes
		{
			get { return _customAttributes ?? new List<Attribute>(); }
			set { _customAttributes = value; }
		}

		public Type Type { get; set; }
	}
}