using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class DisconnectPacket : NetPacket
	{
		public DisconnectPacket(int msgid, byte last)
		{
			ns.WriteOP(Opcodes.eServer_DISCONNECT_FROM_SERVER_ACK);
			ns.Write(msgid);
			ns.Write(last);
		}
	}
}
