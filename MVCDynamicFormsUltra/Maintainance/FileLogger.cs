using System.Collections.Concurrent;

namespace MVCDynamicFormsUltra.Maintainance
{
   

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }


        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _filePath));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly string _loggerName;

        public FileLogger(string loggerName, string filePath)
        {
            _loggerName = loggerName;
            _filePath = filePath;
        }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string fullFilePath = _filePath.Replace("{date}", DateTimeOffset.UtcNow.ToString("yyyyMMdd"));
            string logRecord = string.Format("{0} {1} [{2}] {3} {4}", "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]", _loggerName, logLevel.ToString(), formatter(state, exception), exception != null ? exception.StackTrace : "");

            using (var streamWriter = new StreamWriter(fullFilePath, true))
            {
                streamWriter.WriteLine(logRecord);
            }
        }
    }
}
