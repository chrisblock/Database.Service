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
		//private static readonly Lazy<string> LazyDllName = new Lazy<string>(() => String.Format("{0}.dll", AssemblyName.Name), LazyThreadSafetyMode.ExecutionAndPublication);
		//private static string DllName { get { return LazyDllName.Value; } }

		private static readonly MethodInfo ParameterExpressionMethod = ReflectionUtility.GetMethodInfo(() => Expression.Parameter(null, null));
		private static readonly MethodInfo PropertyExpressionMethod = ReflectionUtility.GetMethodInfo(() => Expression.Property(null, (MethodInfo)null));
		private static readonly MethodInfo UnaryExpressionMethod = ReflectionUtility.GetMethodInfo(() => Expression.Convert(null, null));
		private static readonly MethodInfo OpenGenericLambdaFunction = ReflectionUtility.GetMethodInfo(() => Expression.Lambda<Type>(null, null));

		private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public);
		private static readonly MethodInfo GetMethodFromHandleMethod = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });

		private static readonly Type OpenGenericExpressionType = typeof(Expression<>);
		private static readonly Type OpenGenericFuncType = typeof(Func<,>);
		private static readonly Type OpenGenericClassMapType = typeof(ClassMap<>);
		private static readonly Type OpenGenericCompositeIdentityPartType = typeof (CompositeIdentityPart<>);

		private static readonly Lazy<AssemblyName> LazyAssemblyName = new Lazy<AssemblyName>(CreateAssemblyName, LazyThreadSafetyMode.ExecutionAndPublication);
		private static AssemblyName AssemblyName
		{
			get
			{
				return LazyAssemblyName.Value;
			}
		}

		private static readonly Lazy<AssemblyBuilder> LazyAssemblyBuilder = new Lazy<AssemblyBuilder>(() => Thread.GetDomain().DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run), LazyThreadSafetyMode.ExecutionAndPublication);
		private static AssemblyBuilder AssemblyBuilder
		{
			get
			{
				return LazyAssemblyBuilder.Value;
			}
		}

		private static readonly Lazy<ModuleBuilder> LazyModuleBuilder = new Lazy<ModuleBuilder>(() => AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, true), LazyThreadSafetyMode.ExecutionAndPublication);
		private static ModuleBuilder ModuleBuilder
		{
			get
			{
				return LazyModuleBuilder.Value;
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

		public static Tuple<Type, Type> BuildTypesForTable(TableDefinition table)
		{
			var entityType = BuildEntityType(table.Name, table.Columns);
			var mappingType = BuildMappingType(entityType, table.Columns);

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

						var identityProperties = new List<PropertyInfo>();

						foreach (var column in columns)
						{
							var property = typeBuilder.DefineProperty(column.Name, column.Type);

							if (column.IsPrimaryKeyColumn)
							{
								identityProperties.Add(property);
							}
						}

						if (identityProperties.Count > 0)
						{
							typeBuilder.DefineGetHashCodeMethod(identityProperties);

							if (identityProperties.Count > 1)
							{
								var virtualEqualsMethod = typeBuilder.DefineVirtualEqualsMethod(identityProperties);

								typeBuilder.DefineOverrideEqualsMethod(virtualEqualsMethod, identityProperties);
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
						var baseClassType = OpenGenericClassMapType.MakeGenericType(entityType);
						var tableFunction = baseClassType.GetMethod("Table");

						var fullFuncType = OpenGenericFuncType.MakeGenericType(entityType, typeof(object));
						var filledExpressionType = OpenGenericExpressionType.MakeGenericType(fullFuncType);

						var lambdaExpressionFunction = OpenGenericLambdaFunction.MakeGenericMethod(fullFuncType);

						var compositeIdMethod = baseClassType.GetMethod("CompositeId", Type.EmptyTypes);

						var compositeIdentityPartType = OpenGenericCompositeIdentityPartType.MakeGenericType(entityType);
						var keyPropertyMethod = compositeIdentityPartType.GetMethod("KeyProperty", new[] { filledExpressionType });

						var mapMethod = baseClassType.GetMethod("Map", new[] { filledExpressionType });
						var idMethod = baseClassType.GetMethod("Id", new[] { filledExpressionType });

						var typeBuilder = ModuleBuilder.DefineType(mapName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable, baseClassType);

						var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);

						var constructorIntermediateLanguageGenerator = constructorBuilder.GetILGenerator();

						var baseParameterlessConstructor = baseClassType.GetConstructor(Type.EmptyTypes);

						if (baseParameterlessConstructor == null)
						{
							throw new NullReferenceException(String.Format("Parameterless constructor not found for type '{0}'.", baseClassType));
						}

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

						constructorIntermediateLanguageGenerator.Emit(OpCodes.Nop);

						var identityColumns = new List<ColumnDefinition>();
						var regularColumns = new List<ColumnDefinition>();

						foreach (var columnDefinition in columns)
						{
							if (columnDefinition.IsPrimaryKeyColumn)
							{
								identityColumns.Add(columnDefinition);
							}
							else
							{
								regularColumns.Add(columnDefinition);
							}
						}

						if (identityColumns.Count == 1)
						{
							var column = identityColumns.Single();

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

							// TODO: should this be a CALLVIRT opcode???
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, idMethod);

							constructorIntermediateLanguageGenerator.Emit(OpCodes.Pop);
						}
						else if (identityColumns.Count > 1)
						{
							constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);

							constructorIntermediateLanguageGenerator.Emit(OpCodes.Callvirt, compositeIdMethod);

							foreach (var column in identityColumns)
							{
								constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldtoken, entityType);
								constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, GetTypeFromHandleMethod);
								constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldstr, "x");
								constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, ParameterExpressionMethod);
								constructorIntermediateLanguageGenerator.Emit(OpCodes.Stloc_0);
								constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_0);

								var propertyGetMethod = entityType.GetProperty(column.Name).GetGetMethod();
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

								constructorIntermediateLanguageGenerator.Emit(OpCodes.Callvirt, keyPropertyMethod);
							}

							constructorIntermediateLanguageGenerator.Emit(OpCodes.Pop);
						}

						foreach (var column in regularColumns)
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

							constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, mapMethod);

							constructorIntermediateLanguageGenerator.Emit(OpCodes.Pop);
						}

						constructorIntermediateLanguageGenerator.Emit(OpCodes.Nop);

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
