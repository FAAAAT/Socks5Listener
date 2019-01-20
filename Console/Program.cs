using CommandLine;
using ConsoleApp.LogService;
using ConsoleApp.Socks;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Config.Net;

using Option = CommandLine.OptionAttribute;
using System.Linq;
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
                    List<Task<TransferServer>> tasks = new List<Task<TransferServer>>();

                    var servers = new List<TransferServer>();

                    if (string.IsNullOrWhiteSpace(x.ConfigFile))
                    {

                        var listenAddr = x.Listen.Split(':');
                        var proxyAddr = x.Proxy.Split(':');
                        var destAddr = x.Dest.Split(':');

                        var server = GetTransferServer(listenAddr, proxyAddr, destAddr, logger);

                        servers.Add(server);

                    }
                    else
                    {
                        FileInfo cf = new FileInfo(x.ConfigFile);
                        if (!cf.Exists)
                        {
                            Console.WriteLine($"can not find config file in {cf.FullName}");
                        }
                        else
                        {
    						var builder = new ConfigurationBuilder<ISockMapConfig>();
						builder.UseJsonFile(cf.FullName);
                                var config = builder.Build();
                            foreach (ISockMap map in config.Maps)
                            {

                                Console.WriteLine($"Loaded config settings:{map.Local}=>{map.Remote}");
                                if (map == null || map.Local == null || map.Remote == null)
                                    break;
                                var listenAddr = map.Local.Split(':');
                                if (listenAddr.Length < 2)
                                    Console.WriteLine("Error listen term less than 2");
                                var proxyAddr = x.Proxy.Split(':');
                                if (proxyAddr.Length < 2)
                                    Console.WriteLine("Error proxy term less than 2");
                                var destAddr = map.Remote.Split(':');
                                if (destAddr.Length < 2)
                                    Console.WriteLine("Error dest term less than 2");

                                var server = GetTransferServer(listenAddr, proxyAddr, destAddr, logger);

                                servers.Add(server);
                            }
                        }

                    }


                    while (Console.ReadLine() != "exit")
                    {
                        Console.Write('>');
                    }

                    foreach (var transferServer in servers)
                    {
                        if (transferServer != null)
                        {
                            transferServer.Close();

                        }
                    }


                })
                .WithNotParsed(x =>
                {
                    foreach (Error error in x)
                    {
                        Console.WriteLine(error);
                    }
                });
        }

        public static (DnsEndPoint listen,DnsEndPoint proxy,DnsEndPoint dest) GetTransferServerParameter(string[] listenAddr, string[] proxyAddr, string[] destAddr)
		{
			var listen = new DnsEndPoint(listenAddr[0], Convert.ToInt32(listenAddr[1]));
            var proxy = new DnsEndPoint(proxyAddr[0], Convert.ToInt32(proxyAddr[1]));
            var dest = new DnsEndPoint(destAddr[0], Convert.ToInt32(destAddr[1]));
			return (listen,proxy,dest);
		}

        public static TransferServer GetTransferServer(string[] listenAddr, string[] proxyAddr, string [] destAddr, BaseLogger logger)
        {
            var tuple = GetTransferServerParameter(listenAddr, proxyAddr, destAddr);
            IPAddress ip = null;
            var hostEntry = Dns.GetHostEntry(tuple.listen.Host);
            if(hostEntry.AddressList.Count() == 0) 
            {
                ip = IPAddress.Parse(tuple.listen.Host);
            }
            else 
            {
                ip = hostEntry.AddressList[0];
            }
            
            TransferServer server = new TransferServer(new IPEndPoint(ip, tuple.listen.Port), tuple.proxy, tuple.dest, logger);
            return server;
        }
        
        /// <summary>
        /// deprecated.
        /// </summary>
        /// <returns>The async started transfer server.</returns>
        /// <param name="listenAddr">Listen address.</param>
        /// <param name="proxyAddr">Proxy address.</param>
        /// <param name="destAddr">Destination address.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="serverTask">Server task.</param>
        public static TransferServer GetAsyncStartedTransferServer(string[] listenAddr, string[] proxyAddr, string[] destAddr, BaseLogger logger,out Task<TransferServer> serverTask)
        {
    			var tuple = GetTransferServerParameter(listenAddr, proxyAddr, destAddr);

    			var server = new TransferServer(new IPEndPoint(Dns.GetHostEntry(tuple.listen.Host).AddressList[0], tuple.listen.Port), tuple.proxy, tuple.dest, logger);

    			serverTask = new Task<TransferServer>((state) => 
    			{
    				var temp = state as TransferServer;
    				return temp;
    			}, server);
                
    			return server;
        }

    }


    public class Options
    {
        [Option('l', "listen", HelpText = "address:port to listen", Required = false)]
        public string Listen { get; set; }

        [Option('d', "dest", HelpText = "address:port to where you want to proxy the connection . typically, you can just use localhost or your machine name", Required = false)]
        public string Dest { get; set; }

        [Option('p', "proxy", HelpText = "address:port to socks5 proxy")]
        public string Proxy { get; set; }

        [Option('u', "user", HelpText = "user name if is sercured", Required = false)]
        public string UserName { get; set; }

        [Option('P', "password", HelpText = "password if is sercured", Required = false)]
        public string Password { get; set; }

        [Option('c', "config", HelpText = "Config file path to get the listen port and dest port.", Required = false)]
        public string ConfigFile { get; set; }
    }


    public class ParameterValidator
    {
        public bool ValidateAddr(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;
            return true;
        }
    }

    public interface ISockMapConfig
    {
        IEnumerable<ISockMap> Maps { get; }
    }
    public interface ISockMap
    {
        string Local { get; }
        string Remote { get; }
    }
}
