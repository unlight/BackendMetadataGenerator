using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.Services.Protocols;
using System.Xml.Serialization;
using Microsoft.CSharp;

namespace BackendMetadataGenerator
{
	internal class Program
	{
		private static Assembly _assembly;
		private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();
		// ReSharper disable once UnusedParameter.Local
		private static void Main(string[] args)
		{
			_assembly = GetAssembly();
			var methods = GetMethods();
			foreach (var method in methods)
			{
				//if (method.Name != "GetDealsById") continue;
				var data = GetMetadata(method);
				var filename = String.Format("{0}.json", method.Name);
				var json = data.ChildProperties.ToDictionary(p => p.XPathName, p => new
				{
					name = p.Name,
					isArray = p.IsArray,
					type = p.JavaScriptType
				});
				File.WriteAllText(filename, Serializer.Serialize(json));
			}
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
					var properties = GetProperties(includedType, property)
						.Where(p => !property.Properties.Exists(pr => pr.Name == p.Name));
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
				result.Add(p);
			}
			return result;
		}
	}
}