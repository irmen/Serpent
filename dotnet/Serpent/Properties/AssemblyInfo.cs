#region Using directives

using System;
using System.Reflection;
using System.Runtime.InteropServices;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Razorvine.Serpent")]
[assembly: AssemblyDescription(@"Serpent Python literal expression serialization.

Serpent provides Python ast.literal_eval() compatible object tree serialization.
It serializes an object tree into bytes that can be transferred to Python and
decoded there (using ast.literal_eval()). It can ofcourse also deserialize
such a Python expression itself, back into the equivalent .NET datatypes.

More info for the Python version is on Pypi: https://pypi.python.org/pypi/serpent
")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Irmen de Jong")]
[assembly: AssemblyProduct("Serpent Python literal expression serialization")]
[assembly: AssemblyCopyright("Copyright Irmen de Jong")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// The assembly version has following format :
//
// Major.Minor.Build.Revision
//
// You can specify all the values or you can use the default the Revision and 
// Build Numbers by using the '*' as shown below:
[assembly: AssemblyVersion("1.16.0.*")]
[assembly: AssemblyFileVersion("1.16.0.0")]
