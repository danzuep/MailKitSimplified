using System;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKit;

namespace MailKitSimplified.Receiver.Services
{
    public class MailKitProtocolLogger : IProtocolLogger
    {
        public IAuthenticationSecretDetector AuthenticationSecretDetector
        {
            get => _protocolLogger?.AuthenticationSecretDetector;
            set
            {
                if (_protocolLogger != null)
                    _protocolLogger.AuthenticationSecretDetector = value;
            }
        }

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IProtocolLogger _protocolLogger;

        public MailKitProtocolLogger(string logFilePath = null, IFileSystem fileSystem = null, ILogger<MailKitProtocolLogger> logger = null)
        {
            _logger = logger ?? NullLogger<MailKitProtocolLogger>.Instance;
            if (!string.IsNullOrEmpty(logFilePath))
            {
                _fileSystem = fileSystem ?? new FileSystem();
                var directoryName = _fileSystem.Path.GetDirectoryName(logFilePath);
                _fileSystem.Directory.CreateDirectory(directoryName);
            }
            _protocolLogger = logFilePath == null ? null :
                string.IsNullOrWhiteSpace(logFilePath) ?
                    new ProtocolLogger(Console.OpenStandardError()) :
                        new ProtocolLogger(logFilePath);
            if (!string.IsNullOrWhiteSpace(logFilePath))
                _logger.LogDebug($"Logs saving to file: {logFilePath}");
        }

        public void LogConnect(Uri uri)
        {
            _logger.LogTrace($"Connect: URI={uri}");
            _protocolLogger?.LogConnect(uri);
        }

        public void LogServer(byte[] buffer, int offset, int count)
        {
            _logger.LogTrace($"Server: buffer={buffer:X}, offset={offset}, count={count}");
            _protocolLogger?.LogServer(buffer, offset, count);
        }

        public void LogClient(byte[] buffer, int offset, int count)
        {
            _logger.LogTrace($"Client: buffer={buffer:X}, offset={offset}, count={count}");
            _protocolLogger?.LogClient(buffer, offset, count);
        }

        public void Dispose()
        {
            _protocolLogger?.Dispose();
        }
    }
}
