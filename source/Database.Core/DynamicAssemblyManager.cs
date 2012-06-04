using System;
using System.Collections.Concurrent;
using System.Threading;

using Database.Core.TableReflection;
using Database.Core.TypeBuilding;
using Database.Core.TypeBuilding.Impl;

namespace Database.Core
{
	public static class DynamicAssemblyManager
	{
		private static readonly Lazy<IDynamicAssemblyBuilder> LazyDynamicAssemblyBuilder = new Lazy<IDynamicAssemblyBuilder>(() => new DynamicAssemblyBuilder("Database.DynamicMappings"), LazyThreadSafetyMode.ExecutionAndPublication);
		private static IDynamicAssemblyBuilder DynamicAssemblyBuilder { get { return LazyDynamicAssemblyBuilder.Value; } }

		private static readonly Lazy<ITypeBuilder> LazyEntityTypeBuilder = new Lazy<ITypeBuilder>(() => new EntityTypeBuilder(DynamicAssemblyBuilder), LazyThreadSafetyMode.ExecutionAndPublication);
		private static ITypeBuilder EntityTypeBuilder { get { return LazyEntityTypeBuilder.Value; } }

		private static readonly Lazy<ITypeBuilder> LazyMapTypeBuilder = new Lazy<ITypeBuilder>(() => new MapTypeBuilder(DynamicAssemblyBuilder), LazyThreadSafetyMode.ExecutionAndPublication);
		private static ITypeBuilder MapTypeBuilder { get { return LazyMapTypeBuilder.Value; } }

		private static readonly object TypeLocker = new object();
		private static readonly ConcurrentDictionary<string, Type> Types = new ConcurrentDictionary<string, Type>();

		public static Tuple<Type, Type> BuildTypesForTable(TableDefinition table)
		{
			var entityType = BuildEntityType(table);
			var mappingType = BuildMappingType(table);

			return new Tuple<Type, Type>(entityType, mappingType);
		}

		private static Type BuildEntityType(TableDefinition table)
		{
			var entityName = DynamicAssemblyBuilder.BuildAssemblyQualifiedTypeName(table.GetEntityName());

			if (Types.ContainsKey(entityName) == false)
			{
				lock (TypeLocker)
				{
					if (Types.ContainsKey(entityName) == false)
					{
						var newType = EntityTypeBuilder.Build(table);

						Types.TryAdd(entityName, newType);
					}
				}
			}

			return Types[entityName];
		}

		private static Type BuildMappingType(TableDefinition table)
		{
			var mapName = DynamicAssemblyBuilder.BuildAssemblyQualifiedTypeName(table.GetMapName());

			if (Types.ContainsKey(mapName) == false)
			{
				lock (TypeLocker)
				{
					if (Types.ContainsKey(mapName) == false)
					{
						var newType = MapTypeBuilder.Build(table);

						Types.TryAdd(mapName, newType);
					}
				}
			}

			return Types[mapName];
		}
	}
}
