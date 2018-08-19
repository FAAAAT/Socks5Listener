using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using log4net.Config;

namespace Socks5Listener.Helper
{
    public static class IPEndPointHelper
    {
	    public static bool TryParseEndPoint(string ipNPort,out IPEndPoint endpoint,out string errorMessage)
	    {
		    errorMessage = "";
		    endpoint = null;
		    var result = false;
		    var temp = ipNPort.Split(':');

		    if (!int.TryParse(temp[1], out var p))
		    {
			    errorMessage = "port not valid";
			    return false;
		    }
		    DnsEndPoint dep = new DnsEndPoint(temp[0],p);

		    var ip = Dns.GetHostEntry(temp[0]).AddressList.FirstOrDefault(x=>x.AddressFamily == AddressFamily.InterNetwork);

			if (ipNPort.Length <= 1 || !int.TryParse(temp[1], out var port))
		    {
			    errorMessage = "port not valid";
			    return false;
		    }

		    IPEndPoint ep = new IPEndPoint(ip, port);

		    endpoint = ep;
		    result = true;
		    return result;
	    }
    }
}
