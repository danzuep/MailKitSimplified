using System;
using System.Text;
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
    public class LogFileWriterQueue : ILogFileWriter, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly FileWriterOptions _fileWriteOptions;
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private CancellationTokenSource _cts = null;

        public LogFileWriterQueue(IOptions<FileWriterOptions> options, ILogger<LogFileWriterQueue> logger = null, IFileSystem fileSystem = null)
        {
            _logger = logger ?? NullLogger<LogFileWriterQueue>.Instance;
            _fileSystem = fileSystem ?? new FileSystem();
            _fileWriteOptions = options?.Value ?? new FileWriterOptions();
            Initialise();
        }

        public LogFileWriterQueue SetFilePath(string logFilePath = null)
        {
            if (logFilePath != _fileWriteOptions.FilePath)
                _fileWriteOptions.FilePath = logFilePath;
            return this;
        }

        public void WriteLine(string textToWrite)
        {
            _queue.Enqueue(textToWrite);
        }

        public void Write(StringBuilder textToWrite)
        {
            string textToEnqueue = RemoveLastCharacter(textToWrite, '\n', '\r');
            _queue.Enqueue(textToEnqueue);
        }

        public static string RemoveLastCharacter(StringBuilder sb, params char[] toRemove)
        {
            foreach (char c in toRemove)
            {
                var lastCharacter = sb[sb.Length - 1];
                var endsWithNewLine = lastCharacter == c;
                if (endsWithNewLine) sb.Length--;
            }
            return sb.ToString();
        }

        public async Task<string> ReadAllTextAsync()
        {
            do
            {
                await Task.Delay(_fileWriteOptions.FileWriteMaxDelayMs, _cts.Token).ConfigureAwait(false);
            }
            while (!_queue.IsEmpty);
            var textFromFile = _fileSystem.File.ReadAllText(_fileWriteOptions.FilePath);
            return textFromFile;
        }

        private async Task WriteToFileAsync()
        {
            _logger.LogDebug($"Writing buffered text to file: {_fileWriteOptions.FilePath}");
            try
            {
                var directoryName = _fileSystem.Path.GetDirectoryName(_fileWriteOptions.FilePath);
                if (!string.IsNullOrWhiteSpace(directoryName))
                    _fileSystem.Directory.CreateDirectory(directoryName);
                using (var streamWriter = _fileWriteOptions.AppendToExisting ?
                    _fileSystem.File.AppendText(_fileWriteOptions.FilePath) :
                    _fileSystem.File.CreateText(_fileWriteOptions.FilePath))
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        if (_queue.TryDequeue(out string textLine))
                        {
                            await streamWriter.WriteLineAsync(textLine).ConfigureAwait(false);
                            await streamWriter.FlushAsync().ConfigureAwait(false);
                        }
                        else if (_queue.IsEmpty)
                        {
                            await Task.Delay(_fileWriteOptions.FileWriteMaxDelayMs, _cts.Token).ConfigureAwait(false);
                        }
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

        private void Initialise()
        {
            _cts = new CancellationTokenSource();
            if (!string.IsNullOrWhiteSpace(_fileWriteOptions.FilePath))
            {
                Task.Run(WriteToFileAsync);
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

        public override string ToString() => _fileWriteOptions.ToString();
    }
}
