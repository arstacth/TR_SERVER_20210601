using System;
using System.Net;
using LocalCommons.Logging;
using LocalCommons.Network;
using LocalCommons.Utilities;
using RelayServer.Network.Connections;
using RelayServer.Network.Packet.Send;
using RelayServer.Structuring;

namespace RelayServer.Network.Packet
{
	public class UDPHandle
	{
		public static void Handle_FirstConnect(PacketReader reader, EndPoint endPoint)
		{
			int num = reader.ReadLEInt32();
			IPEndPoint iPEndPoint = endPoint as IPEndPoint;
			ushort uDPPort = (ushort)iPEndPoint.Port;
			string iP = iPEndPoint.Address.MapToIPv4().ToString();
			AccountInfo accountInfo = new AccountInfo
			{
				Session = num,
				UDPPort = uDPPort,
				IP = iP,
				LastPingTime = Utility.CurrentTimeMilliseconds(),
				remoteIpEndPoint = iPEndPoint
			};
			if (!ClientConnection.CurrentAccounts.ContainsKey(num))
			{
				ClientConnection.CurrentAccounts.TryAdd(num, accountInfo);
				accountInfo.SendAsync(new Connect_04FF5604(iPEndPoint), endPoint);
			}
			else
			{
				ClientConnection.CurrentAccounts.TryRemove(num, out var _);
				ClientConnection.CurrentAccounts.TryAdd(num, accountInfo);
				accountInfo.SendAsync(new Connect_04FF5604(iPEndPoint), endPoint);
			}
		}

		public static void Handle_Ping(PacketReader reader, EndPoint endPoint)
		{
			int key = reader.ReadLEInt32();
			if (ClientConnection.CurrentAccounts.TryGetValue(key, out var value))
			{
				value.LastPingTime = Utility.CurrentTimeMilliseconds();
				value.SendAsync(new Connect_04FF5A04(), endPoint);
			}
		}

		public static void Handle_ConnectUser_04FF5804(PacketReader reader, EndPoint endPoint)
		{
			try
			{
				int key = reader.ReadLEInt32();
				short length = reader.ReadLEInt16();
				byte[] array = reader.ReadByteArray(length);
				if (ClientConnection.CurrentAccounts.TryGetValue(key, out var value))
				{
					value.SendAsync(new Connect_04FF5804(array), value.remoteIpEndPoint);
				}
			}
			catch (Exception ex)
			{
				Log.Error("ConnectUser Error:{0}, buffer:{1}", ex.ToString(), Utility.ByteArrayToString(reader.Buffer));
			}
		}
	}
}
