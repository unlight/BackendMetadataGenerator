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

		public Property(Type type)
			: this(new PropertyData {Type = type})
		{
			Name = type.Name;
		}

		public Property(PropertyData propertyData, Property parent = null)
		{
			PropertyData = propertyData;
			Parent = parent;
			Name = propertyData.Name;
			Type = propertyData.Type;
			IsArray = Type.BaseType == typeof (Array);
			if (IsArray)
			{
				Type = Type.GetElementType();
				ArrayItemName = Type.Name;
				var attribute = propertyData.CustomAttributes.OfType<XmlArrayItemAttribute>().FirstOrDefault();
				if (attribute != null && !string.IsNullOrEmpty(attribute.ElementName))
				{
					ArrayItemName = attribute.ElementName;
				}
			}
			IsEnum = Type.BaseType == typeof (Enum);

			// Check XmlArray attributes and fix name.
			var xmlArrayAttribute = propertyData.CustomAttributes.OfType<XmlArrayAttribute>().FirstOrDefault();
			if (xmlArrayAttribute != null && !string.IsNullOrEmpty(xmlArrayAttribute.ElementName))
			{
				Name = xmlArrayAttribute.ElementName;
			}
			var xmlArrayItemAttribute = propertyData.CustomAttributes.OfType<XmlArrayItemAttribute>().FirstOrDefault();
			if (xmlArrayItemAttribute != null && !string.IsNullOrEmpty(xmlArrayItemAttribute.ElementName))
			{
				ArrayItemName = xmlArrayItemAttribute.ElementName;
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

		public string SoapAction { get; set; }
		public string Name { get; set; }
		public bool IsArray { get; set; }
		public bool IsEnum { get; set; }
		public string ArrayItemName { get; set; }
		public Property Parent { get; set; }

		public List<Property> Properties
		{
			get { return _properties ?? new List<Property>(); }
			set { _properties = value; }
		}

		public PropertyData PropertyData { get; set; }
		public bool IsNullable { get; set; }
		public Type Type { get; set; }

		public bool IsAttribute
		{
			get
			{
				var attribute = PropertyData.CustomAttributes.OfType<XmlAttributeAttribute>().FirstOrDefault();
				return attribute != null;
			}
		}

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
				if (IsAttribute)
				{
					result = "@" + result;
				}
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
				if (IsArray) return "array";
				if (Type != null && Map.JavaScriptTypes.ContainsKey(Type))
				{
					result = Map.JavaScriptTypes[Type];
				}
				return result;
			}
		}

		public bool NoNestedElements
		{
			get
			{
				var result = false;
				if (Properties.Count == 0) return true;
				if (ChildProperties.Count > 0)
				{
					result = true;
					var hasAttrs = ChildProperties.Any(p => p.PropertyData.CustomAttributes.OfType<XmlAttributeAttribute>().Any());
					if (hasAttrs && ChildProperties.All(p => p.Properties.Count == 0))
					{
						result = true;
					}
				}
				return result;
			}
		}

		public string UdfType
		{
			get
			{
				if (IsArray) return "array";
				return "navigation";
			}
		}

		public string UdfDataType
		{
			get
			{
				string result = null;
				if (IsArray) return result;
				var type = this.Type.Name.ToLower();
				switch (type)
				{
					case "string":
						break;
					case "boolean":
						result = "bool";
						break;
					case "float":
					case "int64":
					case "double":
					case "int32":
					case "decimal":
					case "byte":
					case "single":
					case "sbyte":
					case "int16":
					case "uint16":
					case "uint32":
					case "uint64":
						result = "float";
						break;
					case "datetime":
						result = "customRFCDate[RFC]";
						break;
					default:
						break;
				}
				if (IsNullable)
				{
					switch (result)
					{
						case "float":
						case "datetime":
							result += "Nullable";
							break;
						default:
							result = null;
							break;
					}
				}

				return result;
			}
		}

		public bool NoSubProperties(Type type)
		{
			if (Type == type) return true;
			if (Type.FullName.StartsWith("System.")) return true;
			if (IsEnum) return true;
			return false;
		}

		public string GetXPath()
		{
			var p = this;
			//var key = "/Envelope/Body/" + p.XPathName;
			var key = p.XPathName;
			var keyParts = key.Split('/').ToList();
			keyParts[0] = ""; // Remove constant.
			if (p.IsAttribute && p.Parent != null && p.Parent.IsArray)
			{
				keyParts.RemoveAt(keyParts.Count - 2);
			}
			if (p.IsArray)
			{
				if (keyParts.Last() != p.Name)
				{
					// Remove last item.
					keyParts.RemoveAt(keyParts.Count - 1);
				}
			}
			key = keyParts.Join("/");
			return key;
		}
	}
}