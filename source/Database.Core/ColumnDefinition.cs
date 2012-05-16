using System;

namespace Database.Core
{
	public class ColumnDefinition
	{
		public string Name { get; set; }
		public Type Type { get; set; }
		public bool IsPrimaryKeyColumn { get; set; }
	}
}
