using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AssemblytoVisible")]

namespace Modules.Carriers.Features;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
