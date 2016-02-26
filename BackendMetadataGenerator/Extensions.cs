using System;
using System.Collections.Generic;

namespace BackendMetadataGenerator
{
	public static class Extensions
	{
		public static string Join(this IEnumerable<string> data, string separator)
		{
			return String.Join(separator, data);
		}
	}
}