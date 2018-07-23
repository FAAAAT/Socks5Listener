using System;
using System.IO;
using System.Net;
using CommandLine;
using ConsoleApp.LogService;
using ConsoleApp.Socks;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;


namespace ConsoleApp
{
	class Program
	{
		static void Main(string[] args)
		{




			CommandLine.Parser.Default.ParseArguments<Options>(args)
				.WithParsed(x =>
				{

					BaseLogger logger = new BaseLogger(new Log4NetLogger());

					var listenAddr = x.Listen.Split(':');
					var proxyAddr = x.Proxy.Split(':');
					var destAddr = x.Dest.Split(':');

					var listen = new DnsEndPoint(listenAddr[0], Convert.ToInt32(listenAddr[1]));
					var proxy = new DnsEndPoint(proxyAddr[0],Convert.ToInt32(proxyAddr[1]));
					var dest = new DnsEndPoint(destAddr[0],Convert.ToInt32(destAddr[1]));

					

					TransferServer server = new TransferServer(new IPEndPoint(Dns.GetHostEntry(listen.Host).AddressList[0], listen.Port), proxy,dest,logger);
					
					while (Console.ReadLine() != "exit")
					{
						Console.Write('>');
					}
					server.Close();
				})
				.WithNotParsed(x =>
				{
					foreach (Error error in x)
					{
						Console.WriteLine(error);
					}
				});
		}
	}


	public class Options
	{
		[Option('l', "listen", HelpText = "address:port to listen", Required = true)]
		public string Listen { get; set; }

		[Option('d', "dest", HelpText = "address:port to where you want to proxy the connection", Required = true)]
		public string Dest { get; set; }

		[Option('p', "proxy", HelpText = "address:port to socks5 proxy")]
		public string Proxy { get; set; }

		[Option('u', "user", HelpText = "user name if is sercured", Required = false)]
		public string UserName { get; set; }

		[Option('P', "password", HelpText = "password if is sercured", Required = false)]
		public string Password { get; set; }
		
	}
}
