using System.Data.SqlClient;

namespace Database.Core
{
	public static class SqlCommandExtensions
	{
		public static void AddParameter(this SqlCommand command, string parameterName, object parameterValue)
		{
			var parameter = command.CreateParameter();

			parameter.ParameterName = parameterName;
			parameter.Value = parameterValue;

			command.Parameters.Add(parameter);
		}
	}
}
