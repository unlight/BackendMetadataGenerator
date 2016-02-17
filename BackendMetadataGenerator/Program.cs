using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace BackendMetadataGenerator
{
	internal class Program
	{
		private static Assembly _assembly;

		// ReSharper disable once UnusedParameter.Local
		private static void Main(string[] args)
		{
			_assembly = Utils.GetAssembly();
			List<MethodInfo> methods = Utils.GetMethods(_assembly);
			foreach (MethodInfo method in methods)
			{
				//if (method.Name != "GetDealsById") continue;
				SaveArrayProperties(method);
			}
		}

		private static void SaveArrayProperties(MethodInfo method)
		{
			var fullName = method.ReturnType.FullName;
			if (fullName.EndsWith("[]"))
			{
				fullName = fullName.Substring(0, fullName.Length - 2);
			}

			var type1 = _assembly.GetType(fullName);
			if (type1 == null) return;
			var properties = GetProperties(type1, null);
			var arrayProperties = new List<string>();
			foreach (var property in properties)
			{
				arrayProperties.Add("/" + property.Name);
				var list = PrintProperty("", property);
				arrayProperties.AddRange(list);
			}

			var data = new Dictionary<string, object>();
			data["arrayProperties"] = arrayProperties.Distinct();
			var serializer = new JavaScriptSerializer();
			var json = serializer.Serialize(data);
			string filename = string.Format("{0}.json", method.Name);
			File.WriteAllText(filename, json);
		}

		private static List<string> PrintProperty(string parent, Property p)
		{
			var result = new List<string>();

			//result.Add(parent + "/" + p.Name);
			if (!string.IsNullOrEmpty(parent))
			{
				result.Add(parent + "/" + p.Name);
			}

			if (p.Properties != null)
			{
				p.Properties.Where(x =>
				{
					var isArr = x.IsArray;
					if (!isArr)
					{
						if (x.Properties == null || x.Properties.Count == 0) return false;
						if (!x.HasSubProps) return false;
						if (!x.HasSubPropsArray) return false;
					}
					return true;
				}).ToList().ForEach(property =>
				{
					if (p.XPathName.Contains("AgentsLawyerName"))
					{
						//Debugger.Break();
					}
					var list = PrintProperty(parent + "/" + p.XPathName, property);
					result.AddRange(list);
				});
			}
			return result;
		}

		private static List<Property> GetProperties(Type type, Property parentNode)
		{
			if (type == null) return null;
			var result = new List<Property>();
			var props = type.GetProperties().ToList();
			foreach (var propertyInfo in props)
			{
				var hasIgnore =
					propertyInfo.GetCustomAttributes().Any(attribute => attribute.GetType() == typeof (XmlIgnoreAttribute));
				if (hasIgnore) continue;
				//XmlIgnoreAttribute
				var p = GetPropertyInfo(propertyInfo);
				var hasParent = p.HasParent(parentNode);
				if (hasParent) continue;

				var subtType = _assembly.GetType(p.TypeName);
				if (subtType != null)
				{
					var subProperties = GetProperties(subtType, p);
					var includedTypes = subtType.GetCustomAttributes()
						.Where(attribute => attribute.GetType().Name == "XmlIncludeAttribute")
						.Select(attribute => ((XmlIncludeAttribute) (attribute)).Type)
						.Select(type1 => GetProperties(type1, p))
						.ToList();

					foreach (var includedType in includedTypes)
					{
						includedType.ForEach(property => property.ParentNode = p);
						subProperties.AddRange(includedType);
					}
					p.Properties = subProperties;
				}

				result.Add(p);
			}

			return result;
		}

		private static Property GetPropertyInfo(PropertyInfo propertyInfo)
		{
			var fullName = propertyInfo.PropertyType.FullName;
			var isArray = propertyInfo.PropertyType.BaseType != null && propertyInfo.PropertyType.BaseType.Name == "Array";
			if (isArray)
			{
				fullName = fullName.Substring(0, fullName.Length - 2);
			}
			if (propertyInfo.Name == "AgentsLawyerName")
			{
				//Debugger.Break();
			}

			return new Property
			{
				TypeName = fullName,
				Name = propertyInfo.Name,
				IsArray = isArray,
				Properties = new List<Property>(),
				RefProperty = propertyInfo
			};
		}
	}
}