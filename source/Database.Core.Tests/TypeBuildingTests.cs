using System;
using System.Collections.Generic;

using Database.Core.TableReflection;

using NUnit.Framework;

namespace Database.Core.Tests
{
	[TestFixture]
	public class TypeBuildingTests
	{
		private TableDefinition _tableDefinition;
		private Type _entityType;

		private void SetColumnOne(object entity, object value)
		{
			var property = _entityType.GetProperty("Column1");

			property.SetValue(entity, value, new object[0]);
		}

		private void SetColumnTwo(object entity, object value)
		{
			var property = _entityType.GetProperty("Column2");

			property.SetValue(entity, value, new object[0]);
		}

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			_tableDefinition = new TableDefinition
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

			var tableTypes = DynamicAssemblyManager.BuildTypesForTable(_tableDefinition);

			_entityType = tableTypes.Item1;
		}

		[Test]
		public void GetHashCodeIsGeneratedCorrectly()
		{
			var entity = Activator.CreateInstance(_entityType);

			var guid = Guid.NewGuid();
			var integer = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity, guid);
			SetColumnTwo(entity, integer);

			var str = String.Format("Column1:{0};Column2:{1};", guid, integer);

			Assert.That(entity.GetHashCode(), Is.EqualTo(str.GetHashCode()));
		}

		[Test]
		public void VirtualEquals_NullObject_ReturnsFalse()
		{
			var entity1 = Activator.CreateInstance(_entityType);
			object entity2 = null;

			var guid = Guid.NewGuid();
			var integer = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity1, guid);
			SetColumnTwo(entity1, integer);

			Assert.That(entity1, Is.Not.SameAs(entity2));

			var virtualEqualsMethod = _entityType.GetMethod("Equals", new[] { _entityType });

			var result = virtualEqualsMethod.Invoke(entity1, new[] { entity2 });

			Assert.That(result, Is.False);
		}

		[Test]
		public void VirtualEquals_SameReference_ReturnsTrue()
		{
			var entity1 = Activator.CreateInstance(_entityType);

			var guid = Guid.NewGuid();
			var integer = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity1, guid);
			SetColumnTwo(entity1, integer);

			var virtualEqualsMethod = _entityType.GetMethod("Equals", new[] { _entityType });

			var result = virtualEqualsMethod.Invoke(entity1, new[] { entity1 });

			Assert.That(result, Is.True);
		}

		[Test]
		public void VirtualEquals_EqualProperties_ReturnsTrue()
		{
			var entity1 = Activator.CreateInstance(_entityType);
			var entity2 = Activator.CreateInstance(_entityType);

			var guid = Guid.NewGuid();
			var integer = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity1, guid);
			SetColumnTwo(entity1, integer);

			SetColumnOne(entity2, guid);
			SetColumnTwo(entity2, integer);

			Assert.That(entity1, Is.Not.SameAs(entity2));

			var virtualEqualsMethod = _entityType.GetMethod("Equals", new[] { _entityType });

			var result = virtualEqualsMethod.Invoke(entity1, new[] { entity2 });

			Assert.That(result, Is.True);
		}

		[Test]
		public void VirtualEquals_UnEqualProperties_ReturnsFalse()
		{
			var entity1 = Activator.CreateInstance(_entityType);
			var entity2 = Activator.CreateInstance(_entityType);

			var guid1 = Guid.NewGuid();
			var guid2 = Guid.NewGuid();
			var integer1 = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();
			var integer2 = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity1, guid1);
			SetColumnTwo(entity1, integer1);

			SetColumnOne(entity2, guid2);
			SetColumnTwo(entity2, integer2);

			Assert.That(entity1, Is.Not.SameAs(entity2));

			var virtualEqualsMethod = _entityType.GetMethod("Equals", new[] { _entityType });

			var result = virtualEqualsMethod.Invoke(entity1, new[] { entity2 });

			Assert.That(result, Is.False);
		}

		[Test]
		public void OverrideEquals_NullObject_ReturnsFalse()
		{
			var entity1 = Activator.CreateInstance(_entityType);
			object entity2 = null;

			var guid = Guid.NewGuid();
			var integer = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity1, guid);
			SetColumnTwo(entity1, integer);

			Assert.That(entity1, Is.Not.SameAs(entity2));

			var overrideEqualsMethod = _entityType.GetMethod("Equals", new[] { typeof(object) });

			var result = overrideEqualsMethod.Invoke(entity1, new[] { entity2 });

			Assert.That(result, Is.False);
		}

		[Test]
		public void OverrideEquals_SameReference_ReturnsTrue()
		{
			var entity1 = Activator.CreateInstance(_entityType);

			var guid = Guid.NewGuid();
			var integer = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity1, guid);
			SetColumnTwo(entity1, integer);

			var overrideEqualsMethod = _entityType.GetMethod("Equals", new[] { typeof(object) });

			var result = overrideEqualsMethod.Invoke(entity1, new[] { entity1 });

			Assert.That(result, Is.True);
		}

		[Test]
		public void OverrideEquals_DifferentTypes_ReturnsFalse()
		{
			var entity1 = Activator.CreateInstance(_entityType);
			var entity2 = String.Empty;

			var guid = Guid.NewGuid();
			var integer = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity1, guid);
			SetColumnTwo(entity1, integer);

			Assert.That(entity1, Is.Not.SameAs(entity2));

			var overrideEqualsMethod = _entityType.GetMethod("Equals", new[] { typeof(object) });

			var result = overrideEqualsMethod.Invoke(entity1, new object[] { entity2 });

			Assert.That(result, Is.False);
		}

		[Test]
		public void OverrideEquals_EqualProperties_ReturnsTrue()
		{
			var entity1 = Activator.CreateInstance(_entityType);
			var entity2 = Activator.CreateInstance(_entityType);

			var guid = Guid.NewGuid();
			var integer = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity1, guid);
			SetColumnTwo(entity1, integer);

			SetColumnOne(entity2, guid);
			SetColumnTwo(entity2, integer);

			Assert.That(entity1, Is.Not.SameAs(entity2));

			var overrideEqualsMethod = _entityType.GetMethod("Equals", new[] { typeof(object) });

			var result = overrideEqualsMethod.Invoke(entity1, new[] { entity2 });

			Assert.That(result, Is.True);
		}

		[Test]
		public void OverrideEquals_UnEqualProperties_ReturnsFalse()
		{
			var entity1 = Activator.CreateInstance(_entityType);
			var entity2 = Activator.CreateInstance(_entityType);

			var guid1 = Guid.NewGuid();
			var guid2 = Guid.NewGuid();
			var integer1 = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();
			var integer2 = new Random(DateTime.Now.TimeOfDay.Milliseconds).Next();

			SetColumnOne(entity1, guid1);
			SetColumnTwo(entity1, integer1);

			SetColumnOne(entity2, guid2);
			SetColumnTwo(entity2, integer2);

			Assert.That(entity1, Is.Not.SameAs(entity2));

			var overrideEqualsMethod = _entityType.GetMethod("Equals", new[] { typeof (object) });

			var result = overrideEqualsMethod.Invoke(entity1, new[] { entity2 });

			Assert.That(result, Is.False);
		}
	}
}
