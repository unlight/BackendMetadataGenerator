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

		public static Dictionary<Type, string> JavaScriptTypes = new Dictionary<Type, string>
		{
			{typeof (Byte), "number"},
			{typeof (Decimal), "number"},
			{typeof (Double), "number"},
			{typeof (Single), "number"},
			{typeof (Int32), "number"},
			{typeof (UInt32), "number"},
			{typeof (Int64), "number"},
			{typeof (UInt64), "number"},
			{typeof (SByte), "number"},
			{typeof (Int16), "number"},
			{typeof (UInt16), "number"},
			{typeof (Boolean), "boolean"},
			{typeof (DateTime), "date"}
		};
	}
}