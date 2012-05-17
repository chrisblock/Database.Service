using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Database.Core
{
	public static class TypeBuilderExtensions
	{
		public static void DefineProperty(this TypeBuilder typeBuilder, string propertyName, Type propertyType)
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
	}
}
