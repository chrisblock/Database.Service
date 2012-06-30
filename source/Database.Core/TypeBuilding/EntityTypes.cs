using System;

using Database.Core.TableReflection;

namespace Database.Core.TypeBuilding
{
	public class EntityTypes
	{
		public Type EntityType { get; set; }
		public Type MappingType { get; set; }
		public TableDefinition TableDefinition { get; set; }
	}
}
