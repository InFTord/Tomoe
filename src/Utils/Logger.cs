using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tomoe.Utils
{
	internal class Logger : ILogger
	{

		private static readonly Random _random = new();

		/// <summary>What to prefix the log content with.</summary>
		private readonly string _branchname;

		/// <summary>A per branch log level. Defaults to the config's log level.</summary>
		private readonly LogLevel _branchLogLevel;

		/// <summary>The log file.</summary>
		public static FileStream LogFile { get; private set; }

		private readonly string _threadId;

		private readonly bool _isCommand;

		/// <summary>Unknown what this does. TODO: Implement this correctly.</summary>
		public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();

		/// <summary>
		/// Tests if a log level is enabled. Compares the <paramref name="logLevel">logLevel</paramref> with <see cref="Config.Logging.Tomoe">Config.Logging.Tomoe</see>.
		/// </summary>
		/// <param name="logLevel">What level to test if is activated.</param>
		/// <returns>true if <paramref name="logLevel">logLevel</paramref> is enabled, otherwise false.</returns>
		public bool IsEnabled(LogLevel logLevel) => Config.Logging.Tomoe <= logLevel;

		/// <summary>
		/// Logs stuff to console.
		/// </summary>
		/// <param name="logLevel">What level you're logging the content at.</param>
		/// <param name="eventId"></param>
		/// <param name="state"></param>
		/// <param name="exception"></param>
		/// <param name="formatter"></param>
		/// <typeparam name="TState"></typeparam>
		/// <remarks>While this isn't depreciated, it is advised that you use <see cref="Log(LogLevel, string)">Log(LogLevel, string)</see> instead, as this function isn't reliably formatting the formatter correctly.</remarks>
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			switch (logLevel)
			{
				case LogLevel.Trace:
					Trace(formatter(state, exception) ?? exception.Message);
					break;
				case LogLevel.Debug:
					Debug(formatter(state, exception) ?? exception.Message);
					break;
				case LogLevel.Information:
					Info(formatter(state, exception) ?? exception.Message);
					break;
				case LogLevel.Warning:
					Warn(formatter(state, exception) ?? exception.Message);
					break;
				case LogLevel.Error:
					Error(formatter(state, exception) ?? exception.Message);
					break;
				case LogLevel.Critical:
					Critical(formatter(state, exception) ?? exception.Message, false);
					break;
				default:
					break;
			}
		}

		public void Log(LogLevel logLevel, string content)
		{
			switch (logLevel)
			{
				case LogLevel.Trace:
					Trace(content);
					break;
				case LogLevel.Debug:
					Debug(content);
					break;
				case LogLevel.Information:
					Info(content);
					break;
				case LogLevel.Warning:
					Warn(content);
					break;
				case LogLevel.Error:
					Error(content);
					break;
				case LogLevel.Critical:
					Critical(content, false);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Creates a new logger.
		/// </summary>
		/// <example>
		/// <code>
		/// Logger logger = new Logger("Main");
		/// logger.Info("Created Main logger!"); //Output something similar to
		///                                      //[Fri, Oct 13 2020 17:32:54] [Info] Main: Created Main logger!
		/// </code>
		/// </example>
		/// <param name="branchName">The area of Tomoe that the logger is Logging.</param>
		public Logger(string branchName)
		{
			_branchname = branchName;
			_branchLogLevel = Config.Logging.Tomoe;
			if (Config.Logging.SaveToFile && LogFile == null) LogFile = new FileStream(Path.Join(FileSystem.ProjectRoot, $"log/{GetTime()}.log"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			if (branchName.Contains("Commands")) _isCommand = true;
			_threadId = _random.Next(1000, 9999).ToString();
		}

		public Logger(string branchName, LogLevel branchLogLevel)
		{
			_branchname = branchName;
			_branchLogLevel = branchLogLevel;
			if (Config.Logging.SaveToFile && LogFile == null) LogFile = new FileStream(Path.Join(FileSystem.ProjectRoot, $"log/{GetTime()}.log"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			if (branchName.Contains("Commands")) _isCommand = true;
			_threadId = _random.Next(1000, 9999).ToString();
		}

		/// <summary>
		/// Logs all values to console/file. If the <see cref="Config.Logging.Tomoe"> isn't on Trace, nothing will log.
		/// </summary>
		/// <example>
		/// <code>
		/// Logger logger = new Logger("Main");
		/// logger.Trace("Created Main logger!"); //Output something similar to
		///                                      //[Fri, Oct 13 2020 17:32:54] [Trace] Main: Created Main logger!
		/// </code>
		/// </example>
		/// <remarks>[Trace] has a blue font color.</remarks>
		/// <param name="value">What to be logged.</param>
		public void Trace(string value)
		{
			if (Config.Logging.Tomoe > LogLevel.Trace || _branchLogLevel > LogLevel.Trace) return;
			string currentTime = GetTime();
			Console.ResetColor();
			Console.Write($"[{currentTime}] ");
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.Write($"[Trace]");
			if (_isCommand && Config.Logging.ShowId)
			{
				Console.ResetColor();
				Console.Write(' ');
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.Write($"[{_threadId}]");
			}
			Console.ResetColor();
			Console.Write(' ');
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(_branchname);
			Console.ResetColor();
			Console.WriteLine($": {value.Trim()}");
			if (Config.Logging.SaveToFile && LogFile != null)
			{
				LogFile.Write(Encoding.UTF8.GetBytes($"[{currentTime}] [Trace] {_branchname}: {value}{Environment.NewLine}"));
				LogFile.Flush();
			}
		}

		/// <summary>
		/// Logs all values to console/file. If the <see cref="Config.Logging.Tomoe"> isn't on Debug or Trace, nothing will log.
		/// </summary>
		/// <example>
		/// <code>
		/// Logger logger = new Logger("Main");
		/// logger.Debug("Created Main logger!"); //Output something similar to
		///                                       //[Fri, Oct 13 2020 17:32:54] [Debug] Main: Created Main logger!
		/// </code>
		/// </example>
		/// <remarks>[Debug] has a dark grey font color.</remarks>
		/// <param name="value">What to be logged.</param>
		public void Debug(string value)
		{
			if (Config.Logging.Tomoe > LogLevel.Debug || _branchLogLevel > LogLevel.Debug) return;
			string currentTime = GetTime();
			Console.ResetColor();
			Console.Write($"[{currentTime}] ");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write($"[Debug]");
			if (_isCommand && Config.Logging.ShowId)
			{
				Console.ResetColor();
				Console.Write(' ');
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.Write($"[{_threadId}]");
			}
			Console.ResetColor();
			Console.Write(' ');
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(_branchname);
			Console.ResetColor();
			Console.WriteLine($": {value.Trim()}");
			if (Config.Logging.SaveToFile && LogFile != null)
			{
				LogFile.Write(Encoding.UTF8.GetBytes($"[{currentTime}] [Debug] {_branchname}: {value}{Environment.NewLine}"));
				LogFile.Flush();
			}
		}

		/// <summary>
		/// Logs all values to console/file. If the <see cref="Config.Logging.Tomoe"> isn't on Info or below, nothing will log.
		/// </summary>
		/// <example>
		/// <code>
		/// Logger logger = new Logger("Main");
		/// logger.Info("Created Main logger!"); //Output something similar to
		///                                      //[Fri, Oct 13 2020 17:32:54] [Info] Main: Created Main logger!
		/// </code>
		/// </example>
		/// <remarks>[Info] has a green font color.</remarks>
		/// <param name="value">What to be logged.</param>
		/// <param name="exit">Determines if the program exits. Defaults to false.</param>
		public void Info(string value, bool exit = false)
		{
			if (Config.Logging.Tomoe > LogLevel.Information || _branchLogLevel > LogLevel.Information) return;
			string currentTime = GetTime();
			Console.ResetColor();
			Console.Write($"[{currentTime}] ");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write($"[Info]");
			if (_isCommand && Config.Logging.ShowId)
			{
				Console.ResetColor();
				Console.Write("  ");
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.Write($"[{_threadId}]");
				Console.ResetColor();
				Console.Write(' ');
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(_branchname);
			}
			else
			{
				Console.ResetColor();
				Console.Write("  ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(_branchname);
			}
			Console.ResetColor();
			Console.WriteLine($": {value.Trim()}");
			if (Config.Logging.SaveToFile && LogFile != null)
			{
				LogFile.Write(Encoding.UTF8.GetBytes($"[{currentTime}] [Info]  {_branchname}: {value}{Environment.NewLine}"));
				LogFile.Flush();
			}
			if (exit)
			{
				if (LogFile != null) LogFile.Dispose();
				Console.WriteLine("Exiting...");
				Environment.Exit(1);
			}
		}

		/// <summary>
		/// Logs all values to console/file. If the <see cref="Config.Logging.Tomoe"> isn't on Warn or below, nothing will log.
		/// </summary>
		/// <example>
		/// <code>
		/// Logger logger = new Logger("Main");
		/// logger.Warn("Created Main logger!"); //Output something similar to
		///                                      //[Fri, Oct 13 2020 17:32:54] [Warn] Main: Created Main logger!
		/// </code>
		/// </example>
		/// <remarks>[Warn] has a yellow font color.</remarks>
		/// <param name="value">What to be logged.</param>
		/// <param name="exit">Determines if the program exits. Defaults to false.</param>
		public void Warn(string value, bool exit = false)
		{
			if (Config.Logging.Tomoe > LogLevel.Warning || _branchLogLevel > LogLevel.Warning) return;
			string currentTime = GetTime();
			Console.ResetColor();
			Console.Write($"[{currentTime}] ");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write($"[Warn]");
			if (_isCommand && Config.Logging.ShowId)
			{
				Console.ResetColor();
				Console.Write("  ");
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.Write($"[{_threadId}]");
				Console.ResetColor();
				Console.Write(' ');
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(_branchname);
			}
			else
			{
				Console.ResetColor();
				Console.Write("  ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(_branchname);
			}
			Console.ResetColor();
			Console.WriteLine($": {value.Trim()}");
			if (Config.Logging.SaveToFile && LogFile != null)
			{
				LogFile.Write(Encoding.UTF8.GetBytes($"[{currentTime}] [Warn]  {_branchname}: {value}{Environment.NewLine}"));
				LogFile.Flush();
			}
			if (exit)
			{
				if (LogFile != null) LogFile.Dispose();
				Console.WriteLine("Exiting...");
				Environment.Exit(1);
			}
		}

		/// <summary>
		/// Logs all values to console/file. If the <see cref="Config.Logging.Tomoe"> isn't on Error or below, nothing will log.
		/// </summary>
		/// <example>
		/// <code>
		/// Logger logger = new Logger("Main");
		/// logger.Errpr("Created Main logger!"); //Output something similar to
		///                                       //[Fri, Oct 13 2020 17:32:54] [Error] Main: Created Main logger!
		/// </code>
		/// </example>
		/// <remarks>[Error] has a red font color.</remarks>
		/// <param name="value">What to be logged.</param>
		/// <param name="exit">Determines if the program exits. Defaults to false.</param>
		public void Error(string value, bool exit = false)
		{
			if (Config.Logging.Tomoe > LogLevel.Error || _branchLogLevel > LogLevel.Error) return;
			string currentTime = GetTime();
			Console.ResetColor();
			Console.Write($"[{currentTime}] ");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write($"[Error]");
			if (_isCommand && Config.Logging.ShowId)
			{
				Console.ResetColor();
				Console.Write(' ');
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.Write($"[{_threadId}]");
			}
			Console.ResetColor();
			Console.Write(' ');
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(_branchname);
			Console.ResetColor();
			Console.WriteLine($": {value.Trim()}");
			if (Config.Logging.SaveToFile && LogFile != null)
			{
				LogFile.Write(Encoding.UTF8.GetBytes($"[{currentTime}] [Error] {_branchname}: {value}{Environment.NewLine}"));
				LogFile.Flush();
			}
			if (exit)
			{
				if (LogFile != null) LogFile.Dispose();
				Console.WriteLine("Exiting...");
				Environment.Exit(1);
			}
		}

		/// <summary>
		/// Logs all values to console/file. *Will always log.*
		/// </summary>
		/// <example>
		/// <code>
		/// Logger logger = new Logger("Main");
		/// logger.Critical("Created Main logger!"); //Output something similar to
		///                                          //[Fri, Oct 13 2020 17:32:54] [Critical] Main: Created Main logger!
		/// </code>
		/// </example>
		/// <remarks>[Critical] has a red font color and white background.</remarks>
		/// <param name="value">What to be logged.</param>
		/// <param name="exit">Determines if the program exits. Defaults to true.</param>
		public void Critical(string value, bool exit = true)
		{
			string currentTime = GetTime();
			Console.ResetColor();
			Console.Write($"[{currentTime}] ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.Red;
			Console.Write($"[Crit]");
			Console.ResetColor();
			if (_isCommand && Config.Logging.ShowId)
			{
				Console.ResetColor();
				Console.Write("  ");
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				Console.Write($"[{_threadId}]");
				Console.ResetColor();
				Console.Write(' ');
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(_branchname);
			}
			else
			{
				Console.ResetColor();
				Console.Write("  ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(_branchname);
			}
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(_branchname);
			Console.ResetColor();
			Console.WriteLine($": {value.Trim()}");
			if (Config.Logging.SaveToFile && LogFile != null)
			{
				LogFile.Write(Encoding.UTF8.GetBytes($"[{currentTime}] [Crit]  {_branchname}: {value}{Environment.NewLine}"));
				LogFile.Flush();
			}
			if (exit)
			{
				if (LogFile != null) LogFile.Dispose();
				Console.WriteLine("Exiting...");
				Environment.Exit(1);
			}
		}

		/// <summary>Gets the time in <code>yyyy-MM-dd HH:mm:ss</code> following rfc3339 format, slightly tweaked (see removed 'T'). See https://tools.ietf.org/html/rfc3339#section-5.6.</summary>
		/// <returns>string</returns>
		public static string GetTime() => DateTime.Now.ToLocalTime().ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffff");
	}

	public class LoggerProvider : ILoggerFactory
	{
		private readonly ConcurrentDictionary<string, Logger> _loggers = new();
		public ILogger CreateLogger(string categoryName)
		{
			if (categoryName.ToLower().StartsWith("dsharpplus")) return _loggers.GetOrAdd(categoryName, name => new Logger(name, Config.Logging.Discord));
			else return _loggers.GetOrAdd(categoryName, name => new Logger(name));
		}

		public void Dispose() => GC.SuppressFinalize(this);
		public void AddProvider(ILoggerProvider provider) { }
	}
}
