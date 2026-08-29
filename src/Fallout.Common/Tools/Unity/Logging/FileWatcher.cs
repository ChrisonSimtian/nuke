using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Fallout.Common.Tools.Unity.Logging;

internal class FileWatcher
{
    private readonly string file;
    private readonly Action<string> processLineAction;
    private AutoResetEvent logResetEvent;
    private FileSystemWatcher fileSystemWatcher;
    private Thread logReaderThread;
    private CancellationTokenSource cancellationTokenSource;
    private readonly Encoding encoding;

    public FileWatcher(string file, Action<string> processLineAction, Encoding encoding = null)
    {
        this.encoding = encoding ?? Encoding.UTF8;
        this.file = file;
        this.processLineAction = processLineAction;
    }

    public void Start()
    {
        logResetEvent = new AutoResetEvent(initialState: false);
        fileSystemWatcher = new FileSystemWatcher(Path.GetPathRoot(file).NotNull())
        {
            Filter = Path.GetFileName(file),
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite
        };

        fileSystemWatcher.Changed += (_, _) => logResetEvent.Set();

        cancellationTokenSource = new CancellationTokenSource();
        logReaderThread = new Thread(ReadLogFile);
        logReaderThread.Start();
    }

    public void AssertStopped()
    {
        fileSystemWatcher.EnableRaisingEvents = false;
        fileSystemWatcher.Dispose();
        fileSystemWatcher = null;
        cancellationTokenSource.Cancel();
        while (logReaderThread != null)
        {
            if (!logReaderThread.IsAlive)
            {
                logReaderThread = null;
            }
            else
            {
                Thread.Sleep(millisecondsTimeout: 100);
            }
        }
    }

    // ReSharper disable once CognitiveComplexity
    private void ReadLogFile()
    {
        while (!File.Exists(file))
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            logResetEvent.WaitOne(millisecondsTimeout: 100);
        }

        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream, encoding);

        var currentLine = "";
        while (true)
        {
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var currentChar = reader.ReadChar();

                if (currentChar == '\n')
                {
                    processLineAction?.Invoke(currentLine);
                    currentLine = "";
                }
                else
                {
                    currentLine += currentChar;
                }
            }

            if (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }

            logResetEvent.WaitOne(millisecondsTimeout: 100);
        }
    }
}
