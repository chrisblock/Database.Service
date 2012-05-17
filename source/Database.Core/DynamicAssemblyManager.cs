using System;
using System.Collections.Concurrent;
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
	public static class DynamicAssemblyManager
	{
		private static readonly MethodInfo ParameterExpressionMethod = typeof(Expression).GetMethod("Parameter", new[] { typeof(Type), typeof(string) });
		private static readonly MethodInfo PropertyExpressionMethod = typeof(Expression).GetMethod("Property", new[] { typeof(Expression), typeof(MethodInfo) });
		private static readonly MethodInfo UnaryExpressionMethod = typeof(Expression).GetMethod("Convert", new[] { typeof(Expression), typeof(Type) });

		private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public);
		private static readonly MethodInfo GetMethodFromHandleMethod = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });

		private static readonly Type OpenGenericExpressionType = typeof(Expression<>);
		private static readonly Type OpenGenericFuncType = typeof(Func<,>);
		private static readonly Type ClassMapType = typeof(ClassMap<>);

		private static readonly Lazy<AssemblyName> _assemblyName = new Lazy<AssemblyName>(CreateAssemblyName, LazyThreadSafetyMode.ExecutionAndPublication);
		private static AssemblyName AssemblyName
		{
			get
			{
				return _assemblyName.Value;
			}
		}

		private static readonly Lazy<AssemblyBuilder> _assemblyBuilder = new Lazy<AssemblyBuilder>(() => Thread.GetDomain().DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run), LazyThreadSafetyMode.ExecutionAndPublication);
		private static AssemblyBuilder AssemblyBuilder
		{
			get
			{
				return _assemblyBuilder.Value;
			}
		}

		private static readonly Lazy<ModuleBuilder> _moduleBuilder = new Lazy<ModuleBuilder>(() => AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, true), LazyThreadSafetyMode.ExecutionAndPublication);
		private static ModuleBuilder ModuleBuilder
		{
			get
			{
				return _moduleBuilder.Value;
			}
		}

		private static readonly object TypeLocker = new object();
		private static readonly ConcurrentDictionary<string, Type> Types;

		static DynamicAssemblyManager()
		{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

			Types = new ConcurrentDictionary<string, Type>();
		}

		private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
		{
			Assembly result = null;

			if (args.Name == AssemblyBuilder.FullName)
			{
				result = AssemblyBuilder;
			}

			return result;
		}

		private static AssemblyName CreateAssemblyName()
		{
			var currentAssembly = typeof(DynamicAssemblyManager).Assembly;
			var currentAssemblyName = new AssemblyName(currentAssembly.FullName);

			var uriToCurrentDll = new Uri(currentAssembly.CodeBase);
			var currentDll = new FileInfo(uriToCurrentDll.LocalPath);

			return new AssemblyName
			{
				Name = "Database.DynamicMappings",
				CodeBase = String.Format("{0}", currentDll.Directory),
				CultureInfo = currentAssemblyName.CultureInfo,
				HashAlgorithm = AssemblyHashAlgorithm.SHA1,
				Version = currentAssemblyName.Version
			};
		}

		public static Tuple<Type, Type> BuildTypesForTable(string tableName, IEnumerable<ColumnDefinition> columns)
		{
			var entityType = BuildEntityType(tableName, columns);
			var mappingType = BuildMappingType(entityType, columns);

			return new Tuple<Type, Type>(entityType, mappingType);
		}

		private static Type BuildEntityType(string tableName, IEnumerable<ColumnDefinition> columns)
		{
			var entityName = String.Format("Database.DynamicMappings.{0}", tableName);

			if (Types.ContainsKey(entityName) == false)
			{
				lock (TypeLocker)
				{
					if (Types.ContainsKey(entityName) == false)
					{
						var typeBuilder = ModuleBuilder.DefineType(entityName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

						var idColumns = new List<ColumnDefinition>();

						foreach (var column in columns)
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
								//var idType = BuildEntityType(String.Format("{0}Id", entityName), idColumns);
								//typeBuilder.DefineProperty("Id", idType);
								throw new NotSupportedException(String.Format("Multiple primary key columns found for table '{0}'.", entityName));
							}
							else
							{
								var id = idColumns.Single();

								typeBuilder.DefineProperty(id.Name, id.Type);
							}
						}
						else
						{
							throw new NotSupportedException(String.Format("No primary key columns found for table '{0}'.", entityName));
						}

						var newType = typeBuilder.CreateType();

						Types.TryAdd(entityName, newType);
					}
				}
			}

			return Types[entityName];
		}

		private static Type BuildMappingType(Type entityType,  IEnumerable<ColumnDefinition> columns)
		{
			var tableName = entityType.Name;

			var mapName = String.Format("Database.DynamicMappings.{0}Map", tableName);

			if (Types.ContainsKey(mapName) == false)
			{
				lock (TypeLocker)
				{
					if (Types.ContainsKey(mapName) == false)
					{
						var baseClassType = ClassMapType.MakeGenericType(entityType);
						var tableFunction = baseClassType.GetMethod("Table");

						var fullFuncType = OpenGenericFuncType.MakeGenericType(entityType, typeof(object));
						var filledExpressionType = OpenGenericExpressionType.MakeGenericType(fullFuncType);

						// TODO: there has got to be a better way to find this MethodInfo...
						var genericLambdaFunction = typeof(Expression).GetMethods(BindingFlags.Static | BindingFlags.Public)
							.Where(x => x.Name == "Lambda")
							.Where(x => x.GetGenericArguments().Count() == 1)
							.Where(x => x.GetParameters().Count() == 2)
							.Single(x => x.GetParameters().ElementAt(1).ParameterType == typeof(ParameterExpression[]));

						var lambdaExpressionFunction = genericLambdaFunction.MakeGenericMethod(fullFuncType);

						var mapMethod = baseClassType.GetMethod("Map", new[] { filledExpressionType });
						var idMethod = baseClassType.GetMethod("Id", new[] { filledExpressionType });

						var typeBuilder = ModuleBuilder.DefineType(mapName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable, baseClassType);

						var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);

						var constructorIntermediateLanguageGenerator = constructorBuilder.GetILGenerator();

						var baseParameterlessConstructor = baseClassType.GetConstructor(Type.EmptyTypes);

						constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
						constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, baseParameterlessConstructor);

						// TODO: save these LocalBuilders and use them below in calls to stloc??
						constructorIntermediateLanguageGenerator.DeclareLocal(typeof (ParameterExpression));
						constructorIntermediateLanguageGenerator.DeclareLocal(typeof(ParameterExpression));

						constructorIntermediateLanguageGenerator.Emit(OpCodes.Nop);
						constructorIntermediateLanguageGenerator.Emit(OpCodes.Nop);

						constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
						constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldstr, entityType.Name);
						constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, tableFunction);

						foreach (var column in columns)
						{
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldtoken, entityType);
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, GetTypeFromHandleMethod);
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldstr, "x");
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, ParameterExpressionMethod);
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Stloc_0);
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_0);

							var propertyGetMethod = entityType.GetProperty(column.Name).GetGetMethod();

							constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldtoken, propertyGetMethod);
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, GetMethodFromHandleMethod);
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Castclass, typeof (MethodInfo));
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, PropertyExpressionMethod);

							if (column.Type.IsValueType)
							{
								constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldtoken, typeof (object));
								constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, GetTypeFromHandleMethod);
								constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, UnaryExpressionMethod);
							}

							constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldc_I4_1);

							constructorIntermediateLanguageGenerator.Emit(OpCodes.Newarr, typeof (ParameterExpression));
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

						Types.TryAdd(mapName, newType);
					}
				}
			}

			return Types[mapName];
		}
	}
}
