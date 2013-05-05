using System.Collections.Concurrent;

namespace Database.Core.TypeBuilding.Impl
{
	public class DynamicAssemblyManagerFactory : IDynamicAssemblyManagerFactory
	{
		// TODO: read whether or not to persist dynamic assemblies from somewhere
		private const bool PersistDynamicAssemblies = false;

		private static readonly object ManagerLocker = new object();
		private static readonly ConcurrentDictionary<string, IDynamicAssemblyManager> Managers = new ConcurrentDictionary<string, IDynamicAssemblyManager>();

		public IDynamicAssemblyManager Create(string assemblyName)
		{
			if (Managers.ContainsKey(assemblyName) == false)
			{
				lock (ManagerLocker)
				{
					if (Managers.ContainsKey(assemblyName) == false)
					{
						var dynamicAssembly = new DynamicAssembly(assemblyName, PersistDynamicAssemblies);

						var manager = new DynamicAssemblyManager(dynamicAssembly);

						Managers.TryAdd(assemblyName, manager);
					}
				}
			}

			return Managers[assemblyName];
		}
	}
}
