namespace Database.Core.Querying
{
	public interface IConnectionStringFactory
	{
		string Create(Database database);
	}
}
