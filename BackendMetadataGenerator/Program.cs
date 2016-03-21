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
				var whiteList = new List<string>()
				{
					//"GetMarketDataDealSummary",
					"GetCompanyIPOProfiles",
					//"GetDealsById",
					//"GetFundRaisingReport",
				};
				if (!whiteList.Contains(method.Name)) continue;
				var data = GetMetadata(method);
				var json = GetJsonObject(data.ChildProperties);
				var filename = String.Format("{0}.json", method.Name);
				File.WriteAllText(filename, json.ToJson());
			}
		}

		private static string GetXPath(Property p)
		{
			//var key = "/Envelope/Body/" + p.XPathName;
			var key = p.XPathName;
			var keyParts = key.Split('/').ToList();
			keyParts[0] = ""; // Remove constant.
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

		public static object GetJsonObject(List<Property> properties)
		{
			return properties
				.Where(p => p.IsArray || p.JavaScriptType != null)
				.Select(p => new
				{
					xpath = GetXPath(p),
					name = p.Name,
					isArray = p.IsArray,
					parse = p.JavaScriptType
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
					.Replace("[Maelstrom.WebServices.Service", "//");
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
			var type = method.ReturnParameter.ParameterType;
			var result = new Property(type.Name);
			var data = GetProperties(type, result);
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
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var propertyInfo in type.GetProperties())
			{
				var customAttributes = propertyInfo.GetCustomAttributes();
				if (customAttributes == null) continue;
				if (customAttributes.OfType<XmlIgnoreAttribute>().Any()) continue;
				var p = new Property(propertyInfo, parent);
				var noSubProperties = (p.Type == type || p.Type.FullName.StartsWith("System."));
				if (!noSubProperties)
				{
					p.Properties = GetProperties(p.Type, p);
				}
				result.Add(p);
			}
			return result;
		}
	}
}