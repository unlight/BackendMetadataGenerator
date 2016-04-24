using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Services.Protocols;
using System.Xml.Serialization;
using Microsoft.CSharp;

namespace BackendMetadataGenerator
{
	internal class Program
	{
		private static Assembly _assembly;
		// ReSharper disable once UnusedParameter.Local
		private static void Main(string[] args)
		{
			_assembly = GetAssembly();
			var methods = GetMethods();
			foreach (var method in methods)
			{
				//if (!new List<string>()
				//{
				//	//"GetMarketDataDealSummary",
				//	"GetCompanyIPOProfiles",
				//	//"GetDealsById",
				//	//"GetFundRaisingReport",
				//	//"HeadlineOp_2",
				//	//"getSubmissionInfoByDCN",
				//}.Contains(method.Name)) continue;
				Property data = GetMetadata(method);
				var json = GetJsonObject(data.ChildProperties);
				File.WriteAllText(String.Format("{0}.json", method.Name), json.ToJson());
				var udfjson = GetUdfConfig(data);
				((Dictionary<string, object>) ((Dictionary<string, object>)
					udfjson["Request"])["Headers"])["SOAPAction"] = string.Format("\"{0}\"",data.SoapAction);
				File.WriteAllText(String.Format("{0}.udf.json", method.Name), udfjson.ToJson());
			}
		}

		public static Dictionary<string, object> GetUdfConfig(Property data)
		{
			var d = new Dictionary<string, object>()
			{
				{"Version", "1.0.0"},
				{"Request", new Dictionary<string, object>()
					{
						{"URL", "{{.env.url.tornado}}/urreq/rrurreq.dll?soaprequest"},
						{"Headers", new Dictionary<string, object>(){
							{"SOAPAction", ""},
							{"Content-Type", "text/xml; charset=utf-8"}
						}},
						{"Method", "POST"},
						{"Params", new Dictionary<string,object>()},
					}
				},
				{"Error", new Dictionary<string, object>(){{"Tornado", new Dictionary<string, object>()}}},
				{"Response", new UdfNode()}
			};
			var response = UdfSelector(data);
			d["Response"] = new Dictionary<string, object>() {{data.Name, response}};
			return d;
		}

		private static UdfResponse UdfSelector(Property p)
		{
			if (p.Properties.Count == 0)
			{
				return new UdfResponse() {datatype = p.UdfDataType, Name = p.Name};
			}
			var result = new UdfResponse {type = p.UdfType, Name = p.Name};
			result.Child = new Dictionary<string, object>();
			p.Properties.Select(UdfSelector).ToList().ForEach(r =>
			{
				result.Child.Add(r.Name, r);
			});
			return result;
		}

		public static object GetJsonObject(List<Property> properties)
		{
			return properties
				.Where(p => p.IsArray || p.JavaScriptType != null)
				.Select(p => new
				{
					xpath = p.GetXPath(),
					name = p.Name,
					parse = p.JavaScriptType,
					//isArray = p.IsArray,
					//noNested = p.NoNestedElements
				});
		}

		public static List<MethodInfo> GetMethods()
		{
			var result = new List<MethodInfo>();
			var methods = _assembly.GetTypes().SelectMany(type => type.GetMethods());
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var method in methods)
			{
				var customAttributes = method.GetCustomAttributes();
				if (!customAttributes.OfType<SoapDocumentMethodAttribute>().Any()) continue;
				result.Add(method);
			}
			return result;
		}

		public static Assembly GetAssembly()
		{
			var provider = new CSharpCodeProvider();
			var parameters = new CompilerParameters();
			parameters.ReferencedAssemblies.Add("System.dll");
			parameters.ReferencedAssemblies.Add("System.Web.Services.dll");
			parameters.ReferencedAssemblies.Add("System.Xml.dll");

			var codes = new List<string>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var path in Directory.GetFiles("./", "*.cs"))
			{
				var code = File.ReadAllText(path)
					.Replace("Maelstrom.WebServices.MaelstromSoapClientProtocol", "SoapHttpClientProtocol")
					.Replace("[Maelstrom.WebServices", "//");
				codes.Add(code);
			}
			var results = provider.CompileAssemblyFromSource(parameters, codes.ToArray());
			if (results.Errors.HasErrors)
			{
				var sb = new StringBuilder();
				foreach (CompilerError error in results.Errors)
				{
					sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
				}
				throw new InvalidOperationException(sb.ToString());
			}
			return results.CompiledAssembly;
		}

		private static Property GetMetadata(MethodInfo method)
		{
			// ReSharper disable once PossibleNullReferenceException
			var result = new Property(method.ReturnParameter.ParameterType);
			var data = GetProperties(result.Type, result);
			var xmlArrayAttribute = method.ReturnParameter.GetCustomAttributes().OfType<XmlArrayAttribute>().FirstOrDefault();
			if (xmlArrayAttribute != null && !string.IsNullOrEmpty(xmlArrayAttribute.ElementName))
			{
				result.Name = xmlArrayAttribute.ElementName;
			}
			var xmlArrayItemAttribute = method.ReturnParameter.GetCustomAttributes().OfType<XmlArrayItemAttribute>().FirstOrDefault();
			if (xmlArrayItemAttribute != null && !string.IsNullOrEmpty(xmlArrayItemAttribute.ElementName))
			{
				result.ArrayItemName = xmlArrayItemAttribute.ElementName;
			}
			var methodAttribute = method.GetCustomAttributes().OfType<SoapDocumentMethodAttribute>().FirstOrDefault();
			result.SoapAction = methodAttribute.Action;

			foreach (var property in data)
			{
				if (property.IncludedTypes.Count == 0) continue;
				foreach (var includedType in property.IncludedTypes)
				{
					var pr = property;
					var properties = GetProperties(includedType, property)
						.Where(p => pr.Properties != null && !pr.Properties.Exists(pp => pp.Name == p.Name));
					property.Properties.AddRange(properties);
				}
			}
			result.Properties = data;
			return result;
		}

		public static List<Property> GetProperties(Type type, Property parent)
		{
			var result = new List<Property>();
			var propertyData = CollectPropertyData(type);
			foreach (var info in propertyData)
			{
				if (info.CustomAttributes.OfType<XmlIgnoreAttribute>().Any()) continue;
				var p = new Property(info, parent);
				if (!p.NoSubProperties(type))
				{
					p.Properties = GetProperties(p.Type, p);
				}
				result.Add(p);
			}
			return result;
		}

		private static List<PropertyData> CollectPropertyData(Type type)
		{
			var result = new List<PropertyData>();
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var propertyInfo in type.GetProperties())
			{
				result.Add(new PropertyData()
				{
					CustomAttributes = propertyInfo.GetCustomAttributes().ToList(),
					Name = propertyInfo.Name,
					Type = propertyInfo.PropertyType
				});
			}
			var publicFields = type.GetFields().Where(f => f.IsPublic);
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var fieldInfo in publicFields)
			{
				result.Add(new PropertyData()
				{
					CustomAttributes = fieldInfo.GetCustomAttributes().ToList(),
					Name = fieldInfo.Name,
					Type = fieldInfo.FieldType
				});
			}
			return result;
		}
	}
}