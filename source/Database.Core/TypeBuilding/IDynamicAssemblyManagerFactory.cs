namespace Database.Core.TypeBuilding
{
	public interface IDynamicAssemblyManagerFactory
	{
		IDynamicAssemblyManager Create(string assemblyName);
	}
}
