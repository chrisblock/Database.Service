using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;

namespace Database.Core.TableReflection.Impl
{
	public class SqlServerTypeNameMapper : ITypeNameMapper
	{
		private static IDictionary<string, Type> TypeMapping {get { return LazyTypeMapping.Value; } }

		private static readonly Lazy<IDictionary<string, Type>> LazyTypeMapping = new Lazy<IDictionary<string, Type>>(BuildTypeMapping, LazyThreadSafetyMode.ExecutionAndPublication);

		private static IDictionary<string, Type> BuildTypeMapping()
		{
			var result = new Dictionary<string, Type>
			{
				{ "bigint", typeof (long) },
				{ "binary", typeof (byte[]) },
				{ "bit", typeof (bool) },
				{ "char", typeof (string) },
				{ "date", typeof (DateTime) },
				{ "datetime", typeof (DateTime) },
				{ "datetimeoffset", typeof (DateTimeOffset) },
				{ "decimal", typeof (decimal) },
				{ "float", typeof (double) },
				{ "image", typeof (byte[]) },
				{ "int", typeof (int) },
				{ "money", typeof (decimal) },
				{ "nchar", typeof (string) },
				{ "ntext", typeof (string) },
				{ "numeric", typeof (decimal) },
				{ "nvarchar", typeof (string) },
				{ "real", typeof (float) },
				{ "rowversion", typeof (byte[]) },
				{ "smalldatetime", typeof (DateTime) },
				{ "smallint", typeof (short) },
				{ "smallmoney", typeof (decimal) },
				{ "sql_variant", typeof (object) },
				{ "text", typeof (string) },
				{ "time", typeof (TimeSpan) },
				{ "timestamp", typeof (byte[]) },
				{ "tinyint", typeof (byte) },
				{ "uniqueidentifier", typeof (Guid) },
				{ "varbinary", typeof (byte[]) },
				{ "varchar", typeof (string) },
				{ "xml", typeof (XDocument) },
			};

			return result;
		}

		public Type GetType(DatabaseType databaseType, string sqlTypeName)
		{
			Type result;
			
			if (TypeMapping.TryGetValue(sqlTypeName, out result) == false)
			{
				throw new ArgumentException(String.Format("Type '{0}' is not a recognized SQL system type.", sqlTypeName));
			}

			return result;
		}
	}
}
