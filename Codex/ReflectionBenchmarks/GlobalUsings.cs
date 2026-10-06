global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Reflection.Emit;
global using System.Runtime.CompilerServices;
global using Disharmony;
global using HarmonyLib;
global using NUnit.Framework;

namespace Disharmony;

// The source-linked lookup needs this internal extension; keep its behavior identical to production.
internal static class BenchmarkReflectionExtensions
{
    extension(Type type)
    {
        internal bool IsClosureType
        {
            get
            {
                if (type.IsByRef) type = type.GetElementType()!;
                return type.Name.StartsWith("<>c") && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute));
            }
        }
    }
}
