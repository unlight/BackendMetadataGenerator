using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;

namespace BackendMetadataGenerator
{
	internal static class Utils
	{

		public static List<MethodInfo> GetMethods(Assembly assembly)
		{
			var result = new List<MethodInfo>();
			var methods = assembly.GetTypes().SelectMany(type => type.GetMethods());
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (MethodInfo method in methods)
			{
				var customAttributes = method.GetCustomAttributes();
				if (customAttributes != null && customAttributes.Any(attribute => attribute.GetType().Name == "SoapDocumentMethodAttribute"))
				{
					result.Add(method);
				}
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
			foreach (string path in Directory.GetFiles("./", "*.cs"))
			{
				string code = File.ReadAllText(path)
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
	}
}
