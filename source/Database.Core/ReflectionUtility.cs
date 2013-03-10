using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Database.Core
{
	public static class ReflectionUtility
	{
		public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
		{
			var result = GetMethodFromExpression(expression.Body);

			return result;
		}

		public static MethodInfo GetMethodInfo<T>(Expression<Func<T>> expression)
		{
			var result = GetMethodFromExpression(expression.Body);

			return result;
		}

		public static MethodInfo GetMethodInfo<T, TMethodResult>(Expression<Func<T, TMethodResult>> expression)
		{
			var result = GetMethodFromExpression(expression.Body);

			return result;
		}

		private static MethodInfo GetMethodFromExpression(Expression expression)
		{
			var methodCall = expression as MethodCallExpression;

			if (methodCall == null)
			{
				throw new ArgumentException(String.Format("'{0}' was not a method call expression.", expression));
			}

			return methodCall.Method.IsGenericMethod
				? methodCall.Method.GetGenericMethodDefinition()
				: methodCall.Method;
		}
	}
}
