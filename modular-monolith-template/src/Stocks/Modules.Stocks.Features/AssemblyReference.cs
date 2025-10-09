using System.Reflection;

namespace Modules.Stocks.Features;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
