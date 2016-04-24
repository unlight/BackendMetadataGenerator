using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;

namespace BackendMetadataGenerator
{
	public static class Extensions
	{
		private const string IndentString = "    ";

		public static string Join(this IEnumerable<string> data, string separator)
		{
			return String.Join(separator, data);
		}

		public static string ToJson(this object o)
		{
			var serializer = new JavaScriptSerializer();
			var result = serializer.Serialize(o);
			//result = FormatJson(result);
			return result;
		}

		public static string ToJson2(this object o)
		{
			MemoryStream memoryStream = new MemoryStream();
			var settings = new DataContractJsonSerializerSettings
			{
				EmitTypeInformation = EmitTypeInformation.Never,
				UseSimpleDictionaryFormat = true,
			};
			var serializer = new DataContractJsonSerializer(o.GetType(), settings);
			serializer.WriteObject(memoryStream, o);
			memoryStream.Position = 0;
			string result = new StreamReader(memoryStream).ReadToEnd();
			return FormatJson(result);
		}

		private static string FormatJson(string json)
		{
			var indentation = 0;
			var quoteCount = 0;
			var result =
				from ch in json
				let quotes = ch == '"' ? quoteCount++ : quoteCount
				let lineBreak =
					ch == ',' && quotes%2 == 0
						? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(IndentString, indentation))
						: null
				let openChar =
					ch == '{' || ch == '['
						? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(IndentString, ++indentation))
						: ch.ToString()
				let closeChar =
					ch == '}' || ch == ']'
						? Environment.NewLine + String.Concat(Enumerable.Repeat(IndentString, --indentation)) + ch
						: ch.ToString()
				select lineBreak ?? (openChar.Length > 1 ? openChar : closeChar);

			return String.Concat(result);
		}
	}
}