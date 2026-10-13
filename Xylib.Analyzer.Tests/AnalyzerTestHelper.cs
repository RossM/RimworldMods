using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace Xylib.Analyzer.Tests;

internal static class AnalyzerTestHelper
{
    internal const string Contracts = """
        namespace Disharmony
        {
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class PatchAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class PrefixAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public class PostfixAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true)]
            public class TargetAttribute : System.Attribute { }
            public class TargetsAttribute : TargetAttribute { }
        }
        namespace Xylib
        {
            [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
            public class FeatureAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class UsedFromXmlAttribute : System.Attribute { }
        }
        namespace Verse
        {
            public class AbilityCompProperties { }
            public interface IExposable { void ExposeData(); }
            public class ThingComp { public virtual void PostExposeData() { } }
            [System.AttributeUsage(System.AttributeTargets.Field)]
            public class UnsavedAttribute : System.Attribute { }
            public class Def { public virtual System.Collections.Generic.IEnumerable<string> ConfigErrors() { yield break; } }
            public class ColorGenerator { }
            public class CompProperties { }
            public class DefModExtension { }
            public class HediffCompProperties { }
            public class HediffGiver { }
            public class PatchOperation { }
            public class PawnRenderNodeProperties { }
        }
        namespace RimWorld
        {
            public class IngestionOutcomeDoer { }
            public class ScenPart { }
        }
        namespace HarmonyLib
        {
            public class HarmonyPatch : System.Attribute { }
            public class HarmonyPrefix : System.Attribute { }
            public class HarmonyPostfix : System.Attribute { }
            public class HarmonyTranspiler : System.Attribute { }
        }
        """;

    internal static async Task<ImmutableArray<Diagnostic>> Analyze(string source, string contracts = Contracts,
        MetadataReference? additionalReference = null)
    {
        MetadataReference[] references = [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];
        // Framework contracts are metadata, just as RimWorld and Disharmony are in a mod build.
        if (contracts.Length != 0)
        {
            var framework = CSharpCompilation.Create("Framework", [CSharpSyntaxTree.ParseText(contracts)], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var stream = new MemoryStream();
            var emit = framework.Emit(stream);
            Assert.That(emit.Success, Is.True, string.Join("\n", emit.Diagnostics));
            references = [.. references, MetadataReference.CreateFromImage(stream.ToArray())];
        }
        if (additionalReference is not null)
            references = [.. references, additionalReference];
        var compilation = CSharpCompilation.Create("Test",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.That(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty,
            "The fixture must compile before analyzer diagnostics are checked.");
        var diagnostics = await compilation.WithAnalyzers([new XylibAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        Assert.That(diagnostics.Where(d => d.Id == "AD0001"), Is.Empty, "The analyzer must not throw.");
        return diagnostics;
    }
}
