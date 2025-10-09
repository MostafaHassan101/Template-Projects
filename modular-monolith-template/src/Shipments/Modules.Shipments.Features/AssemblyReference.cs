using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Modules.Shipments.Tests.Unit")]

namespace Modules.Shipments.Features;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
