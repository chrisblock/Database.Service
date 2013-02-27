using System;
using System.Collections.Generic;
using System.Linq;

namespace Database.Core
{
	public abstract class Enums<TEnum>
	{
		public static T Parse<T>(string value)
			where T : struct, TEnum
		{
			var result = (T) Enum.Parse(typeof (T), value);

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
		{
			return Enum.GetValues(typeof (T)).Cast<T>();
		}
	}

	public abstract class Enums : Enums<Enum>
	{
	}
}
