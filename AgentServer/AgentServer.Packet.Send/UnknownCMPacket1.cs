using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class UnknownCMPacket1 : NetPacket
	{
		public UnknownCMPacket1(byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMMUNITY_SERVER_PROTOCOL);
			ns.WriteOP(39);
			ns.Write((short)0);
			ns.Write(last);
		}
	}
}
