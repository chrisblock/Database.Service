using System;

using Database.Core.TableReflection;

namespace Database.Core.TypeBuilding
{
	public interface IEntityTypeBuilder
	{
		Type Build(TableDefinition table);
	}
}
