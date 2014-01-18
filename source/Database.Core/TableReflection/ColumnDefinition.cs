using System;

namespace Database.Core.TableReflection
{
	public class ColumnDefinition
	{
		public string Name { get; set; }
		public Type Type { get; set; }
		public short Length { get; set; }
		public byte Scale { get; set; }
		public byte Precision { get; set; }
		public bool IsNullable { get; set; }
		public bool IsIdentity { get; set; }
		public bool IsPrimaryKeyColumn { get; set; }
	}
}
