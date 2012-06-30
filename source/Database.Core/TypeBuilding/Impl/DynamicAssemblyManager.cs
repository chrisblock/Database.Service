using System;
using System.Collections.Concurrent;
using System.Reflection;

using Database.Core.TableReflection;

namespace Database.Core.TypeBuilding.Impl
{
	public class DynamicAssemblyManager : IDynamicAssemblyManager
	{
		private readonly DynamicAssembly _dynamicAssembly;
		private readonly IEntityTypeBuilder _entityTypeBuilder;
		private readonly IMappingTypeBuilder _mappingTypeBuilder;

		private readonly object _typeLocker = new object();
		private readonly ConcurrentDictionary<string, Type> _types = new ConcurrentDictionary<string, Type>();

		public DynamicAssemblyManager(DynamicAssembly dynamicAssembly)
		{
			_dynamicAssembly = dynamicAssembly;

			_entityTypeBuilder = new EntityTypeBuilder(dynamicAssembly);
			_mappingTypeBuilder = new MappingTypeBuilder(dynamicAssembly);

			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
		}

		// TODO: should this event handler be cleaned up by making this class disposable, etc etc????
		private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
		{
			Assembly result = null;

			if (args.Name == _dynamicAssembly.AssemblyBuilder.FullName)
			{
				result = _dynamicAssembly.AssemblyBuilder;
			}

			return result;
		}

		public EntityTypes BuildTypesFor(TableDefinition tableDefinition)
		{
			var entityType = CreateEntityType(tableDefinition);
			var mappingType = CreateMappingType(tableDefinition);

			return new EntityTypes
			{
				EntityType = entityType,
				MappingType = mappingType,
				TableDefinition = tableDefinition
			};
		}

		private Type CreateEntityType(TableDefinition tableDefinition)
		{
			var typeName = tableDefinition.GetEntityName();

			if (_types.ContainsKey(typeName) == false)
			{
				lock (_typeLocker)
				{
					if (_types.ContainsKey(typeName) == false)
					{
						var type = _entityTypeBuilder.Build(tableDefinition);

						_types.TryAdd(typeName, type);
					}
				}
			}

			return _types[typeName];
		}

		private Type CreateMappingType(TableDefinition tableDefinition)
		{
			var typeName = tableDefinition.GetMapName();

			if (_types.ContainsKey(typeName) == false)
			{
				lock (_typeLocker)
				{
					if (_types.ContainsKey(typeName) == false)
					{
						var type = _mappingTypeBuilder.Build(tableDefinition);

						_types.TryAdd(typeName, type);
					}
				}
			}

			return _types[typeName];
		}
	}
}
