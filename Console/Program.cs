﻿using CommandLine;
using ConsoleApp.LogService;
using ConsoleApp.Socks;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

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
                            var builder = Config.Net.ConfigurationExtensions.UseIniFile<ISockMapConfig>(new Config.Net.ConfigurationBuilder<ISockMapConfig>(), cf.FullName);
                            var config = builder.Build();
                            foreach (ISockMap map in config.Maps)
                            {
                                var listenAddr = map.Local.Split(':');
                                var proxyAddr = x.Proxy.Split(':');
                                var destAddr = map.Remote.Split(':');

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

        public static TransferServer GetTransferServer(string[] listenAddr, string[] proxyAddr, string [] destAddr, BaseLogger logger)
        {
            var listen = new DnsEndPoint(listenAddr[0], Convert.ToInt32(listenAddr[1]));
            var proxy = new DnsEndPoint(proxyAddr[0], Convert.ToInt32(proxyAddr[1]));
            var dest = new DnsEndPoint(destAddr[0], Convert.ToInt32(destAddr[1]));

            TransferServer server = new TransferServer(new IPEndPoint(Dns.GetHostEntry(listen.Host).AddressList[0], listen.Port), proxy, dest, logger);
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
        [Config.Net.Option(Alias = "Maps.Maps")]
        IEnumerable<ISockMap> Maps { get; set; }
    }
    public interface ISockMap
    {
        string Local { get; set; }
        string Remote { get; set; }
    }
}
