namespace Database.Core.TableReflection
{
	public interface IConnectionStringFactory
	{
		string Create(Database database);
	}
}
