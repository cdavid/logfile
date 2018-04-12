using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LogReader.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogReader.Implementations
{
    public class FileReader : IFileReader
    {
        private readonly string _fileName;
        private readonly BufferBlock<Tuple<string, DateTime>> _destinationBlock;
        private readonly SemaphoreSlim trigger;
        private readonly ILogger _logger;

        /// <summary>
        /// A reader that tries to read as fast as possible and that
        /// puts the strings that it reads into a BufferBlock (for further processing).
        /// 
        /// The strings that it reads are tagged with the timestamp when the
        /// event was read (the event itself contains the timestamp when it was
        /// generated) - this timestamp can be useful in case we want to track how long
        /// the request took and track the event further through the processing pipeline.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="destinationBlock"></param>
        public FileReader(string fileName, BufferBlock<Tuple<string, DateTime>> destinationBlock)
        {
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _destinationBlock = destinationBlock ?? throw new ArgumentNullException(nameof(destinationBlock));

            if (!File.Exists(_fileName))
            {
                throw new ArgumentException(nameof(fileName));
            }

            trigger = new SemaphoreSlim(0, 1);
            _logger = Program.LoggerFactory.CreateLogger<FileReader>();
        }

        public async Task StartReadAsync(CancellationToken ct)
        {
            if (!File.Exists(_fileName))
            {
                throw new ArgumentException(nameof(_fileName));
            }

            // We want to open the file for reading and we can share the locks for both read and write.
            using (FileStream fs = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                // We skip over the contents of the file. I am assuming that we are not interested in the contents
                // of the file before we start reading from it.
                //
                // NOTE: This part will require further optimization in case that the files that we read are big.
                // If the files are big, the file will need to be read which will require a lot of time. A better approach
                // in those cases is to handle the read ourselves (using the FileStream directly):
                // * find the length of the file, walk back until we find the last \r\n and store that index
                // * have a FileSystemWatcher notification (same as below) for when the file changes
                // * when the file changes, read the bytes that were changed (from the index above to end of stream)
                // * transform the bytes to text (assume ASCII encoding)
                // * split the text by newline markers (\r\n)
                // * if the last line is not complete keep it around until the next read.
                //
                // The implementation below is a "shortcut" in the interest of time and space.
                var readLine = await sr.ReadLineAsync();
                while (readLine != null)
                {
                    readLine = await sr.ReadLineAsync();
                }

                // We make use here of a FileSystemWatcher that will signal us (via the trigger)
                // whenever the file was changed. When the file is changed, we try to read again.
                FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(_fileName))
                {
                    EnableRaisingEvents = true,
                    Filter = Path.GetFileName(_fileName)
                };
                fileSystemWatcher.Changed += (s, e) =>
                {
                    try
                    {
                        if (!ct.IsCancellationRequested && trigger.CurrentCount == 0)
                        {
                            trigger.Release();
                        }
                    }
                    catch (Exception)
                    {
                        // this can happen in cases when the semaphore is signaled after our CurrentCount check (race condition)
                        // (e.g.: when the events are added faster than they are read and sent to the queue)
                        // This is OK because the other thread will have to read anyway all the lines that are available in the file.
                    }
                };

                while (!ct.IsCancellationRequested)
                {
                    // We wait for a signal from the FileSystemWatcher - we try to decrement the
                    // semaphore here (initial value is 0), so we wait until the FileSystemWatcher
                    // releases (increments) the value.
                    await trigger.WaitAsync(ct);
                    readLine = await sr.ReadLineAsync();

                    _logger.LogTrace("Read from file: " + readLine);

                    // Depending on the implementation of the generator of data, we might get multiple notifications 
                    if (!string.IsNullOrWhiteSpace(readLine))
                    {
                        // Send the event to the sink so we can come back and keep reading
                        await _destinationBlock.SendAsync(new Tuple<string, DateTime>(readLine, DateTime.UtcNow));
                    }
                }
            }
        }
    }
}
