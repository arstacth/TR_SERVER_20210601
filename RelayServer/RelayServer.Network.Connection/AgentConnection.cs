using System;
using System.Net.Sockets;
using LocalCommons.Logging;
using LocalCommons.Network;
using RelayServer.Network.Packet.AgentServer;

namespace RelayServer.Network.Connections
{
	public sealed class AgentConnection : IConnection
	{
		public AgentConnection(Socket socket)
			: base(socket)
		{
			Log.Info("Connected to AgentServer, installing data...");
			base.DisconnectedEvent += LoginConnection_DisconnectedEvent;
			SendAsync(new Net_RegisterRelayServer());
		}

		private void LoginConnection_DisconnectedEvent(object sender, EventArgs e)
		{
			Log.Info("AgentServer IP: {0} disconnected", this);
			Dispose();
		}

		public override void HandleReceived(byte[] data)
		{
			PacketReader packetReader = new PacketReader(data, 0);
			switch (packetReader.ReadByte())
			{
			case 0:
				AgentServerHandle.Handle_RelayRegisterResult(this, packetReader);
				break;
			case 2:
				AgentServerHandle.Handle_RemoveClient(this, packetReader);
				break;
			}
		}
	}
}
