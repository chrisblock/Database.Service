using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Database.Core.TypeBuilding
{
	public interface IDynamicAssemblyBuilder
	{
		string BuildAssemblyQualifiedTypeName(string className);
		Type GetBuiltType(string typeName);
		TypeBuilder BuildType(string typeName, TypeAttributes typeAttributes, Type baseClassType = null);
		void Save();
	}
}
