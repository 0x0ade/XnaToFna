using System;
using System.IO;
using System.Reflection;

static class GetAssemblyVersion
{
	static void Main(string[] args)
	{
		Console.Write(Assembly.LoadFile(args[0]).GetName().Version.ToString());
	}
}
