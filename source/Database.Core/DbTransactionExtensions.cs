using System;
using System.Data;

namespace Database.Core
{
	public static class DbTransactionExtensions
	{
		public static IDbCommand CreateCommand(this IDbTransaction transaction, string sql)
		{
			if (transaction == null)
			{
				throw new ArgumentNullException("transaction", "Cannot create a command for a null transaction.");
			}

			if (String.IsNullOrWhiteSpace(sql))
			{
				throw new ArgumentException(String.Format("'{0}' is not valid SQL.", sql));
			}

			if (transaction.Connection == null)
			{
				throw new InvalidOperationException("Cannot create an IDbCommand from an IDbTransaction not associated with an IDbConnection.");
			}

			var connection = transaction.Connection;

			var command = connection.CreateCommand(sql);

			command.Transaction = transaction;

			return command;
		}
	}
}
