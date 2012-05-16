using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Assemblies;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

using FluentNHibernate.Mapping;

namespace Database.Core
{
	public static class AnonymousTypeManager
	{
		private static readonly Lazy<bool> _saveAssembly = new Lazy<bool>(() =>
		{
			var saveDynamicMappingAssembly = ConfigurationManager.AppSettings["SaveAssembly"];

			bool saveAssembly;
			return (String.IsNullOrWhiteSpace(saveDynamicMappingAssembly) == false) && Boolean.TryParse(saveDynamicMappingAssembly, out saveAssembly) && saveAssembly;
		});
		private static bool SaveAssembly { get { return _saveAssembly.Value; } }

		private static readonly Lazy<AssemblyBuilderAccess> _assemblyBuilderAccess = new Lazy<AssemblyBuilderAccess>(() =>
		{
			var result = AssemblyBuilderAccess.Run;

			if (SaveAssembly)
			{
				result = AssemblyBuilderAccess.RunAndSave;
			}

			return result;
		});
		private static AssemblyBuilderAccess AssemblyBuilderAccess { get { return _assemblyBuilderAccess.Value; } }

		private static readonly Lazy<AssemblyName> _assemblyName = new Lazy<AssemblyName>(() =>
		{
			var currentAssembly = typeof(AnonymousTypeManager).Assembly;
			var currentAssemblyName = new AssemblyName(currentAssembly.FullName);

			return new AssemblyName
			{
				Name = "Database.DynamicMappingTypes",
				CodeBase = currentAssembly.CodeBase,
				CultureInfo = currentAssemblyName.CultureInfo,
				HashAlgorithm = AssemblyHashAlgorithm.SHA1,
				Version = currentAssemblyName.Version
			};
		});
		private static AssemblyName AssemblyName
		{
			get
			{
				return _assemblyName.Value;
			}
		}

		private static readonly Lazy<AssemblyBuilder> _assemblyBuilder = new Lazy<AssemblyBuilder>(() => Thread.GetDomain().DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess));
		private static AssemblyBuilder AssemblyBuilder
		{
			get
			{
				return _assemblyBuilder.Value;
			}
		}

