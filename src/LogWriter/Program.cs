using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shared;

namespace LogWriter
{
    class Program
    {
        private static TimeSpan _interval = TimeSpan.FromSeconds(1);

        static async Task Main(string[] args)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            Task t = WriteToFileAsync(cts.Token);
            HandleFrequency(cts);
            await t;
        }

        static async Task WriteToFileAsync(CancellationToken ct)
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(LogSettings.FILE_PATH);
                var logEntityFactory = new LogEntityFactory();

                Log($"Checking if folder {directoryPath} exists...");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                Log($"Using file {LogSettings.FILE_PATH} for logging...");

                using (Stream stream = File.Open(LogSettings.FILE_PATH, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (TextWriter writer = new StreamWriter(stream))
                {
                    Stopwatch sw = Stopwatch.StartNew();

                    while (true)
                    {
                        // Give some feedback that we are doing something ... :)
                        if (sw.Elapsed > TimeSpan.FromSeconds(5))
                        {
                            Log("Still writing...");
                            sw.Restart();
                        }

                        var log = logEntityFactory.GetRandomLogEntity();
                        await writer.WriteLineAsync(log.ToString());

                        // Do we need to stop?
                        if (!ct.IsCancellationRequested)
                        {
                            await Task.Delay(_interval);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Oops! Hit exception: {ex}");
            }
        }

        static void HandleFrequency(CancellationTokenSource cts)
        {
            bool shouldContinue = true;

            Log("Please press '+' for increasing the frequency of the logs, '-' for decreasing and 'x' for stopping");

            while (shouldContinue)
            {
                var key = Console.ReadKey();
                switch (key.KeyChar)
                {
                    case '+':
                        // we halve the interval at which we put logs into the file
                        _interval /= 2;
                        break;
                    case '-':
                        // we double the interval at which we put logs to the file
                        _interval *= 2;
                        break;
                    case 'x':
                    case 'X':
                        // Quit
                        cts.Cancel();
                        shouldContinue = false;
                        break;
                    default:
                        break;
                }
            }
        }

        static void Log(string format, params object[] args)
        {
            // TODO: Maybe replace with real logging infrastructure, if needed?
            Console.WriteLine(format, args);
        }
    }
}
