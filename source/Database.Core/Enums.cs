using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Database.Core
{
	public abstract class Enums<TEnum>
	{
		public static T Parse<T>(string value)
			where T : struct, TEnum
		{
			return Parse<T>(value, true);
		}

		public static T Parse<T>(string value, bool ignoreCase)
			where T : struct, TEnum
		{
			var result = (T)Enum.Parse(typeof(T), value, ignoreCase);

			return result;
		}

		public static bool TryParse<T>(string value, out T result)
			where T : struct, TEnum
		{
			return TryParse(value, true, out result);
		}

		public static bool TryParse<T>(string value, bool ignoreCase, out T result)
			where T : struct, TEnum
		{
			return Enum.TryParse(value, ignoreCase, out result);
		}

		public static IEnumerable<T> GetValues<T>()
			where T : struct, TEnum
		{
			return Enum.GetValues(typeof (T)).Cast<T>();
		}

		public static IEnumerable<MemberInfo> GetMembers<T>()
			where T : struct, TEnum
		{
			var type = typeof (T);

			var result = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

			return result;
		}
	}

	public abstract class Enums : Enums<Enum>
	{
	}
}
