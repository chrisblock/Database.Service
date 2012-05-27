using System.Collections.Generic;

namespace Database.Core
{
	public class TableDefinition
	{
		public string Name { get; set; }
		public ICollection<ColumnDefinition> Columns { get; set; }
	}
}
