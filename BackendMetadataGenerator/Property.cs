using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BackendMetadataGenerator
{
	[DebuggerDisplay("{Name}")]
	public class Property
	{
		public bool IsArray;
		public string Name;
		public Property ParentNode;
		public List<Property> Properties;
		public PropertyInfo RefProperty;
		public string TypeName;

		public bool HasSubProps
		{
			get
			{
				return Properties.Any(x => x.Properties != null && x.Properties.Count > 0);
			}
		}

		public bool HasSubPropsArray
		{
			get
			{
				return Properties.Any(x => x.Properties.Any(property => property.IsArray));
			}
		}

		public string XPathName
		{
			get
			{
				var result = Name;
				if (IsArray)
				{
					var arrayItemName = GetArrayItemName();
					if (arrayItemName != null)
					{
						result += "/" + arrayItemName;
					}
				}
				return result;
			}
		}

		public bool HasParent(Property parentProperty)
		{
			if (parentProperty == null) return false;
			Property p;
			for (p = this; p != null; p = p.ParentNode)
			{
				if (p.TypeName == parentProperty.TypeName) return true;
			}

			return false;
		}

		// ReSharper disable once MemberCanBePrivate.Local
		public string GetArrayItemName()
		{
			if (!IsArray) throw new InvalidOperationException();
			var name = RefProperty.PropertyType.Name;
			if (RefProperty.PropertyType.FullName.StartsWith("System."))
			{
				name = name.ToLower();
			}
			//return null;
			return name.Substring(0, name.Length - 2);
		}
	}
}