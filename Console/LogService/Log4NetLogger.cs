using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using log4net;
using log4net.Config;
using log4net.Core;

namespace ConsoleApp.LogService
{
	public class Log4NetLogger : ISystemLogger
	{
		private ILog log;
		public Log4NetLogger()
		{
			var repo = LoggerManager.CreateRepository("LoggerRepo");

			XmlConfigurator.ConfigureAndWatch(repo, new FileInfo(Path.Combine(Environment.CurrentDirectory,".\\log4net.config")));
			ILog log = LogManager.GetLogger("LoggerRepo", "Logger");
			this.log = log;
		}

		public void Error(string error)
		{
			log.Error(error);
#if DEBUG
			Console.WriteLine(error);
#endif
		}

		public void Warn(string warn)
		{
			log.Warn(warn);
#if DEBUG
			Console.WriteLine(warn);
#endif
		}

		public void Debug(string debug)
		{
			log.Debug(debug);
#if DEBUG
			Console.WriteLine(debug);
#endif
		}

		public void Info(string info)
		{
			log.Info(info);
#if DEBUG
			Console.WriteLine(info);
#endif

		}
	}
}
