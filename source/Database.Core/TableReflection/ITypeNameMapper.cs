using System;

namespace Database.Core.TableReflection
{
	public interface ITypeNameMapper
	{
		Type GetType(DatabaseType databaseType, string typeName);
	}
}
