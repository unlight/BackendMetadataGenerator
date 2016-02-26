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
		private List<Property> _properties;

		public Property(string name)
		{
			Name = name;
		}

		public Property(PropertyInfo propertyInfo, Property parent = null)
		{
			PropertyInfo = propertyInfo;
			Parent = parent;
			Name = propertyInfo.Name;
			Type = propertyInfo.PropertyType;
			IsArray = Type.BaseType == typeof(Array);
			if (IsArray)
			{
				Type = Type.GetElementType();
				ArrayItemName = Type.Name;
			}
			if (Type.FullName.StartsWith("System."))
			{
				if (Type.FullName.StartsWith("System.Nullable"))
				{
					IsNullable = true;
					Type = Type.GetGenericArguments().First();
				}
				if (Map.BuiltInTypes.ContainsKey(Type))
				{
					ArrayItemName = Map.BuiltInTypes[Type];
				}
			}
		}

		public string Name { get; set; }

		public bool IsArray { get; set; }

		public string ArrayItemName { get; set; }

		public Property Parent { get; set; }

		public List<Property> Properties
		{
			get { return _properties ?? new List<Property>(); }
			set { _properties = value; }
		}

		public PropertyInfo PropertyInfo { get; set; }

		public bool IsNullable { get; set; }

		public Type Type { get; set; }

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

		public string JavaScriptType
		{
			get
			{
				string result = null;
				if (Type != null && Map.JavaScriptTypes.ContainsKey(Type))
				{
					result = Map.JavaScriptTypes[Type];
				}
				return result;
			}
		}
	}
}