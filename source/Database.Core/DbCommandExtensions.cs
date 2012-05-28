using System;
using System.Data;

namespace Database.Core
{
	public static class DbCommandExtensions
	{
		public static void AddParameter(this IDbCommand command, string parameterName, object parameterValue)
		{
			if (command == null)
			{
				throw new ArgumentNullException("command", "Cannot add a parameter to a null command.");
			}

			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentException(String.Format("'{0}' is not a valid parameter name.", parameterName));
			}

			var parameter = command.CreateParameter();

			parameter.ParameterName = parameterName;
			parameter.Value = parameterValue;

			command.Parameters.Add(parameter);
		}
	}
}
