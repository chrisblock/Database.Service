using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

using FluentNHibernate.Mapping;

namespace Database.Core
{
	public class DynamicAssemblyCreator
	{
		private static readonly MethodInfo ParameterExpressionMethod = typeof(Expression).GetMethod("Parameter", new[] { typeof(Type), typeof(string) });
		private static readonly MethodInfo PropertyExpressionMethod = typeof(Expression).GetMethod("Property", new[] { typeof(Expression), typeof(MethodInfo) });
		private static readonly MethodInfo UnaryExpressionMethod = typeof(Expression).GetMethod("Convert", new[] { typeof(Expression), typeof(Type) });

		private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public);
		private static readonly MethodInfo GetMethodFromHandleMethod = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });

		private static readonly Type OpenGenericExpressionType = typeof(Expression<>);
		private static readonly Type OpenGenericFuncType = typeof(Func<,>);
		private static readonly Type ClassMapType = typeof(ClassMap<>);

		private readonly string _tableName;
		private readonly IEnumerable<ColumnDefinition> _columns;
		private readonly string _dllName;

		private readonly AssemblyName _assemblyName;

		private readonly Lazy<AssemblyBuilder> _assemblyBuilder;
		private AssemblyBuilder AssemblyBuilder
		{
			get
			{
				return _assemblyBuilder.Value;
			}
		}

		private readonly Lazy<ModuleBuilder> _moduleBuilder;
		private ModuleBuilder ModuleBuilder
		{
			get
			{
				return _moduleBuilder.Value;
			}
		}

		public Type EntityType { get; private set; }
		public Type MappingType { get; private set; }

		public DynamicAssemblyCreator(string tableName, IEnumerable<ColumnDefinition> columns)
		{
			_tableName = tableName;
			_dllName = String.Format("Database.DynamicMappings.{0}.dll", _tableName);

			_columns = columns;

			_assemblyName = BuildAssemblyName();

			_assemblyBuilder = new Lazy<AssemblyBuilder>(() => Thread.GetDomain().DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run, GetAssemblyFilePath()));

			_moduleBuilder = new Lazy<ModuleBuilder>(() => AssemblyBuilder.DefineDynamicModule(_assemblyName.Name, true), LazyThreadSafetyMode.ExecutionAndPublication);
		}

		// AssemblyBuilder.DefineDynamicModule(_assemblyName.Name, _dllName, true);

		private string GetAssemblyFilePath()
		{
			var uri = new Uri(GetType().Assembly.CodeBase);

			var file = new FileInfo(uri.LocalPath);

			return file.Directory.ToString();
		}

		private AssemblyName BuildAssemblyName()
		{
			var currentAssembly = GetType().Assembly;
			var currentAssemblyName = new AssemblyName(currentAssembly.FullName);

			return new AssemblyName
			{
				Name = String.Format("Database.DynamicMapping.{0}", _tableName),
				CultureInfo = currentAssemblyName.CultureInfo,
				HashAlgorithm = AssemblyHashAlgorithm.SHA1,
				Version = currentAssemblyName.Version
			};
		}

		public void CreateDynamicAssembly()
		{
			if ((EntityType == null) && (MappingType == null))
			{
				EntityType = BuildEntityType();
				MappingType = BuildMappingType();

				SaveDynamicAssembly();
			}
		}

		private Type BuildEntityType()
		{
			var typeBuilder = ModuleBuilder.DefineType(String.Format("Database.DynamicMappings.{0}.Entity", _tableName), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

			var idColumns = new List<ColumnDefinition>();

			foreach (var column in _columns)
			{
				if (column.IsPrimaryKeyColumn)
				{
					idColumns.Add(column);
				}
				else
				{
					typeBuilder.DefineProperty(column.Name, column.Type);
				}
			}

			if (idColumns.Any())
			{
				if (idColumns.Count > 1)
				{
					// TODO: this won't work, as it will infinitely recurse
					//var idType = BuildEntityType(String.Format("{0}Id", tableName), idColumns);
					//typeBuilder.DefineProperty("Id", idType);
					throw new NotSupportedException(String.Format("Multiple primary key columns found for table '{0}'.", _tableName));
				}
				else
				{
					var id = idColumns.Single();

					typeBuilder.DefineProperty(id.Name, id.Type);
				}
			}
			else
			{
				throw new NotSupportedException(String.Format("No primary key columns found for table '{0}'.", _tableName));
			}

			var newType = typeBuilder.CreateType();

			return newType;
		}

		private Type BuildMappingType()
		{
			var mapName = String.Format("Database.DynamicMappings.{0}.Map", _tableName);

			var baseClassType = ClassMapType.MakeGenericType(EntityType);
			var tableFunction = baseClassType.GetMethod("Table");

			var fullFuncType = OpenGenericFuncType.MakeGenericType(EntityType, typeof(object));
			var filledExpressionType = OpenGenericExpressionType.MakeGenericType(fullFuncType);

			var typeBuilder = ModuleBuilder.DefineType(mapName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable, baseClassType);

			var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);

			var constructorIntermediateLanguageGenerator = constructorBuilder.GetILGenerator();

			var baseParameterlessConstructor = baseClassType.GetConstructor(Type.EmptyTypes);

			constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
			constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, baseParameterlessConstructor);

			// TODO: save these LocalBuilders and use them below in calls to stloc??
			constructorIntermediateLanguageGenerator.DeclareLocal(typeof(ParameterExpression));
			constructorIntermediateLanguageGenerator.DeclareLocal(typeof(ParameterExpression));

			constructorIntermediateLanguageGenerator.Emit(OpCodes.Nop);
			constructorIntermediateLanguageGenerator.Emit(OpCodes.Nop);

			constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
			constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldstr, EntityType.Name);
			constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, tableFunction);

			// TODO: there has got to be a better way to find this MethodInfo...
			var genericLambdaFunction = typeof(Expression).GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Where(x => x.Name == "Lambda")
				.Where(x => x.GetGenericArguments().Count() == 1)
				.Where(x => x.GetParameters().Count() == 2)
				.Single(x => x.GetParameters().ElementAt(1).ParameterType == typeof(ParameterExpression[]));

			var mapMethod = baseClassType.GetMethod("Map", new[] { filledExpressionType });
			var idMethod = baseClassType.GetMethod("Id", new[] { filledExpressionType });

			var lambdaExpressionFunction = genericLambdaFunction.MakeGenericMethod(fullFuncType);

			foreach (var column in _columns)
			{
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldtoken, EntityType);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, GetTypeFromHandleMethod);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldstr, "x");
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, ParameterExpressionMethod);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Stloc_0);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_0);

				var propertyGetMethod = EntityType.GetProperty(column.Name).GetGetMethod();

				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldtoken, propertyGetMethod);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, GetMethodFromHandleMethod);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, PropertyExpressionMethod);

				if (column.Type.IsValueType)
				{
					constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldtoken, typeof(object));
					constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, GetTypeFromHandleMethod);
					constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, UnaryExpressionMethod);
				}

				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldc_I4_1);

				constructorIntermediateLanguageGenerator.Emit(OpCodes.Newarr, typeof(ParameterExpression));
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Stloc_1);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_1);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldc_I4_0);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_0);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Stelem_Ref);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_1);
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, lambdaExpressionFunction);

				var method = column.IsPrimaryKeyColumn
					? idMethod
					: mapMethod;

				constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, method);

				constructorIntermediateLanguageGenerator.Emit(OpCodes.Pop);
			}

			constructorIntermediateLanguageGenerator.Emit(OpCodes.Ret);

			var newType = typeBuilder.CreateType();

			return newType;
		}

		private void SaveDynamicAssembly()
		{
			AppDomain.CurrentDomain.AssemblyResolve += Hello;

			// TODO: i think i need to unload, modify and then reload the assembly from a file on the disk,
			//       and make sure it is loadable via Assembly.Load, so that NHibernate can get at it
			//AssemblyBuilder.Save(_dllName);
		}

		private Assembly Hello(object sender, ResolveEventArgs args)
		{
			Assembly result = null;

			if (args.Name == EntityType.Assembly.FullName)
			{
				result = EntityType.Assembly;
			}

			return result;
		}
	}
}
