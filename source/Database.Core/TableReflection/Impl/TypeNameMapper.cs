using System;
using System.Collections.Generic;

namespace Database.Core.TableReflection.Impl
{
	public class TypeNameMapper : ITypeNameMapper
	{
		private readonly IDictionary<DatabaseType, ITypeNameMapper> _typeNameMappers;

		public TypeNameMapper()
		{
			_typeNameMappers = new Dictionary<DatabaseType, ITypeNameMapper>
			{
				{ DatabaseType.SqlServer, new SqlServerTypeNameMapper() },
				{ DatabaseType.MySql, new MySqlTypeNameMapper() }
			};
		}

		public Type GetType(DatabaseType databaseType, string typeName)
		{
			if (String.IsNullOrWhiteSpace(typeName))
			{
				throw new ArgumentException(String.Format("'{0}' is not a valid type name.", typeName), typeName);
			}

			ITypeNameMapper tableReflector;
			if (_typeNameMappers.TryGetValue(databaseType, out  tableReflector) == false)
			{
				throw new ArgumentException(String.Format("No ITableReflector defined for database type '{0}'.", databaseType));
			}

			return tableReflector.GetType(databaseType, typeName);
		}
	}
}
