using System;
using System.Configuration;
using System.Configuration.Assemblies;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Database.Core.TypeBuilding.Impl
{
	public class DynamicAssemblyBuilder : IDynamicAssemblyBuilder
	{
		private static readonly Lazy<bool> LazySaveAssemblyToDisk = new Lazy<bool>(ReadSaveConfigurationSetting, LazyThreadSafetyMode.ExecutionAndPublication);
		private static bool SaveAssemblyToDisk { get { return LazySaveAssemblyToDisk.Value; } }

		private static bool ReadSaveConfigurationSetting()
		{
			var setting = ConfigurationManager.AppSettings["SaveDynamicAssembly"];

			bool save;
			return Boolean.TryParse(setting, out save) && save;
		}

		private readonly AssemblyName _assemblyName;
		private readonly AssemblyBuilder _assemblyBuilder;
		private readonly ModuleBuilder _moduleBuilder;

		private readonly string _dllName;

		public DynamicAssemblyBuilder(string assemblyName)
		{
			_assemblyName = CreateAssemblyName(assemblyName);

			if (SaveAssemblyToDisk)
			{
				_dllName = String.Format("{0}.dll", _assemblyName.Name);
				var dllPath = _assemblyName.CodeBase;

				_assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave, dllPath);
				_moduleBuilder = _assemblyBuilder.DefineDynamicModule(_assemblyName.Name, _dllName, true);
			}
			else
			{
				_assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run);
				_moduleBuilder = _assemblyBuilder.DefineDynamicModule(_assemblyName.Name, true);
			}

			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
		}

		private AssemblyName CreateAssemblyName(string assemblyName)
		{
			var currentAssembly = GetType().Assembly;
			var currentAssemblyName = new AssemblyName(currentAssembly.FullName);

			var uriToCurrentDll = new Uri(currentAssembly.CodeBase);
			var currentDll = new FileInfo(uriToCurrentDll.LocalPath);

			return new AssemblyName
			{
				Name = assemblyName,
				//TODO: should CodeBase include the dll name??
				CodeBase = String.Format("{0}", currentDll.Directory),
				CultureInfo = currentAssemblyName.CultureInfo,
				HashAlgorithm = AssemblyHashAlgorithm.SHA1,
				Version = currentAssemblyName.Version
			};
		}

		// TODO: should this event handler be cleaned up by making this class disposable, etc etc????
		private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
		{
			Assembly result = null;

			if (args.Name == _assemblyBuilder.FullName)
			{
				result = _assemblyBuilder;
			}

			return result;
		}

		public string BuildAssemblyQualifiedTypeName(string className)
		{
			return String.Format("{0}.{1}", _assemblyName.Name, className);
		}

		public Type GetBuiltType(string typeName)
		{
			return _assemblyBuilder.GetType(typeName);
		}

		public TypeBuilder BuildType(string typeName, TypeAttributes typeAttributes, Type baseClassType = null)
		{
			var result = (baseClassType == null)
				? _moduleBuilder.DefineType(typeName, typeAttributes)
				: _moduleBuilder.DefineType(typeName, typeAttributes, baseClassType);

			if (SaveAssemblyToDisk)
			{
				_assemblyBuilder.Save(_dllName);
			}

			return result;
		}
	}
}
