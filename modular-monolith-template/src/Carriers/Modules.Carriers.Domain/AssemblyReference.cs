using System.Reflection;

namespace Modules.Carriers.Domain;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
