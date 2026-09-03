using System.IO;
using System.Text;

namespace Disharmony.Tests;

[SetUpFixture]
public sealed class TestRunSetup
{
    private StreamWriter? harmonyLogWriter;

    [OneTimeSetUp]
    public void ConfigureHarmonyLog()
    {
        string logPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "disharmony-tests.log");
        var logStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        harmonyLogWriter = new StreamWriter(logStream, new UTF8Encoding(false)) { AutoFlush = true };

        FileLog.SetBuffer([]);
        FileLog.indentLevel = 0;
        FileLog.LogWriter = harmonyLogWriter;

        TestContext.Progress.WriteLine($"Harmony log: {logPath}");
    }

    [OneTimeTearDown]
    public void CloseHarmonyLog()
    {
        try
        {
            FileLog.FlushBuffer();
        }
        finally
        {
            FileLog.LogWriter = null!;
            harmonyLogWriter?.Dispose();
        }
    }
}
