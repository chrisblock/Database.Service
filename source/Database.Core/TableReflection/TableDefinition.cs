using System;
using System.Collections.Generic;

namespace Database.Core.TableReflection
{
	public class TableDefinition
	{
		public string Name { get; set; }
		public ICollection<ColumnDefinition> Columns { get; set; }

		public string GetEntityName()
		{
			return String.Format("{0}", Name);
		}

		public string GetMapName()
		{
			return String.Format("{0}Map", Name);
		}
	}
}
