using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;

using StructureMap;

namespace Database.Service
{
	public class StructureMapDependencyScope : IDependencyScope
	{
		private readonly IContainer _container;

		public StructureMapDependencyScope(IContainer container)
		{
			_container = container;
		}

		public object GetService(Type serviceType)
		{
			object result = null;

			if (serviceType.IsInterface || serviceType.IsAbstract)
			{
				result = _container.TryGetInstance(serviceType);
			}
			else if (serviceType.Assembly.GlobalAssemblyCache == false)
			{
				result = _container.GetInstance(serviceType);
			}

			return result;
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			return _container.GetAllInstances(serviceType)
				.Cast<object>();
		}

		public void Dispose()
		{
			_container.Dispose();
		}
	}
}
