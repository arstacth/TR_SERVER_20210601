using System;
using System.Collections.Concurrent;
using System.Net;
using LocalCommons.Network;
using LocalCommons.Utilities;
using RelayServer.Network.Packet;
using RelayServer.Structuring;
using Serilog;

namespace RelayServer.Network.Connections
{
	public static class ClientConnection
	{
		public static ConcurrentDictionary<int, AccountInfo> CurrentAccounts { get; } = new ConcurrentDictionary<int, AccountInfo>();


		public static void HandleReceived(byte[] data, EndPoint endPoint)
		{
			try
			{
				PacketReader packetReader = new PacketReader(data, 0);
				packetReader.ReadLEInt32();
				packetReader.ReadLEUInt32();
				packetReader.Offset++;
				switch (packetReader.ReadLEInt16())
				{
				case 23:
					UDPHandle.Handle_FirstConnect(packetReader, endPoint);
					break;
				case 27:
					UDPHandle.Handle_Ping(packetReader, endPoint);
					break;
				case 25:
					UDPHandle.Handle_ConnectUser_04FF5804(packetReader, endPoint);
					break;
				default:
				{
					IPEndPoint iPEndPoint = endPoint as IPEndPoint;
					Log.Information("Unknown Packet:{0}, ip:{1}", Utility.ByteArrayToString(packetReader.Buffer), iPEndPoint.Address.MapToIPv4().ToString());
					break;
				}
				}
			}
			catch (Exception ex)
			{
				Log.Error("HandleReceived Error:{0}", ex.ToString());
			}
		}
	}
}
