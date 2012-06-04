using System;

using Database.Core.TableReflection;

namespace Database.Core.TypeBuilding
{
	public interface ITypeBuilder
	{
		Type Build(TableDefinition table);
	}
}
