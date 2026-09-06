using System;
using System.Net;
using Akka.Actor;
using Akka.IO;
using LocalCommons.Network;
using Serilog;

namespace RelayServer.Structuring
{
	public class AccountInfo
	{
		public int Session { get; set; }

		public ushort UDPPort { get; set; }

		public string IP { get; set; }

		public long LastPingTime { get; set; }

		public IPEndPoint remoteIpEndPoint { get; set; }

		public void SendAsync(NetPacket packet, EndPoint EndPoint)
		{
			try
			{
				RelayServer.server.Tell(Udp.Send.Create(ByteString.FromBytes(packet.Compile()), EndPoint));
			}
			catch (Exception ex)
			{
				Log.Error("SendAsync Error:{0}", ex.ToString());
			}
		}
	}
}
