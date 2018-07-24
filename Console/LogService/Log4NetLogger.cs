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
		}

		public void Warn(string warn)
		{
			log.Warn(warn);
		}

		public void Debug(string debug)
		{
			log.Debug(debug);
		}

		public void Info(string info)
		{
			log.Info(info);

		}
	}
}
