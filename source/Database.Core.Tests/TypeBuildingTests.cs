using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Database.Core.Tests
{
	[TestFixture]
	public class TypeBuildingTests
	{
		[Test]
		public void GetHashCodeIsGeneratedCorrectly()
		{
			var table = new TableDefinition
			{
				Name = "Table",
				Columns = new List<ColumnDefinition>
				{
					new ColumnDefinition
					{
						Name = "Column1",
						Type = typeof (Guid),
						IsPrimaryKeyColumn = true
					},
					new ColumnDefinition
					{
						Name = "Column2",
						Type = typeof (int),
						IsPrimaryKeyColumn = true
					}
				}
			};

			var tableTypes = DynamicAssemblyManager.BuildTypesForTable(table);

			var entityType = tableTypes.Item1;

			var entity = Activator.CreateInstance(entityType);

			var columnOneProperty = entityType.GetProperty("Column1");

			var guid = Guid.NewGuid();

			columnOneProperty.SetValue(entity, guid, new object[0]);

			var str = String.Format("Column1:{0};Column2:{1};", guid, 0);

			Assert.That(entity.GetHashCode(), Is.EqualTo(str.GetHashCode()));
		}
	}
}
