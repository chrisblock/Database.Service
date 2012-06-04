using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Database.Core.TableReflection
{
	public interface ITableReflector
	{
		TableDefinition GetTableDefinition(string serverName, string databaseName, string tableName);
	}
}
