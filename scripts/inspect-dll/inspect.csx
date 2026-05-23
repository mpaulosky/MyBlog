using System.Reflection;
var dll = args[0];
var asm = Assembly.LoadFrom(dll);
foreach(var t in asm.GetExportedTypes().OrderBy(t=>t.FullName))
    Console.WriteLine(t.FullName);
