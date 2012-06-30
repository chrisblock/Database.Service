using System;

using Database.Core.TableReflection;

namespace Database.Core.TypeBuilding
{
	public interface IMappingTypeBuilder
	{
		Type Build(TableDefinition table);
	}
}
