using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace BackendMetadataGenerator
{
	[DebuggerDisplay("{Name}")]
	public class Property
	{
		public Property(string name)
		{
			Name = name;
		}

		public Property(PropertyInfo propertyInfo, Property parent = null)
		{
			PropertyInfo = propertyInfo;
			Parent = parent;
			Name = propertyInfo.Name;
			var type = propertyInfo.PropertyType;
			IsArray = type.BaseType == typeof (Array);
			if (IsArray)
			{
				type = type.GetElementType();
				ArrayItemName = type.Name;
			}
			if (type.FullName.StartsWith("System."))
			{
				if (type.FullName.StartsWith("System.Nullable"))
				{
					IsNullable = true;
					type = type.GetGenericArguments().First();
				}
				if (Map.BuiltInTypes.ContainsKey(type))
				{
					ArrayItemName = Map.BuiltInTypes[type];
				}
				return;
			}
			Type = type;
			Properties = Program.GetProperties(type, this);
		}

		public string Name { get; set; }

		public bool IsArray { get; set; }

		public string ArrayItemName { get; set; }

		public Property Parent { get; set; }

		public List<Property> Properties { get; set; }

		public PropertyInfo PropertyInfo { get; set; }

		public string TypeName { get; set; }

		public bool IsNullable { get; set; }

		protected Type Type { get; set; }

		public List<Type> IncludedTypes
		{
			get
			{
				var includedTypes = Type.GetCustomAttributes()
					.OfType<XmlIncludeAttribute>()
					.Select(attribute => attribute.Type)
					.ToList();
				return includedTypes;
			}
		}

		public string XPathName
		{
			get
			{
				var result = Name;
				if (IsArray)
				{
					result = result + "/" + ArrayItemName;
				}
				if (Parent != null)
				{
					result = Parent.XPathName + "/" + result;
				}
				return result;
			}
		}

		public List<Property> ChildProperties
		{
			get
			{
				var result = new List<Property>();
				if (Properties != null)
				{
					Properties.ForEach(p =>
					{
						result.Add(p);
						result.AddRange(p.ChildProperties);
					});
				}
				return result;
			}
		}
	}
}