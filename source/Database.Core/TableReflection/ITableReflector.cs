namespace Database.Core.TableReflection
{
	public interface ITableReflector
	{
		TableDefinition GetTableDefinition(Database database, string tableName);
	}
}
