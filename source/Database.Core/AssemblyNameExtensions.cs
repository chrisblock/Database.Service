using System;
using System.Reflection;

namespace Database.Core
{
	public static class AssemblyNameExtensions
	{
		public static string BuildAssemblyQualifiedTypeName(this AssemblyName assemblyName, string typeName)
		{
			return String.Format("{0}.{1}", assemblyName.Name, typeName);
		}
	}
}
