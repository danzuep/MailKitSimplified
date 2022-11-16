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
            get => _protocolLogger?.AuthenticationSecretDetector;
            set
            {
                if (_protocolLogger != null)
                    _protocolLogger.AuthenticationSecretDetector = value;
            }
        }

        private bool _clientMidline;
        private bool _serverMidline;

        private static readonly string _timestampFormat = "yyyy-MM-ddTHH:mm:ssZ";
        private static readonly string _serverPrefix = "S: ";
        private static readonly string _clientPrefix = "C: ";

        private readonly ILogger _logger;
        private IProtocolLogger _protocolLogger;
        private bool _redactSecrets;
        private bool _useTimestamp;

        public MailKitProtocolLogger(ILogger<MailKitProtocolLogger> logger = null)
        {
            _logger = logger ?? NullLogger<MailKitProtocolLogger>.Instance;
            _protocolLogger = logger == null ?
                new ProtocolLogger(Console.OpenStandardError()) :
                new ProtocolLogger(Stream.Null);
        }

        public IProtocolLogger SetLogFilePath(string logFilePath, bool appendFile = false, bool useTimestamp = false, bool redactSecrets = true, IFileSystem fileSystem = null)
        {
            _useTimestamp = useTimestamp;
            _redactSecrets = redactSecrets;
            if (string.IsNullOrEmpty(logFilePath))
            {
                _protocolLogger = logFilePath == null ?
                    new ProtocolLogger(Stream.Null) :
                    new ProtocolLogger(Console.OpenStandardError());
            }
            else
            {
                var _fileSystem = fileSystem ?? new FileSystem();
                var directoryName = _fileSystem.Path.GetDirectoryName(logFilePath);
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

        public void LogServer(byte[] buffer, int offset, int count)
        {
            Log(buffer, offset, count, isClient: false);
            _protocolLogger?.LogServer(buffer, offset, count);
        }

        public void LogClient(byte[] buffer, int offset, int count)
        {
            Log(buffer, offset, count, isClient: true);
            _protocolLogger?.LogClient(buffer, offset, count);
        }

        public void Dispose()
        {
            _protocolLogger?.Dispose();
        }
    }
}
