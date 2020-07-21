using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IngestTask.Server.Extensions
{
    public class CustomConsoleLoggerConfiguration
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Warning;
        public int EventId { get; set; } = 0;
        public ConsoleColor Color { get; set; } = ConsoleColor.Yellow;
    }

    
    public class CustomLogger : ILogger
    {
        private readonly string _name;
        private readonly CustomConsoleLoggerConfiguration _config;
        private static readonly Sobey.Core.Log.ILogger loggser = Sobey.Core.Log.LoggerManager.GetLogger("Orleans");
        public CustomLogger(string name, CustomConsoleLoggerConfiguration config)
        {
            _name = name;
            _config = config;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == _config.LogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                            Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (_config.EventId == 0 || _config.EventId == eventId.Id)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = _config.Color;
#pragma warning disable CA1062 // 验证公共方法的参数
                var str = $"{ _name } - { formatter(state, exception)}";
                Console.WriteLine($"{logLevel} - {eventId.Id} - {str}");
#pragma warning restore CA1062 // 验证公共方法的参数
                Console.ForegroundColor = color;

                if (loggser != null && logLevel > LogLevel.Information)
                {
                    loggser.Log((Sobey.Core.Log.LogLevels)logLevel, str);
                }
            }
        }
    }

#pragma warning disable CA1063 // 正确实现 IDisposable
    public class CustomConsoleLoggerProvider : ILoggerProvider
#pragma warning restore CA1063 // 正确实现 IDisposable
    {
        private readonly CustomConsoleLoggerConfiguration _config;
        private readonly ConcurrentDictionary<string, CustomLogger> _loggers = new ConcurrentDictionary<string, CustomLogger>();

        public CustomConsoleLoggerProvider(CustomConsoleLoggerConfiguration config)
        {
            _config = config;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new CustomLogger(name, _config));
        }

#pragma warning disable CA1063 // 正确实现 IDisposable
#pragma warning disable CA1816 // Dispose 方法应调用 SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose 方法应调用 SuppressFinalize
#pragma warning restore CA1063 // 正确实现 IDisposable
        {
            _loggers.Clear();
        }
    }
}
