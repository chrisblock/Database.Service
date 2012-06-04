using System;
using System.Collections.Generic;
using System.Reflection;

using Database.Core.TableReflection;

namespace Database.Core.TypeBuilding.Impl
{
	public class EntityTypeBuilder : ITypeBuilder
	{
		private readonly IDynamicAssemblyBuilder _dynamicAssemblyBuilder;

		public EntityTypeBuilder(IDynamicAssemblyBuilder dynamicAssemblyBuilder)
		{
			_dynamicAssemblyBuilder = dynamicAssemblyBuilder;
		}

		public Type Build(TableDefinition table)
		{
			var entityName = _dynamicAssemblyBuilder.BuildAssemblyQualifiedTypeName(table.GetEntityName());

			var typeBuilder = _dynamicAssemblyBuilder.BuildType(entityName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

			var identityProperties = new List<PropertyInfo>();

			foreach (var column in table.Columns)
			{
				var property = typeBuilder.DefineProperty(column.Name, column.Type);

				if (column.IsPrimaryKeyColumn)
				{
					identityProperties.Add(property);
				}
			}

			if (identityProperties.Count > 0)
			{
				// TODO: i don't think these three methods should be extension methods...
				typeBuilder.DefineGetHashCodeMethod(identityProperties);

				var virtualEqualsMethod = typeBuilder.DefineVirtualEqualsMethod(identityProperties);

				typeBuilder.DefineOverrideEqualsMethod(virtualEqualsMethod, identityProperties);
			}
			else
			{
				throw new NotSupportedException(String.Format("No primary key columns found for table '{0}'.", entityName));
			}

			var newType = typeBuilder.CreateType();

			return newType;
		}
	}
}