		private static readonly Lazy<ModuleBuilder> _moduleBuilder = new Lazy<ModuleBuilder>(() =>
		{
			return SaveAssembly
				? AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, String.Format("{0}.dll", AssemblyName.Name), true)
				: AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, true);
		}, LazyThreadSafetyMode.ExecutionAndPublication);
		private static ModuleBuilder ModuleBuilder
		{
			get
			{
				return _moduleBuilder.Value;
			}
		}

		private static readonly object TypeLocker = new object();
		private static readonly ConcurrentDictionary<string, Type> Types;

		static AnonymousTypeManager()
		{
			Types = new ConcurrentDictionary<string, Type>();
		}

		public static Type BuildEntityType(string tableName, IEnumerable<ColumnDefinition> columns)
		{
			if (Types.ContainsKey(tableName) == false)
			{
				lock (TypeLocker)
				{
					if (Types.ContainsKey(tableName) == false)
					{
						var typeBuilder = ModuleBuilder.DefineType(tableName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

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
								//var idType = BuildEntityType(String.Format("{0}Id", tableName), idColumns);
								//typeBuilder.DefineProperty("Id", idType);
								throw new NotSupportedException(String.Format("Multiple primary key columns found for table '{0}'.", tableName));
							}
							else
							{
								var id = idColumns.Single();

								typeBuilder.DefineProperty(id.Name, id.Type);
							}
						}
						else
						{
							throw new NotSupportedException(String.Format("No primary key columns found for table '{0}'.", tableName));
						}

						var newType = typeBuilder.CreateType();

						// TODO: perhaps save the dynamic assembly here...
						if (SaveAssembly)
						{
							// TODO: i think i need to unload, modify and then reload the assembly from a file on the disk,
							//       and make sure it is loadable via Assembly.Load, so that NHibernate can get at it
							AssemblyBuilder.Save(String.Format("{0}.dll", AssemblyName.Name));
						}

						Types.TryAdd(tableName, newType);
					}
				}
			}

			return Types[tableName];
		}

		public static Type BuildMappingType(string tableName,  IEnumerable<ColumnDefinition> columns)
		{
			var mapName = String.Format("{0}Map", tableName);

			Type entityType;
			if (Types.TryGetValue(tableName, out entityType) == false)
			{
				entityType = BuildEntityType(tableName, columns);
			}

			if (Types.ContainsKey(mapName) == false)
			{
				lock (TypeLocker)
				{
					if (Types.ContainsKey(mapName) == false)
					{
						var classMap = typeof(ClassMap<>);

						var baseClassType = classMap.MakeGenericType(entityType);

						var typeBuilder = ModuleBuilder.DefineType(mapName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable, baseClassType);

						var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);

						var constructorILGenerator = constructorBuilder.GetILGenerator();

						var baseParameterlessConstructor = baseClassType.GetConstructor(Type.EmptyTypes);

						constructorILGenerator.Emit(OpCodes.Ldarg_0);
						constructorILGenerator.Emit(OpCodes.Call, baseParameterlessConstructor);

						var parameterLocal1 = constructorILGenerator.DeclareLocal(typeof (ParameterExpression));
						var parameterLocal2 = constructorILGenerator.DeclareLocal(typeof(ParameterExpression));

						var tableFunction = baseClassType.GetMethod("Table");

						constructorILGenerator.Emit(OpCodes.Nop);
						constructorILGenerator.Emit(OpCodes.Nop);

						constructorILGenerator.Emit(OpCodes.Ldarg_0);
						constructorILGenerator.Emit(OpCodes.Ldstr, entityType.Name);
						constructorILGenerator.Emit(OpCodes.Call, tableFunction);

						var parameterExpressionFunction = typeof(Expression).GetMethod("Parameter", new[] { typeof(Type), typeof(string) });
						var propertyExpressionFunction = typeof(Expression).GetMethod("Property", new[] { typeof(Expression), typeof(MethodInfo) });
						var unaryExpressionFunction = typeof(Expression).GetMethod("Convert", new[] { typeof(Expression), typeof(Type) });
						var emptyFuncType = typeof(Func<,>);

						var getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public);
						var getMethodFromHandle = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });

						// TODO: there has got to be a better way to find this MethodInfo...
						var genericLambdaFunction = typeof(Expression).GetMethods(BindingFlags.Static | BindingFlags.Public)
							.Where(x => x.Name == "Lambda")
							.Where(x => x.GetGenericArguments().Count() == 1)
							.Where(x => x.GetParameters().Count() == 2)
							.Single(x => x.GetParameters().ElementAt(1).ParameterType == typeof(ParameterExpression[]));

						var fullFuncType = emptyFuncType.MakeGenericType(entityType, typeof(object));

						var expressionType = typeof(Expression<>);
						var filledExpressionType = expressionType.MakeGenericType(fullFuncType);

						var lambdaExpressionFunction = genericLambdaFunction.MakeGenericMethod(fullFuncType);

						foreach (var column in columns)
						{
							constructorILGenerator.Emit(OpCodes.Ldarg_0);
							constructorILGenerator.Emit(OpCodes.Ldtoken, entityType);
							constructorILGenerator.Emit(OpCodes.Call, getTypeFromHandle);
							constructorILGenerator.Emit(OpCodes.Ldstr, "x");
							constructorILGenerator.Emit(OpCodes.Call, parameterExpressionFunction);
							constructorILGenerator.Emit(OpCodes.Stloc_0);
							constructorILGenerator.Emit(OpCodes.Ldloc_0);

							var propertyGetMethod = entityType.GetProperty(column.Name).GetGetMethod();

							constructorILGenerator.Emit(OpCodes.Ldtoken, propertyGetMethod);
							constructorILGenerator.Emit(OpCodes.Call, getMethodFromHandle);
							constructorILGenerator.Emit(OpCodes.Castclass, typeof (MethodInfo));
							constructorILGenerator.Emit(OpCodes.Call, propertyExpressionFunction);

							if (column.Type.IsValueType)
							{
								constructorILGenerator.Emit(OpCodes.Ldtoken, typeof (object));
								constructorILGenerator.Emit(OpCodes.Call, getTypeFromHandle);
								constructorILGenerator.Emit(OpCodes.Call, unaryExpressionFunction);
							}

							constructorILGenerator.Emit(OpCodes.Ldc_I4_1);

							constructorILGenerator.Emit(OpCodes.Newarr, typeof (ParameterExpression));
							constructorILGenerator.Emit(OpCodes.Stloc_1);
							constructorILGenerator.Emit(OpCodes.Ldloc_1);
							constructorILGenerator.Emit(OpCodes.Ldc_I4_0);
							constructorILGenerator.Emit(OpCodes.Ldloc_0);
							constructorILGenerator.Emit(OpCodes.Stelem_Ref);
							constructorILGenerator.Emit(OpCodes.Ldloc_1);
							constructorILGenerator.Emit(OpCodes.Call, lambdaExpressionFunction);

							var mapMethod = baseClassType.GetMethod("Map", new[] { filledExpressionType });
							var idMethod = baseClassType.GetMethod("Id", new[] { filledExpressionType });

							var method = column.IsPrimaryKeyColumn
								? idMethod
								: mapMethod;

							constructorILGenerator.Emit(OpCodes.Call, method);

							constructorILGenerator.Emit(OpCodes.Pop);
						}

						constructorILGenerator.Emit(OpCodes.Ret);

						var newType = typeBuilder.CreateType();

						// TODO: perhaps save the dynamic assembly here...
						if (SaveAssembly)
						{
							AssemblyBuilder.Save(String.Format("{0}.dll", AssemblyName.Name));
						}

						Types.TryAdd(mapName, newType);
					}
				}
			}

			return Types[mapName];
		}

		private static void DefineProperty(this TypeBuilder typeBuilder, string propertyName, Type propertyType)
		{
			var backingFieldBuilder = typeBuilder.DefineField(String.Format("_{0}", propertyName), propertyType, FieldAttributes.Private);

			// The last argument of DefineProperty is null, because the
			// property has no parameters. (If you don't specify null, you must
			// specify an array of Type objects. For a parameterless property,
			// use an array with no elements: new Type[] {})
			var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, Type.EmptyTypes);

			// TODO: not sure if HideBySig is needed.
			const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;

			var propertyGetMethodBuilder = typeBuilder.DefineGetMethod(propertyName, propertyType, getSetAttr, backingFieldBuilder);

			var propertySetMethodBuilder = typeBuilder.DefineSetMethod(propertyName, propertyType, getSetAttr, backingFieldBuilder);

			propertyBuilder.SetGetMethod(propertyGetMethodBuilder);
			propertyBuilder.SetSetMethod(propertySetMethodBuilder);
		}

		private static MethodBuilder DefineGetMethod(this TypeBuilder typeBuilder, string propertyName, Type propertyType, MethodAttributes methodAttributes, FieldBuilder backingFieldBuilder)
		{
			var propertyGetMethodBuilder = typeBuilder.DefineMethod(String.Format("get_{0}", propertyName), methodAttributes, propertyType, Type.EmptyTypes);

			var propertyGetILGenerator = propertyGetMethodBuilder.GetILGenerator();

			propertyGetILGenerator.Emit(OpCodes.Ldarg_0);
			propertyGetILGenerator.Emit(OpCodes.Ldfld, backingFieldBuilder);
			propertyGetILGenerator.Emit(OpCodes.Ret);

			return propertyGetMethodBuilder;
		}

		private static MethodBuilder DefineSetMethod(this TypeBuilder typeBuilder, string propertyName, Type propertyType, MethodAttributes methodAttributes, FieldBuilder backingFieldBuilder)
		{
			var propertySetMethodBuilder = typeBuilder.DefineMethod(String.Format("set_{0}", propertyName), methodAttributes, null, new[] { propertyType });

			var propertySetILGenerator = propertySetMethodBuilder.GetILGenerator();

			propertySetILGenerator.Emit(OpCodes.Ldarg_0);
			propertySetILGenerator.Emit(OpCodes.Ldarg_1);
			propertySetILGenerator.Emit(OpCodes.Stfld, backingFieldBuilder);
			propertySetILGenerator.Emit(OpCodes.Ret);

			return propertySetMethodBuilder;
		}
	}
}
