using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Database.Core
{
	public static class TypeBuilderExtensions
	{
		private const MethodAttributes PublicVirtualHideBySig = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
		private const MethodAttributes PublicVirtualHideBySigNewSlotSpecialName = PublicVirtualHideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName;

		private static readonly MethodInfo TypeEqualityOperator = typeof (Type).GetMethod("op_Equality", new[] { typeof (Type), typeof (Type) });

		private static readonly MethodInfo ObjectEqualsMethod = typeof (object).GetMethod("Equals", new[] { typeof (object) });
		private static readonly MethodInfo ObjectGetTypeMethod = typeof (object).GetMethod("GetType", Type.EmptyTypes);
		private static readonly MethodInfo ObjectGetHashCodeMethod = typeof (object).GetMethod("GetHashCode", Type.EmptyTypes);

		private static readonly MethodInfo ObjectStaticEqualsMethod = typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) });
		private static readonly MethodInfo ObjectStaticReferenceEqualsMethod = typeof(object).GetMethod("ReferenceEquals", new[] { typeof(object), typeof(object) });

		private static readonly MethodInfo StringFormatMethod = typeof (string).GetMethod("Format", new[] { typeof (string), typeof (object[]) });

		private static readonly MethodInfo GetTypeFromHandleMethod = typeof (Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public);

		public static PropertyBuilder DefineProperty(this TypeBuilder typeBuilder, string propertyName, Type propertyType)
		{
			// TODO: is FieldAttributes.PrivateScope necessary??
			var backingFieldBuilder = typeBuilder.DefineField(String.Format("<{0}>k__BackingField", propertyName), propertyType, FieldAttributes.Private);

			// The last argument of DefineProperty is null, because the
			// property has no parameters. (If you don't specify null, you must
			// specify an array of Type objects. For a parameterless property,
			// use an array with no elements: new Type[] {})
			// TODO: should we use PropertyAttributes.HasDefault??
			var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, CallingConventions.HasThis, propertyType, Type.EmptyTypes);

			//var customAttributeBuilder = new CustomAttributeBuilder(typeof(DataContractAttribute))

			//propertyBuilder.SetCustomAttribute();

			var propertyGetMethodBuilder = typeBuilder.DefineGetMethod(propertyName, propertyType, PublicVirtualHideBySigNewSlotSpecialName, backingFieldBuilder);

			var propertySetMethodBuilder = typeBuilder.DefineSetMethod(propertyName, propertyType, PublicVirtualHideBySigNewSlotSpecialName, backingFieldBuilder);

			propertyBuilder.SetGetMethod(propertyGetMethodBuilder);
			propertyBuilder.SetSetMethod(propertySetMethodBuilder);

			return propertyBuilder;
		}

		private static MethodBuilder DefineGetMethod(this TypeBuilder typeBuilder, string propertyName, Type propertyType, MethodAttributes methodAttributes, FieldInfo backingFieldBuilder)
		{
			var propertyGetMethodBuilder = typeBuilder.DefineMethod(String.Format("get_{0}", propertyName), methodAttributes, propertyType, Type.EmptyTypes);

			var propertyGetIntermediateLanguageGenerator = propertyGetMethodBuilder.GetILGenerator();

			propertyGetIntermediateLanguageGenerator.Emit(OpCodes.Ldarg_0);
			propertyGetIntermediateLanguageGenerator.Emit(OpCodes.Ldfld, backingFieldBuilder);
			propertyGetIntermediateLanguageGenerator.Emit(OpCodes.Ret);

			return propertyGetMethodBuilder;
		}

		private static MethodBuilder DefineSetMethod(this TypeBuilder typeBuilder, string propertyName, Type propertyType, MethodAttributes methodAttributes, FieldInfo backingFieldBuilder)
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

			il.DeclareLocal(typeof(object[]));

			var parameterArraySize = identityProperties.Count;

			il.Emit(OpCodes.Ldc_I4, parameterArraySize);

			il.Emit(OpCodes.Newarr, typeof(object));

			il.Emit(OpCodes.Stloc_0);

			for (var i = 0; i < parameterArraySize; i++)
			{
				var property = identityProperties[i];

				var type = property.PropertyType;

				var getMethod = property.GetGetMethod();

				il.Emit(OpCodes.Ldloc_0);

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

			il.Emit(OpCodes.Ldloc_0);

			il.Emit(OpCodes.Call, StringFormatMethod);

			il.Emit(OpCodes.Callvirt, ObjectGetHashCodeMethod);

			il.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(method, ObjectGetHashCodeMethod);

			return method;
		}

		public static MethodBuilder DefineVirtualEqualsMethod(this TypeBuilder typeBuilder, IEnumerable<PropertyInfo> includedProperties)
		{
			var method = typeBuilder.DefineMethod("Equals", PublicVirtualHideBySig | MethodAttributes.Virtual, typeof(bool), new Type[] { typeBuilder });

			var il = method.GetILGenerator();

			var returnLocalZero = il.DefineLabel();

			il.DeclareLocal(typeof (bool));

			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Call, ObjectStaticReferenceEqualsMethod);

			il.Emit(OpCodes.Brtrue_S, returnLocalZero);

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Call, ObjectStaticReferenceEqualsMethod);

			il.Emit(OpCodes.Stloc_0);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Brtrue_S, returnLocalZero);

			foreach (var property in includedProperties)
			{
				var type = property.PropertyType;
				var getMethod = property.GetGetMethod();

				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Callvirt, getMethod);

				if (type.IsValueType)
				{
					il.Emit(OpCodes.Box, type);
				}

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Callvirt, getMethod);

				if (type.IsValueType)
				{
					il.Emit(OpCodes.Box, type);
				}

				il.Emit(OpCodes.Call, ObjectStaticEqualsMethod);

				il.Emit(OpCodes.Brfalse_S, returnLocalZero);
			}

			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Stloc_0);

			il.MarkLabel(returnLocalZero);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ret);

			return method;
		}

		public static MethodBuilder DefineOverrideEqualsMethod(this TypeBuilder typeBuilder, MethodInfo virtualEqualsMethod, ICollection<PropertyInfo> includedProperties)
		{
			var method = typeBuilder.DefineMethod("Equals", PublicVirtualHideBySig, typeof (bool), new[] { typeof (object) });

			var il = method.GetILGenerator();

			var returnLocalVariableZero = il.DefineLabel();

			il.DeclareLocal(typeof(bool));

			//
			// ReferenceEquals(null, obj)
			//
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldarg_1);

			il.Emit(OpCodes.Call, ObjectStaticReferenceEqualsMethod);

			il.Emit(OpCodes.Brtrue_S, returnLocalVariableZero);

			//
			// ReferenceEquals(this, obj)
			//

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);

			il.Emit(OpCodes.Call, ObjectStaticReferenceEqualsMethod);

			il.Emit(OpCodes.Stloc_0);
			il.Emit(OpCodes.Ldloc_0);

			il.Emit(OpCodes.Brtrue_S, returnLocalVariableZero);

			//
			// obj.GetType() != typeof (<TYPE>)
			//

			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, ObjectGetTypeMethod);

			il.Emit(OpCodes.Ldtoken, typeBuilder);
			il.Emit(OpCodes.Call, GetTypeFromHandleMethod);

			il.Emit(OpCodes.Call, TypeEqualityOperator);

			il.Emit(OpCodes.Brfalse_S, returnLocalVariableZero);

			//
			// Equals((<TYPE>) obj)
			//

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);

			il.Emit(OpCodes.Castclass, typeBuilder);

			il.Emit(OpCodes.Call, virtualEqualsMethod);

			il.Emit(OpCodes.Stloc_0);

			//
			// return
			//

			il.MarkLabel(returnLocalVariableZero);
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(method, ObjectEqualsMethod);

			return method;
		}
	}
}
