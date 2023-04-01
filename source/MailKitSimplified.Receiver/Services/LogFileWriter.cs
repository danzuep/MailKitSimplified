using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Abstractions;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public class LogFileWriter : IFileWriter, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly FileWriterOptions _fileWriteOptions;
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private CancellationTokenSource _cts = null;

        public LogFileWriter(ILogger<LogFileWriter> logger = null, IFileSystem fileSystem = null, IOptions<FileWriterOptions> options = null) :
            this(options, logger, fileSystem)
        {
        }

        private LogFileWriter(IOptions<FileWriterOptions> options, ILogger logger = null, IFileSystem fileSystem = null)
        {
            _logger = logger ?? NullLogger<LogFileWriter>.Instance;
            _fileSystem = fileSystem ?? new FileSystem();
            _fileWriteOptions = options?.Value ?? new FileWriterOptions();
            Initialise();
        }

        public static LogFileWriter Create(FileWriterOptions fileWriteOptions, ILogger logger = null, IFileSystem fileSystem = null)
        {
            var options = Options.Create(fileWriteOptions);
            var protocolLogger = new LogFileWriter(options, logger, fileSystem);
            return protocolLogger;
        }

        public void Write(string textToEnqueue) => _queue.Enqueue(textToEnqueue);

        private async Task WriteToFile()
        {
            try
            {
                var directoryName = _fileSystem.Path.GetDirectoryName(_fileWriteOptions.FilePath);
                if (!string.IsNullOrWhiteSpace(directoryName))
                    _fileSystem.Directory.CreateDirectory(directoryName);
                _logger.LogDebug($"Writing buffered text to file: {_fileWriteOptions.FilePath}");
                using (var streamWriter = _fileWriteOptions.AppendToExisting ?
                    _fileSystem.File.AppendText(_fileWriteOptions.FilePath) :
                    _fileSystem.File.CreateText(_fileWriteOptions.FilePath))
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        if (_queue.TryDequeue(out string textLine))
                            await streamWriter.WriteAsync(textLine).ConfigureAwait(false);
                        else if (_queue.IsEmpty)
                            await Task.Delay(_fileWriteOptions.FileWriteMaxDelayMs, _cts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("Text file writing queue cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        internal void Initialise()
        {
            _cts = new CancellationTokenSource();
            if (!string.IsNullOrWhiteSpace(_fileWriteOptions.FilePath))
            {
                Task.Run(WriteToFile);
            }
        }

        public void ResetLogQueue()
        {
            CancelLogQueue();
            Initialise();
        }

        internal void CancelLogQueue()
        {
            if (_cts != null)
            {
                _cts.Cancel(false);
#if NET5_0_OR_GREATER
                _queue.Clear();
#endif
            }
        }

        public void Dispose()
        {
            CancelLogQueue();
        }
    }
}
