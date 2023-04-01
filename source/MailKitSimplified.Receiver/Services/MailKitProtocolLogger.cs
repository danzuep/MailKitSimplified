using MailKit;
using System;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MailKitSimplified.Receiver.Models;
using MailKitSimplified.Receiver.Abstractions;
using System.IO.Abstractions;

namespace MailKitSimplified.Receiver.Services
{
    public class MailKitProtocolLogger : IProtocolLogger, IDisposable
    {
        public IAuthenticationSecretDetector AuthenticationSecretDetector
        {
            get => _nullLogger.AuthenticationSecretDetector;
            set => _nullLogger.AuthenticationSecretDetector = value;
        }

        private readonly ILogger _logger;
        private readonly IFileWriter _fileWriter;
        private readonly ProtocolLoggerOptions _protocolLoggerOptions;
        private readonly IProtocolLogger _nullLogger = new ProtocolLogger(Stream.Null);

        public MailKitProtocolLogger(ILogger<MailKitProtocolLogger> logger = null, IFileWriter fileWriter = null, IOptions<ProtocolLoggerOptions> options = null, IFileSystem fileSystem = null)
        {
            _logger = logger ?? NullLogger<MailKitProtocolLogger>.Instance;
            _protocolLoggerOptions = options?.Value ?? new ProtocolLoggerOptions();
            _fileWriter = fileWriter ?? LogFileWriter.Create(_protocolLoggerOptions.FileWriter, logger, fileSystem);
        }

        public static MailKitProtocolLogger Create(ProtocolLoggerOptions protocolLoggerOptions, ILogger<MailKitProtocolLogger> logger = null, IFileSystem fileSystem = null)
        {
            var options = Options.Create(protocolLoggerOptions);
            var fileWriter = LogFileWriter.Create(protocolLoggerOptions.FileWriter, logger, fileSystem);
            var protocolLogger = new MailKitProtocolLogger(logger, fileWriter, options);
            return protocolLogger;
        }

        [Obsolete("Use ProtocolLoggerOptions or use any file logger that implements ILogger (e.g NLog or Serilog) instead.")]
        public IProtocolLogger SetLogFilePath(string logFilePath = null, bool appendToExisting = false, bool useTimestamp = false, bool redactSecrets = true)
        {
            if (logFilePath != _protocolLoggerOptions.FileWriter.FilePath)
                _protocolLoggerOptions.FileWriter.FilePath = logFilePath;
            _protocolLoggerOptions.FileWriter.AppendToExisting = appendToExisting;
            _protocolLoggerOptions.TimestampFormat = useTimestamp == true ?
                ProtocolLoggerOptions.DefaultTimestampFormat : null;
            if (redactSecrets == false)
                _nullLogger.AuthenticationSecretDetector = null;
            return this;
        }

        private bool _clientMidline;
        private bool _serverMidline;

        private void Log(byte[] buffer, int offset, int count, bool isClient)
        {
            if (_protocolLoggerOptions.FileWriter.FilePath == null)
                return;
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException("count");

            var sb = new StringBuilder();
            int num = offset + count;
            int i = offset;
            while (i < num)
            {
                int num2 = i;
                for (; i < num && buffer[i] != 10; i++) { }

                if (!(isClient ? _clientMidline : _serverMidline))
                {
                    LogTimestamp(sb).Append(isClient ? _protocolLoggerOptions.ClientPrefix : _protocolLoggerOptions.ServerPrefix);
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

                if (isClient && AuthenticationSecretDetector != null)
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

            var textToWrite = sb.ToString();
            _logger.LogTrace(textToWrite);
            _fileWriter.Write(textToWrite);
        }

        public void LogConnect(Uri uri)
        {
            var sb = new StringBuilder();
            LogTimestamp(sb).Append($"Connected to {uri}");

            if (_clientMidline || _serverMidline)
            {
                _clientMidline = false;
                _serverMidline = false;
            }

            var textToWrite = sb.ToString();
            _logger.LogTrace(textToWrite);
            _fileWriter.Write(textToWrite);
        }

        private StringBuilder LogTimestamp(StringBuilder sb)
        {
            if (_protocolLoggerOptions.TimestampFormat != null)
            {
                sb.Append(DateTimeOffset.Now.ToString(_protocolLoggerOptions.TimestampFormat, CultureInfo.InvariantCulture));
                sb.Append(' ');
            }
            return sb;
        }

        public void LogServer(byte[] buffer, int offset, int count) => Log(buffer, offset, count, isClient: false);

        public void LogClient(byte[] buffer, int offset, int count) => Log(buffer, offset, count, isClient: true);

        public void Dispose()
        {
            _nullLogger?.Dispose();
            _fileWriter.Dispose();
        }
    }
}
