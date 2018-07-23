using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConsoleApp.LogService
{
    public class BaseLogger
    {
	    private ISystemLogger logger;

	    public BaseLogger(ISystemLogger logger)
	    {
		    this.logger = logger;
	    }

	    public virtual void Error(string error)
	    {
			
			logger.Error($"[{Thread.CurrentThread.ManagedThreadId}]{error}");
	    }


	    public virtual void Warn(string warn)
	    {
		    logger.Warn($"[{Thread.CurrentThread.ManagedThreadId}]{warn}");

		}

		public virtual void Debug(string debug)
	    {
		    logger.Debug($"[{Thread.CurrentThread.ManagedThreadId}]{debug}");

		}

		public virtual void Info(string info)
	    {
		    logger.Info($"[{Thread.CurrentThread.ManagedThreadId}]{info}");

		}
	}


	public interface ISystemLogger
	{
		void Error(string error);
	

		void Warn(string warn);
		

		void Debug(string debug);
		

		void Info(string info);



	}
}
