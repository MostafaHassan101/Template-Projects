using System.Reflection;

namespace Modules.Carriers.PublicApi;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
