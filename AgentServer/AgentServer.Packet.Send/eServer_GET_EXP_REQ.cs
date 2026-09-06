using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class eServer_GET_EXP_REQ : NetPacket
	{
		public eServer_GET_EXP_REQ(short type, long value, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GET_EXP_ACK);
			ns.Write(0);
			ns.Write(type);
			ns.Write(value);
			ns.Write(last);
		}
	}
}
