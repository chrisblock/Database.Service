// ReSharper disable InconsistentNaming

using NUnit.Framework;

namespace Database.Core.Tests
{
	[TestFixture]
	public class DatabaseTests
	{
		[Test]
		public void Equals_Null_ReturnsFalse()
		{
			var database1 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			Database database2 = null;

			Assert.That(database1.Equals(database2), Is.False);
		}

		[Test]
		public void Equals_SameReference_ReturnsTrue()
		{
			var database1 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			Assert.That(database1.Equals(database1), Is.True);
		}

		[Test]
		public void Equals_SameServerNameDifferentDatabaseName_ReturnsFalse()
		{
			var database1 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			var database2 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Hello, World."
			};

			Assert.That(database1.Equals(database2), Is.False);
		}

		[Test]
		public void Equals_DifferentServerNameSameDatabaseName_ReturnsFalse()
		{
			var database1 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			var database2 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Hello, World.",
				DatabaseName = "Database"
			};

			Assert.That(database1.Equals(database2), Is.False);
		}

		[Test]
		public void Equals_DifferentServerNameDifferentDatabaseName_ReturnsFalse()
		{
			var database1 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			var database2 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Hello, World.",
				DatabaseName = "Goodbye, World."
			};

			Assert.That(database1.Equals(database2), Is.False);
		}

		[Test]
		public void Equals_SameServerNameSameDatabaseName_ReturnsFalse()
		{
			var database1 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			var database2 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			Assert.That(database1.Equals(database2), Is.True);
		}

		[Test]
		public void ObjectEquals_Null_ReturnsFalse()
		{
			var database1 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			object database2 = null;

			Assert.That(database1.Equals(database2), Is.False);
		}

		[Test]
		public void ObjectEquals_SameReference_ReturnsTrue()
		{
			var database1 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			object database2 = database1;

			Assert.That(database1.Equals(database2), Is.True);
		}

		[Test]
		public void ObjectEquals_DifferentTypes_ReturnsFalse()
		{
			var database1 = new Database
			{
				DatabaseType = DatabaseType.SqlServer,
				ServerName = "Server",
				DatabaseName = "Database"
			};

			object database2 = "The quick brown fox jumped over the lazy dog.";

			Assert.That(database1.Equals(database2), Is.False);
		}
	}
}
