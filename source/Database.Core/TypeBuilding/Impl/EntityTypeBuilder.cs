﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Database.Core.TableReflection;

namespace Database.Core.TypeBuilding.Impl
{
	public class EntityTypeBuilder : IEntityTypeBuilder
	{
		private readonly DynamicAssembly _dynamicAssembly;

		public EntityTypeBuilder(DynamicAssembly dynamicAssembly)
		{
			_dynamicAssembly = dynamicAssembly;
		}

		public Type Build(TableDefinition table)
		{
			var entityName = _dynamicAssembly.BuildAssemblyQualifiedTypeName(table.GetEntityName());

			var typeBuilder = _dynamicAssembly.CreateType(entityName);

			var identityProperties = new List<PropertyInfo>();

			foreach (var column in table.Columns)
			{
				var property = typeBuilder.DefineProperty(column.Name, column.Type);

				if (column.IsPrimaryKeyColumn)
				{
					identityProperties.Add(property);
				}
			}

			if (identityProperties.Any())
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
