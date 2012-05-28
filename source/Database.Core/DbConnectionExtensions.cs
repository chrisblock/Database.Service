using System;
using System.Data;

namespace Database.Core
{
	public static class DbConnectionExtensions
	{
		public static IDbCommand CreateCommand(this IDbConnection connection, string sql)
		{
			if (connection == null)
			{
				throw new ArgumentNullException("connection", "Cannot create a command on a null connection.");
			}

			if (String.IsNullOrWhiteSpace(sql))
			{
				throw new ArgumentException(String.Format("'{0}' is not valid SQL.", sql));
			}

			var command = connection.CreateCommand();

			command.CommandText = sql;

			return command;
		}
	}
}
