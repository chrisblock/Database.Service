using System;

namespace Database.Core
{
	public class Database : IEquatable<Database>
	{
		public DatabaseType DatabaseType { get; set; }
		public string ServerName { get; set; }
		public string DatabaseName { get; set; }

		public virtual bool Equals(Database other)
		{
			var result = false;

			if (ReferenceEquals(null, other))
			{
				result = false;
			}
			else if (ReferenceEquals(this, other))
			{
				result = true;
			}
			else
			{
				result = Equals(other.ServerName, ServerName) && Equals(other.DatabaseName, DatabaseName) && Equals(other.DatabaseType, DatabaseType);
			}

			return result;
		}

		public override bool Equals(object obj)
		{
			var result = false;

			if (ReferenceEquals(null, obj))
			{
				result = false;
			}
			else if (ReferenceEquals(this, obj))
			{
				result = true;
			}
			else if (obj.GetType() != typeof (Database))
			{
				result = false;
			}
			else
			{
				result = Equals((Database)obj);
			}

			return result;
		}

		public override int GetHashCode()
		{
			var str = ToString();

			return str.GetHashCode();
		}

		public override string ToString()
		{
			return String.Format("Type:{0};ServerName:{1};DatabaseName:{2};", DatabaseType, ServerName, DatabaseName);
		}
	}
}
