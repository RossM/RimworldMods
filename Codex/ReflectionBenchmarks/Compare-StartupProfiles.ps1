[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $BeforeEtl,
    [Parameter(Mandatory)] [string] $AfterEtl,
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '../../TestResults/ReflectionProfile/Comparison'),
    [string] $PrivateAssemblies = 'C:/Program Files/Microsoft Visual Studio/18/Enterprise/Common7/IDE/PrivateAssemblies'
)
$ErrorActionPreference = 'Stop'
$traceAssembly = Join-Path $PrivateAssemblies 'Microsoft.Diagnostics.Tracing.TraceEvent.dll'
$serialization = Join-Path $PrivateAssemblies 'Microsoft.Diagnostics.FastSerialization.dll'
Add-Type -Path $serialization
Add-Type -Path $traceAssembly
Add-Type -ReferencedAssemblies @($traceAssembly, $serialization, 'System.Collections', 'System.Runtime', 'System.Runtime.Extensions', 'netstandard', 'System.Linq.Expressions', 'System.Linq') -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
public static class DisharmonyStartupComparison
{
    private sealed class Group
    {
        internal long Count;
        internal double First = Double.PositiveInfinity, Last;
        internal readonly Dictionary<string, long> Methods = new Dictionary<string, long>();
        internal readonly Dictionary<string, long> Owners = new Dictionary<string, long>();
        internal void Add(double time, HashSet<string> methods, string owner)
        {
            Count++; First = Math.Min(First, time); Last = Math.Max(Last, time);
            foreach (string name in methods) Increment(Methods, name);
            Increment(Owners, owner);
        }
    }
    private static void Increment(Dictionary<string, long> counts, string name)
    {
        long value; counts.TryGetValue(name, out value); counts[name] = value + 1;
    }
    private static bool IsDisharmony(string name) => name.StartsWith("Disharmony.", StringComparison.Ordinal) || name.StartsWith("dynamicClass.Disharmony.", StringComparison.Ordinal);
    public static void Run(string path, string directory)
    {
        Directory.CreateDirectory(directory);
        using (var log = TraceLog.OpenOrConvert(path))
        {
            var processes = log.Processes.Where(p => p.Name.IndexOf("RimWorld", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (processes.Length != 1) throw new InvalidOperationException("Expected one RimWorld process, found " + processes.Length);
            var process = processes[0];
            var groups = new Dictionary<string, Group>();
            long total = 0, withStack = 0;
            foreach (var e in log.Events)
            {
                if (e.ProcessID != process.ProcessID || e.EventName != "PerfInfo/Sample") continue;
                total++;
                var stack = e.CallStack();
                if (stack == null) continue;
                withStack++;
                var frames = new HashSet<string>();
                string owner = "(no Disharmony frame)";
                for (var frame = stack; frame != null; frame = frame.Caller)
                {
                    string name = frame.CodeAddress.FullMethodName;
                    if (String.IsNullOrEmpty(name)) continue;
                    int arguments = name.IndexOf('(');
                    if (arguments >= 0) name = name.Substring(0, arguments);
                    frames.Add(name);
                    if (owner == "(no Disharmony frame)" && IsDisharmony(name)) owner = name;
                }
                bool patching = frames.Any(f => f.StartsWith("Disharmony.Patcher.") || f.StartsWith("Disharmony.PatchRegistry."));
                bool reflection = frames.Any(f => f.StartsWith("Disharmony.ReflectionTools"));
                bool members = frames.Contains("Disharmony.ReflectionTools.GetMembers");
                bool xenos = frames.Any(f => f.StartsWith("dynamicClass.XylXenos.Main..ctor") || f.StartsWith("XylXenos.Main..ctor"));
                bool xylib = frames.Any(f => f.StartsWith("dynamicClass.Xylib.Mod_Xylib..ctor") || f.StartsWith("Xylib.Mod_Xylib..ctor"));
                bool ideos = frames.Any(f => f.StartsWith("XylIdeos.Main..cctor"));
                var scopes = new List<string> { "Process" };
                if (frames.Any(IsDisharmony)) scopes.Add("AnyDisharmony");
                if (patching) scopes.Add("Patching");
                if (frames.Contains("Disharmony.Patcher.PatchAll") || frames.Contains("Disharmony.Patcher.PatchCategory")) scopes.Add("PatchAllOrCategory");
                if (frames.Contains("Disharmony.Patcher.ForceApply")) scopes.Add("ForceApply");
                if (frames.Contains("Disharmony.PatchRegistry.ProcessAssembly")) scopes.Add("ProcessAssembly");
                if (members) scopes.Add("GetMembers");
                if (reflection) scopes.Add("ReflectionTools");
                if (patching && !reflection) scopes.Add("PatchingOutsideReflection");
                if (xenos) scopes.Add("XylXenosConstructor");
                if (xylib) scopes.Add("XylibConstructor");
                if (ideos) scopes.Add("XylIdeosInitializer");
                if (xenos && patching) scopes.Add("XylXenosPatching");
                if (xylib && patching) scopes.Add("XylibPatching");
                if (ideos && patching) scopes.Add("XylIdeosPatching");
                if (xenos && members) scopes.Add("XylXenosGetMembers");
                if (xylib && members) scopes.Add("XylibGetMembers");
                if (ideos && members) scopes.Add("XylIdeosGetMembers");
                foreach (string scope in scopes)
                {
                    Group group;
                    if (!groups.TryGetValue(scope, out group)) groups[scope] = group = new Group();
                    group.Add(e.TimeStampRelativeMSec, frames, owner);
                }
            }
            using (var writer = new StreamWriter(Path.Combine(directory, "summary.txt")))
            {
                writer.WriteLine("Process=" + process.Name + "; PID=" + process.ProcessID);
                writer.WriteLine("DurationMs=" + log.SessionDuration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("SampleIntervalMs=" + log.SampleProfileInterval.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Samples=" + total + "; WithStack=" + withStack + "; Lost=" + log.EventsLost);
                writer.WriteLine("ProcessCPUMs=" + process.CPUMSec.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("CPU estimates are sample count * sampling interval, not elapsed time. Inclusive scopes overlap.");
                writer.WriteLine("First/last times bound observed samples; they are not instrumented entry/exit timestamps.");
            }
            using (var metrics = new StreamWriter(Path.Combine(directory, "scopes.tsv")))
            using (var methods = new StreamWriter(Path.Combine(directory, "methods.tsv")))
            using (var owners = new StreamWriter(Path.Combine(directory, "owners.tsv")))
            {
                metrics.WriteLine("Scope\tSamples\tCpuMs\tFirstMs\tLastMs");
                methods.WriteLine("Scope\tSamples\tMethod");
                owners.WriteLine("Scope\tSamples\tNearestDisharmonyFrame");
                foreach (var pair in groups)
                {
                    Group group = pair.Value;
                    metrics.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2:F3}\t{3:F3}\t{4:F3}", pair.Key, group.Count, group.Count * log.SampleProfileInterval.TotalMilliseconds, group.First, group.Last));
                    foreach (var method in group.Methods.OrderByDescending(p => p.Value)) methods.WriteLine(pair.Key + "\t" + method.Value + "\t" + method.Key);
                    foreach (var owner in group.Owners.OrderByDescending(p => p.Value)) owners.WriteLine(pair.Key + "\t" + owner.Value + "\t" + owner.Key);
                }
            }
        }
    }
}
"@
[DisharmonyStartupComparison]::Run((Resolve-Path -LiteralPath $BeforeEtl).Path, [IO.Path]::GetFullPath((Join-Path $OutputDirectory 'Before')))
[DisharmonyStartupComparison]::Run((Resolve-Path -LiteralPath $AfterEtl).Path, [IO.Path]::GetFullPath((Join-Path $OutputDirectory 'After')))
