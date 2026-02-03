using System;
using System.Net;
using Server.Game;
using ServerCore;

namespace Server
{
    class Program
	{
		static Listener _listener = new Listener();

		static void Main(string[] args)
		{
			RoomManager.Instance.Add();

            string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

			_listener.init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine("Listening...");

			while (true)
			{
				;
			}
		}
	}
}


