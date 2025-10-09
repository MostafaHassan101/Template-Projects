using System.Reflection;

namespace Modules.Stocks.Infrastructure;

public static class AssemblyReference
{
	public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
