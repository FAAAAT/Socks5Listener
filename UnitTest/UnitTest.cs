using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using ConsoleApp.LogService;
using ConsoleApp.Socks;
using ConsoleApp;
using Socks5Listener.Helper;
using Xunit;
using Xunit.Abstractions;
using Config;
using Config.Net;

namespace UnitTest
{

	public class TestLogger : ISystemLogger
	{
		private ITestOutputHelper output;
		public TestLogger(ITestOutputHelper output)
		{
			this.output = output;
		}

		public void Error(string error)
		{
			output.WriteLine(error);
		}

		public void Warn(string warn)
		{
			output.WriteLine(warn);
		}

		public void Debug(string debug)
		{
			output.WriteLine(debug);

		}

		public void Info(string info)
		{
			output.WriteLine(info);
		}
	}

	public class FunctionTest
	{
		private ITestOutputHelper output;
		public string WinUtilityPath = "..\\..\\..\\..\\ssocks-win32-0.0.14.2\\";
		public string IPEndpoint = "localhost:10097";
		public string TestContent = "HelloWorld";

		public int ListenServicePort = 10091;
		public int RelayPort = 10092;
		public string RelayToIp = "localhost";
		public int MaxConnections = 10;
		public string ServiceListeningIP = "localhost:10090";


		public FunctionTest(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void BaseConnectFunctionTest()
		{

			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				output.WriteLine(Path.Combine(Environment.CurrentDirectory, WinUtilityPath));
				Assert.True(TestBaseConnectionFunction(Path.Combine(Environment.CurrentDirectory, WinUtilityPath), out var msg));
			}
			else
			{

			}


		}

		private bool TestTCPServer(string testContent, string ipendpointandport, out Socket s)
		{
			s = null;
			if (!IPEndPointHelper.TryParseEndPoint(ipendpointandport, out var ep, out var msg))
			{
				output.WriteLine(msg);
				return false;
			}

			IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
			if (properties.GetActiveTcpListeners().Any(x => x.Address.Equals(ep.Address) && x.Port == ep.Port))
			{
				output.WriteLine("port has been used " + ep.Port);
				return false;
			}


			s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			s.Bind(ep);
			s.Listen(10);
			s.AcceptAsync().ContinueWith(x =>
			{
				if (x.Exception == null)
				{

					var ns = x.Result;
					ns.Send(Encoding.UTF8.GetBytes(testContent));

					//					ns.Shutdown(SocketShutdown.Send);
					//					ns.Close();
				}


			});

			return true;
		}

		private bool TestStartRcSocks(string basePath, int listenPortForService, int listenPortForRelay, out Process p)
		{
			var result = false;

			ProcessStartInfo sinfo = new ProcessStartInfo(Path.Combine(basePath, "rcsocks.exe"), $" -p {listenPortForService} -l {listenPortForRelay}");
			sinfo.UseShellExecute = true;
			sinfo.CreateNoWindow = false;
			p = Process.Start(sinfo);
			result = true;
			return result;

		}

		private bool TestStartRsSocks(string basePath, string ipEndPoint, int maxConnections, out Process p)
		{
			var result = false;

			ProcessStartInfo sinfo = new ProcessStartInfo(Path.Combine(basePath, "rssocks.exe"), $" --socks {ipEndPoint} --ncon {maxConnections}");
			sinfo.UseShellExecute = true;
			sinfo.CreateNoWindow = false;
			p = Process.Start(sinfo);
			result = true;

			return result;
		}

		private bool TestTTTService(string ip, int listenPortForService, string serviceListeningip, string testServerEPAddress, out TransferServer ts)
		{
			ts = null;
			var result = false;
			if (!IPEndPointHelper.TryParseEndPoint(ip + ":" + listenPortForService, out var ep, out var msg))
			{
				output.WriteLine(msg);
				return result;
			}
			if (!IPEndPointHelper.TryParseEndPoint(serviceListeningip, out var listenEp, out var msg2))
			{
				output.WriteLine(msg2);
				return result;
			}
			if (!IPEndPointHelper.TryParseEndPoint(testServerEPAddress, out var testServerEP, out var msg3))
			{
				output.WriteLine(msg3);
				return result;
			}

			ts = new TransferServer(listenEp, ep, testServerEP, new BaseLogger(new TestLogger(output)));
			result = true;
			return result;
		}

		private bool TestClient(string content, string ipEndPointAddress, out Socket s)
		{
			s = null;
			if (!IPEndPointHelper.TryParseEndPoint(ipEndPointAddress, out var ep, out var msg))
			{
				output.WriteLine(msg);
				return false;
			}
			var result = false;
			s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			s.Connect(ep);
			byte[] buffer = new byte[4096];
			var length = s.Receive(buffer);
			if (length == 0)
			{
				output.WriteLine("remote closed unexcepted");
				return false;
			}
			if (Encoding.UTF8.GetString(buffer.Take(length).ToArray()) == content)
			{
				result = true;
			}
			else
			{
				output.WriteLine("received result not same");
			}
			return result;
		}

		private bool TestBaseConnectionFunction(string path, out string msg)
		{

			bool result = false;
			Socket testServer = null;
			Process rcsocks = null;
			Process rssocks = null;
			TransferServer ts = null;
			Socket clientSocket = null;
			try
			{

				msg = "";
				if (!TestTCPServer(TestContent, IPEndpoint, out testServer))
				{
					return result;
				}




				if (!TestStartRcSocks(path, ListenServicePort, RelayPort, out rcsocks))
				{
					return result;
				}

				if (!TestStartRsSocks(path, $"{RelayToIp}:{RelayPort}", MaxConnections, out rssocks))
				{
					return result;
				}

				if (!TestTTTService("localhost", ListenServicePort, ServiceListeningIP, IPEndpoint, out ts))
				{
					return result;
				}

				if (!TestClient(TestContent, ServiceListeningIP, out clientSocket))
				{
					return result;
				}

				result = true;
				return result;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
			finally
			{
				if (testServer != null)
				{
					testServer.Close();
					testServer.Dispose();
				}
				if (rcsocks != null)
				{
					rcsocks.Kill();
					rcsocks.Close();
				}
				if (rssocks != null)
				{
					rssocks.Kill();
					rssocks.Close();
				}
				if (ts != null)
				{
					ts.Close();
				}
				if (clientSocket != null)
				{
					clientSocket.Disconnect(false);
					clientSocket.Close();
					clientSocket.Dispose();

				}
			}


		}


	}

	public class ConfigurationTest
	{

		public string file = "./SockMaps.ini";
		[Fact]
		public void TestConfiruation()
		{
			var builder = Config.Net.ConfigurationExtensions.UseIniFile<ISockMapConfig>(new Config.Net.ConfigurationBuilder<ISockMapConfig>(), this.file);
			var config = builder.Build();

			Assert.True(config.Maps.Any());


		}

	}
}
