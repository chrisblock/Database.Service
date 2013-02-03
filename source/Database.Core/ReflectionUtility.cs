using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Database.Core
{
	public static class ReflectionUtility
	{
		public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
		{
			var methodCall = expression.Body as MethodCallExpression;

			if (methodCall == null)
			{
				throw new ArgumentException(String.Format("'{0}' was not a method call expression.", expression.Body));
			}

			return methodCall.Method.IsGenericMethod
				? methodCall.Method.GetGenericMethodDefinition()
				: methodCall.Method;
		}

		public static MethodInfo GetMethodInfo<T>(Expression<Func<T>> expression)
		{
			var methodCall = expression.Body as MethodCallExpression;

			if (methodCall == null)
			{
				throw new ArgumentException(String.Format("'{0}' was not a method call expression.", expression.Body));
			}

			return methodCall.Method.IsGenericMethod
				? methodCall.Method.GetGenericMethodDefinition()
				: methodCall.Method;
		}

		public static MethodInfo GetMethodInfo<T, TMethodResult>(Expression<Func<T, TMethodResult>> expression)
		{
			var methodCall = expression.Body as MethodCallExpression;

			if (methodCall == null)
			{
				throw new ArgumentException(String.Format("'{0}' was not a method call expression.", expression.Body));
			}

			return methodCall.Method.IsGenericMethod
				? methodCall.Method.GetGenericMethodDefinition()
				: methodCall.Method;
		}
	}
}
