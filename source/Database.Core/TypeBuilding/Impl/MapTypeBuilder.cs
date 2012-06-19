using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

using Database.Core.TableReflection;

using FluentNHibernate.Mapping;

namespace Database.Core.TypeBuilding.Impl
{
	public class MapTypeBuilder : ITypeBuilder
	{
		private static readonly MethodInfo ParameterExpressionMethod = ReflectionUtility.GetMethodInfo(() => Expression.Parameter(null, null));
		private static readonly MethodInfo PropertyExpressionMethod = ReflectionUtility.GetMethodInfo(() => Expression.Property(null, (MethodInfo)null));
		private static readonly MethodInfo ConvertExpressionMethod = ReflectionUtility.GetMethodInfo(() => Expression.Convert(null, null));
		private static readonly MethodInfo OpenGenericLambdaFunction = ReflectionUtility.GetMethodInfo(() => Expression.Lambda<Type>(null, null));

		private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public);
		private static readonly MethodInfo GetMethodFromHandleMethod = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) });

		private static readonly Type OpenGenericExpressionType = typeof(Expression<>);
		private static readonly Type OpenGenericFuncType = typeof(Func<,>);
		private static readonly Type OpenGenericClassMapType = typeof(ClassMap<>);
		private static readonly Type OpenGenericCompositeIdentityPartType = typeof(CompositeIdentityPart<>);

		private readonly IDynamicAssemblyBuilder _dynamicAssemblyBuilder;

		public MapTypeBuilder(IDynamicAssemblyBuilder dynamicAssemblyBuilder)
		{
			_dynamicAssemblyBuilder = dynamicAssemblyBuilder;
		}

		// TODO: this method is in need of some love
		public Type Build(TableDefinition table)
		{
			var mapName = _dynamicAssemblyBuilder.BuildAssemblyQualifiedTypeName(table.GetMapName());

			var entityType = _dynamicAssemblyBuilder.GetBuiltType(_dynamicAssemblyBuilder.BuildAssemblyQualifiedTypeName(table.GetEntityName()));

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

			var typeBuilder = _dynamicAssemblyBuilder.BuildType(mapName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.BeforeFieldInit, baseClassType);

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
			constructorIntermediateLanguageGenerator.DeclareLocal(typeof(ParameterExpression));
			constructorIntermediateLanguageGenerator.DeclareLocal(typeof(ParameterExpression));

			constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
			constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldstr, entityType.Name);
			constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, tableFunction);

			constructorIntermediateLanguageGenerator.Emit(OpCodes.Nop);

			var identityColumns = new List<ColumnDefinition>();
			var regularColumns = new List<ColumnDefinition>();

			foreach (var columnDefinition in table.Columns)
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
					constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, ConvertExpressionMethod);
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
						constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, ConvertExpressionMethod);
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
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
				constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, PropertyExpressionMethod);

				if (column.Type.IsValueType)
				{
					constructorIntermediateLanguageGenerator.Emit(OpCodes.Ldtoken, typeof(object));
					constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, GetTypeFromHandleMethod);
					constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, ConvertExpressionMethod);
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

				constructorIntermediateLanguageGenerator.Emit(OpCodes.Call, mapMethod);

				constructorIntermediateLanguageGenerator.Emit(OpCodes.Pop);
			}

			constructorIntermediateLanguageGenerator.Emit(OpCodes.Ret);

			var newType = typeBuilder.CreateType();

			return newType;
		}
	}
}
