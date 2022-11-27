using MailKit;
using System;
using System.Text;
using System.IO;
using System.IO.Abstractions;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public class MailKitProtocolLogger : IProtocolLogger
    {
        public IAuthenticationSecretDetector AuthenticationSecretDetector
        {
            get => _protocolLogger.AuthenticationSecretDetector;
            set => _protocolLogger.AuthenticationSecretDetector = value;
        }

        private bool _clientMidline;
        private bool _serverMidline;

        private static readonly string _timestampFormat = "yyyy-MM-ddTHH:mm:ssZ";
        private static readonly string _serverPrefix = "S: ";
        private static readonly string _clientPrefix = "C: ";

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private IProtocolLogger _protocolLogger;
        private bool _redactSecrets;
        private bool _useTimestamp;

        public MailKitProtocolLogger(ILogger<MailKitProtocolLogger> logger = null, IProtocolLogger protocolLogger = null, IFileSystem fileSystem = null)
        {
            _logger = logger ?? NullLogger<MailKitProtocolLogger>.Instance;
            _protocolLogger = protocolLogger == null || protocolLogger is MailKitProtocolLogger ?
                new ProtocolLogger(Stream.Null) : protocolLogger;
            _fileSystem = fileSystem ?? new FileSystem();
        }

        public IProtocolLogger SetLogFilePath(string logFilePath = null, bool appendFile = false, bool useTimestamp = false, bool redactSecrets = true)
        {
            _useTimestamp = useTimestamp;
            _redactSecrets = redactSecrets;
            //bool isMockFileSystem = _fileSystem.GetType().Name == "MockFileSystem";
            if (logFilePath?.Equals("console", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                _protocolLogger = new ProtocolLogger(Console.OpenStandardError());
            }
            else if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                var directoryName = _fileSystem.Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrWhiteSpace(directoryName))
                    _fileSystem.Directory.CreateDirectory(directoryName);
                var mode = appendFile ? FileMode.Append : FileMode.Create;
                var stream = _fileSystem.File.Open(logFilePath, mode, FileAccess.Write, FileShare.Read);
                _protocolLogger = new ProtocolLogger(stream);
                _logger.LogDebug($"Saving logs to file: {logFilePath}");
            }
            return this;
        }

        private void Log(byte[] buffer, int offset, int count, bool isClient)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException("count");

            var sb = new StringBuilder(Environment.NewLine);
            int num = offset + count;
            int i = offset;
            while (i < num)
            {
                int num2 = i;
                for (; i < num && buffer[i] != 10; i++) { }

                if (!(isClient ? _clientMidline : _serverMidline))
                {
                    if (_useTimestamp)
                    {
                        sb.Append(DateTimeOffset.Now.ToString(_timestampFormat, CultureInfo.InvariantCulture));
                        sb.Append(' ');
                    }
                    sb.Append(isClient ? _clientPrefix : _serverPrefix);
                }

                if (i < num && buffer[i] == 10)
                {
                    if (isClient)
                        _clientMidline = false;
                    else
                        _serverMidline = false;
                    i++;
                }
                else if (isClient)
                {
                    _clientMidline = true;
                }
                else
                {
                    _serverMidline = true;
                }

                if (isClient && _redactSecrets && AuthenticationSecretDetector != null)
                {
                    IList<AuthenticationSecret> list = AuthenticationSecretDetector.DetectSecrets(buffer, num2, i - num2);
                    foreach (AuthenticationSecret item in list)
                    {
                        if (item.StartIndex > num2)
                        {
                            sb.Append(Encoding.UTF8.GetString(buffer, num2, item.StartIndex - num2));
                        }

                        num2 = item.StartIndex + item.Length;
                        sb.Append("********");
                    }
                }

                sb.Append(Encoding.UTF8.GetString(buffer, num2, i - num2));
            }

            using (_logger.BeginScope(nameof(MailKitProtocolLogger)))
                _logger.LogTrace(sb.ToString());

            if (isClient)
                _protocolLogger?.LogClient(buffer, offset, count);
            else
                _protocolLogger?.LogServer(buffer, offset, count);
        }

        public void LogConnect(Uri uri)
        {
            var sb = new StringBuilder(Environment.NewLine);
            if (_useTimestamp)
            {
                sb.Append(DateTimeOffset.Now.ToString(_timestampFormat, CultureInfo.InvariantCulture));
                sb.Append(' ');
            }
            sb.AppendLine($"Connected to {uri}");
            using (_logger.BeginScope(nameof(MailKitProtocolLogger)))
                _logger.LogTrace(sb.ToString());

            if (_clientMidline || _serverMidline)
            {
                _clientMidline = false;
                _serverMidline = false;
            }
            _protocolLogger?.LogConnect(uri);
        }

        public void LogServer(byte[] buffer, int offset, int count) => Log(buffer, offset, count, isClient: false);

        public void LogClient(byte[] buffer, int offset, int count) => Log(buffer, offset, count, isClient: true);

        public void Dispose() => _protocolLogger?.Dispose();
    }
}
