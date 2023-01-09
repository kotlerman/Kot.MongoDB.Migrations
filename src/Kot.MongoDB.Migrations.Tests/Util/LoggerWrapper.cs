using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System.IO;

namespace Kot.MongoDB.Migrations.Tests.Util
{
    internal class LoggerWrapper<T>
    {
        private readonly StringWriter _stringWriter;

        public ILogger<T> Logger { get; }

        public LoggerWrapper(bool withLogger)
        {
            if (withLogger)
            {
                _stringWriter = new StringWriter();

                var logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.TextWriter(_stringWriter, outputTemplate: "[{Level}] {Message}{NewLine}{Exception}")
                    .CreateLogger();

                Logger = new SerilogLoggerFactory(logger).CreateLogger<T>();
            }
        }

        public string GetLogString() => _stringWriter?.ToString();
    }
}
