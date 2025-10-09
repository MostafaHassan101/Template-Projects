using System.Reflection;

namespace Modules.Shipments.Infrastructure;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
