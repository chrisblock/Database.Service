using System;
using System.Configuration.Assemblies;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Database.Core.TypeBuilding
{
	public class DynamicAssembly
	{
		public const TypeAttributes DefaultTypeAttributes = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.BeforeFieldInit;

		public AssemblyName AssemblyName { get; private set; }

		private readonly Lazy<AssemblyBuilder> _lazyAssemblyBuilder;
		public AssemblyBuilder AssemblyBuilder { get { return _lazyAssemblyBuilder.Value; } }

		private readonly Lazy<ModuleBuilder> _lazyModuleBuilder;
		public ModuleBuilder ModuleBuilder { get { return _lazyModuleBuilder.Value; } }

		public bool IsPersisted { get; private set; }

		public DynamicAssembly(string assemblyName, bool isForSaving = false)
		{
			AssemblyName = CreateAssemblyName(assemblyName);

			IsPersisted = isForSaving;

			_lazyAssemblyBuilder = new Lazy<AssemblyBuilder>(CreateAssemblyBuilder, LazyThreadSafetyMode.ExecutionAndPublication);
			_lazyModuleBuilder = new Lazy<ModuleBuilder>(CreateModuleBuilder, LazyThreadSafetyMode.ExecutionAndPublication);
		}

		private AssemblyName CreateAssemblyName(string assemblyName)
		{
			var currentAssembly = GetType().Assembly;
			var currentAssemblyName = currentAssembly.GetName();

			var uriToCurrentDll = new Uri(currentAssembly.CodeBase);
			var currentDll = new FileInfo(uriToCurrentDll.LocalPath);

			return new AssemblyName
			{
				Name = assemblyName,
				CodeBase = Path.Combine(String.Format("{0}", currentDll.Directory), String.Format("{0}.dll", assemblyName)),
				CultureInfo = currentAssemblyName.CultureInfo,
				HashAlgorithm = AssemblyHashAlgorithm.SHA1,
				Version = currentAssemblyName.Version
			};
		}

		private AssemblyBuilder CreateAssemblyBuilder()
		{
			var dllPath = GetAssemblyFolder();

			return IsPersisted
				? Thread.GetDomain().DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.RunAndSave, dllPath)
				: Thread.GetDomain().DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run);
		}

		private ModuleBuilder CreateModuleBuilder()
		{
			ModuleBuilder result;

			if (IsPersisted)
			{
				var dllName = GetAssemblyFileName();

				if (dllName == null)
				{
					throw new ArgumentException(String.Format("Could not get CodeBase file name for assembly '{0}'.", AssemblyName));
				}

				result = AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, dllName, true);
			}
			else
			{
				result = AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, true);
			}

			return result;
		}

		private string GetAssemblyFilePath()
		{
			var codeBaseUri = new Uri(AssemblyName.CodeBase);

			return codeBaseUri.LocalPath;
		}

		private string GetAssemblyFolder()
		{
			return Path.GetDirectoryName(GetAssemblyFilePath());
		}

		private string GetAssemblyFileName()
		{
			return Path.GetFileName(GetAssemblyFilePath());
		}

		public string BuildAssemblyQualifiedTypeName(string typeName)
		{
			return AssemblyName.BuildAssemblyQualifiedTypeName(typeName);
		}

		public TypeBuilder CreateType(string name, TypeAttributes typeAttributes = DefaultTypeAttributes, Type baseType = null)
		{
			// TODO: use the Assembly and Module name's to build a fully namespaced typename? Or maybe namespace it like this: <Server>.<Database>.<Table>?

			var result = (baseType == null)
				? ModuleBuilder.DefineType(name, typeAttributes)
				: ModuleBuilder.DefineType(name, typeAttributes, baseType);

			return result;
		}

		public Type GetDynamicType(string typeName)
		{
			return AssemblyBuilder.GetType(typeName);
		}

		public void Save()
		{
			if (IsPersisted == true)
			{
				AssemblyBuilder.Save(GetAssemblyFilePath());
			}
		}
	}
}
