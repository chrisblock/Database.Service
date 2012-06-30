using Database.Core.TableReflection;

namespace Database.Core.TypeBuilding
{
	public interface IDynamicAssemblyManager
	{
		EntityTypes BuildTypesFor(TableDefinition tableDefinition);
	}
}
