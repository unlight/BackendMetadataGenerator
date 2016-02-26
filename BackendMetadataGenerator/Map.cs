using System;
using System.Collections.Generic;

namespace BackendMetadataGenerator
{
	internal static class Map
	{
		static public Dictionary<Type, string> BuiltInTypes = new Dictionary<Type, string>
		{
			{typeof (Byte), "byte"},
			{typeof (Boolean), "boolean"},
			{typeof (Char), "char"},
			{typeof (Decimal), "decimal"},
			{typeof (Double), "double"},
			{typeof (Single), "float"},
			{typeof (Int32), "int"},
			{typeof (Int64), "long"},
			{typeof (SByte), "sbyte"},
			{typeof (Int16), "short"}
		};
	}
}