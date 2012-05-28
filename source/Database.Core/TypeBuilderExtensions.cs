﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Database.Core
{
	public static class TypeBuilderExtensions
	{
		private const MethodAttributes PublicVirtualHideBySig = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
		private const MethodAttributes PublicVirtualHideBySigSpecialName = PublicVirtualHideBySig | MethodAttributes.SpecialName;

		private static readonly MethodInfo TypeInequalityOperator = typeof(Type).GetMethod("op_Inequality", new[] { typeof(Type), typeof(Type) });

		private static readonly MethodInfo ObjectEqualsMethod = typeof(object).GetMethod("Equals", new[] { typeof(object) });
		private static readonly MethodInfo ObjectReferenceEqualsMethod = typeof(object).GetMethod("ReferenceEquals", new[] { typeof(object), typeof(object) });
		private static readonly MethodInfo ObjectGetTypeMethod = typeof(object).GetMethod("GetType", Type.EmptyTypes);
		private static readonly MethodInfo ObjectGetHashCodeMethod = typeof(object).GetMethod("GetHashCode", Type.EmptyTypes);

		private static readonly MethodInfo StringFormatMethod = typeof(string).GetMethod("Format", new[] { typeof(string), typeof(object[]) });

		private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public);

		public static PropertyBuilder DefineProperty(this TypeBuilder typeBuilder, string propertyName, Type propertyType)
		{
			var backingFieldBuilder = typeBuilder.DefineField(String.Format("_{0}", propertyName), propertyType, FieldAttributes.Private);

			// The last argument of DefineProperty is null, because the
			// property has no parameters. (If you don't specify null, you must
			// specify an array of Type objects. For a parameterless property,
			// use an array with no elements: new Type[] {})
			var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, Type.EmptyTypes);

			var propertyGetMethodBuilder = typeBuilder.DefineGetMethod(propertyName, propertyType, PublicVirtualHideBySigSpecialName, backingFieldBuilder);

			var propertySetMethodBuilder = typeBuilder.DefineSetMethod(propertyName, propertyType, PublicVirtualHideBySigSpecialName, backingFieldBuilder);

			propertyBuilder.SetGetMethod(propertyGetMethodBuilder);
			propertyBuilder.SetSetMethod(propertySetMethodBuilder);

			return propertyBuilder;
		}

		private static MethodBuilder DefineGetMethod(this TypeBuilder typeBuilder, string propertyName, Type propertyType, MethodAttributes methodAttributes, FieldBuilder backingFieldBuilder)
		{
			var propertyGetMethodBuilder = typeBuilder.DefineMethod(String.Format("get_{0}", propertyName), methodAttributes, propertyType, Type.EmptyTypes);

			var propertyGetIntermediateLanguageGenerator = propertyGetMethodBuilder.GetILGenerator();

			propertyGetIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
			propertyGetIntermediateLanguageGenerator.Emit(OpCodes.Ldfld, backingFieldBuilder);
			propertyGetIntermediateLanguageGenerator.Emit(OpCodes.Ret);

			return propertyGetMethodBuilder;
		}

		private static MethodBuilder DefineSetMethod(this TypeBuilder typeBuilder, string propertyName, Type propertyType, MethodAttributes methodAttributes, FieldBuilder backingFieldBuilder)
		{
			var propertySetMethodBuilder = typeBuilder.DefineMethod(String.Format("set_{0}", propertyName), methodAttributes, null, new[] { propertyType });

			var propertySetIntermediateLanguageGenerator = propertySetMethodBuilder.GetILGenerator();

			propertySetIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
			propertySetIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_1);
			propertySetIntermediateLanguageGenerator.Emit(OpCodes.Stfld, backingFieldBuilder);
			propertySetIntermediateLanguageGenerator.Emit(OpCodes.Ret);

			return propertySetMethodBuilder;
		}

		public static MethodBuilder DefineGetHashCodeMethod(this TypeBuilder typeBuilder, IEnumerable<PropertyInfo> includedProperties)
		{
			var identityProperties = includedProperties.ToList();

			var hashCodeFormatString = String.Join("", identityProperties.Select((x, i) => String.Format("{0}:{{{1}}};", x.Name, i)));

			var method = typeBuilder.DefineMethod("GetHashCode", PublicVirtualHideBySig, typeof(int), Type.EmptyTypes);

			var il = method.GetILGenerator();

			il.DeclareLocal(typeof(int));

			il.DeclareLocal(typeof(object[]));

			var parameterArraySize = identityProperties.Count;

			il.Emit(OpCodes.Ldc_I4, parameterArraySize);

			il.Emit(OpCodes.Newarr, typeof(object));

			il.Emit(OpCodes.Stloc_1);

			for (var i = 0; i < parameterArraySize; i++)
			{
				var property = identityProperties[i];

				var type = property.PropertyType;

				var getMethod = property.GetGetMethod();

				il.Emit(OpCodes.Ldloc_1);

				il.Emit(OpCodes.Ldc_I4, i);

				il.Emit(OpCodes.Ldarg_0);

				il.Emit(OpCodes.Callvirt, getMethod);

				if (type.IsValueType)
				{
					il.Emit(OpCodes.Box, type);
				}

				il.Emit(OpCodes.Stelem_Ref);
			}

			il.Emit(OpCodes.Ldstr, hashCodeFormatString);

			il.Emit(OpCodes.Ldloc_1);

			il.Emit(OpCodes.Call, StringFormatMethod);

			il.Emit(OpCodes.Callvirt, ObjectGetHashCodeMethod);

			il.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(method, ObjectGetHashCodeMethod);

			return method;
		}

		public static MethodBuilder DefineVirtualEqualsMethod(this TypeBuilder typeBuilder, IEnumerable<PropertyInfo> includedProperties)
		{
			var method = typeBuilder.DefineMethod("Equals", PublicVirtualHideBySig | MethodAttributes.Virtual, typeof(bool), new Type[] { typeBuilder });

			// TODO: define virtual Equals(<TYPE>) method

			var il = method.GetILGenerator();

			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Ret);

			return method;
		}

		public static MethodBuilder DefineOverrideEqualsMethod(this TypeBuilder typeBuilder, MethodInfo virtualEqualsMethod, ICollection<PropertyInfo> includedProperties)
		{
			var equalsOverride = typeBuilder.DefineMethod("Equals", PublicVirtualHideBySig, typeof(bool), new[] { typeof(object) });

			var equalsOverrideIntermediateLanguageGenerator = equalsOverride.GetILGenerator();

			var referenceEqualThisAndArgument = equalsOverrideIntermediateLanguageGenerator.DefineLabel();
			var compareTypeEquality = equalsOverrideIntermediateLanguageGenerator.DefineLabel();
			var castAndCallVirtualEquals = equalsOverrideIntermediateLanguageGenerator.DefineLabel();
			var returnLocalVariableZero = equalsOverrideIntermediateLanguageGenerator.DefineLabel();

			equalsOverrideIntermediateLanguageGenerator.DeclareLocal(typeof(bool));
			equalsOverrideIntermediateLanguageGenerator.DeclareLocal(typeof(bool));

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Nop);

			//
			// ReferenceEquals(null, obj)
			//
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldnull);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_1);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Call, ObjectReferenceEqualsMethod);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldc_I4_0);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ceq);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Stloc_1);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_1);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Brtrue_S, referenceEqualThisAndArgument);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Nop);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldc_I4_0);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Stloc_0);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Br_S, returnLocalVariableZero);

			//
			// ReferenceEquals(this, obj)
			//
			equalsOverrideIntermediateLanguageGenerator.MarkLabel(referenceEqualThisAndArgument);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_1);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Call, ObjectReferenceEqualsMethod);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldc_I4_0);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ceq);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Stloc_1);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_1);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Brtrue_S, compareTypeEquality);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Nop);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldc_I4_1);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Stloc_0);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Br_S, returnLocalVariableZero);

			//
			// obj.GetType() != typeof (<TYPE>)
			//

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_1);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Callvirt, ObjectGetTypeMethod);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldtoken, typeBuilder);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Call, GetTypeFromHandleMethod);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Call, TypeInequalityOperator);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldc_I4_0);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ceq);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Stloc_1);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_1);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Brtrue_S, castAndCallVirtualEquals);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Nop);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldc_I4_0);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Stloc_0);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Br_S, returnLocalVariableZero);

			//
			// Equals((<TYPE>) obj)
			//

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_1);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Castclass, typeBuilder);

			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Call, virtualEqualsMethod);

			//
			// return
			//

			equalsOverrideIntermediateLanguageGenerator.MarkLabel(returnLocalVariableZero);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ldloc_0);
			equalsOverrideIntermediateLanguageGenerator.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(equalsOverride, ObjectEqualsMethod);

			return equalsOverride;
		}
	}
}
