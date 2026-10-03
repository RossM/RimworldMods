[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $TracePath,
    [int] $ProcessId = 46364,
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '../../TestResults/ReflectionProfile'),
    [string] $PrivateAssemblies = 'C:/Program Files/Microsoft Visual Studio/18/Enterprise/Common7/IDE/PrivateAssemblies'
)

$ErrorActionPreference = 'Stop'
$traceAssembly = Join-Path $PrivateAssemblies 'Microsoft.Diagnostics.Tracing.TraceEvent.dll'
$serialization = Join-Path $PrivateAssemblies 'Microsoft.Diagnostics.FastSerialization.dll'
Add-Type -Path $serialization
Add-Type -Path $traceAssembly
Add-Type -ReferencedAssemblies @($traceAssembly, $serialization, 'System.Collections', 'System.Runtime', 'System.Runtime.Extensions', 'netstandard', 'System.Linq.Expressions', 'System.Linq') -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
public static class ReflectionProfileSummary
{
    public static void Run(string path, string output, int processId)
    {
        using (var log = TraceLog.OpenOrConvert(path))
        using (var writer = new StreamWriter(output))
        {
            var inclusive = new Dictionary<string, long>();
            var underLookup = new Dictionary<string, long>();
            long total = 0, withStack = 0, lookup = 0;
            foreach (var e in log.Events)
            {
                if (e.ProcessID != processId || e.EventName != "PerfInfo/Sample") continue;
                total++;
                var stack = e.CallStack();
                if (stack == null) continue;
                withStack++;
                var frames = new HashSet<string>();
                for (var frame = stack; frame != null; frame = frame.Caller)
                {
                    string name = frame.CodeAddress.FullMethodName;
                    if (!String.IsNullOrEmpty(name)) frames.Add(name);
                }
                bool inLookup = frames.Any(f => f.Contains("Disharmony.ReflectionTools.GetMembers"));
                if (inLookup) lookup++;
                foreach (string name in frames)
                {
                    long value;
                    inclusive.TryGetValue(name, out value); inclusive[name] = value + 1;
                    if (inLookup) { underLookup.TryGetValue(name, out value); underLookup[name] = value + 1; }
                }
            }
            writer.WriteLine("Total samples=" + total + "; with stacks=" + withStack + "; GetMembers=" + lookup);
            writer.WriteLine("Duration ms=" + log.SessionDuration.TotalMilliseconds + "; lost=" + log.EventsLost);
            writer.WriteLine("Managed names from ETL; no native symbol lookup. Inclusive counts overlap.");
            writer.WriteLine("Top inclusive methods:");
            foreach (var pair in inclusive.OrderByDescending(p => p.Value).Take(60)) writer.WriteLine(pair.Value + " " + pair.Key);
            writer.WriteLine("Under GetMembers:");
            foreach (var pair in underLookup.OrderByDescending(p => p.Value).Take(60)) writer.WriteLine(pair.Value + " " + pair.Key);
            writer.WriteLine("ReflectionTools and mod constructors:");
            foreach (var pair in inclusive.Where(p => p.Key.Contains("Disharmony.ReflectionTools") || p.Key.Contains("XylXenos.Main") || p.Key.Contains("Xylib.Mod_Xylib")).OrderByDescending(p => p.Value))
                writer.WriteLine(pair.Value + " " + pair.Key);
        }
    }
}
'@

New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $TracePath).Path)
try {
    $entries = @($archive.Entries | Where-Object FullName -like '*.etl')
    if ($entries.Count -ne 1) { throw 'Expected exactly one ETL payload.' }
    $etlPath = Join-Path $OutputDirectory 'profile.etl'
    [IO.Compression.ZipFileExtensions]::ExtractToFile($entries[0], $etlPath, $true)
}
finally { $archive.Dispose() }
[ReflectionProfileSummary]::Run($etlPath, (Join-Path $OutputDirectory 'summary.txt'), $ProcessId)
