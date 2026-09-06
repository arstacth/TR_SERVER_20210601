using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class DelGuildFailACK : NetPacket
	{
		public DelGuildFailACK(byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(7);
			ns.Write((short)12);
			ns.Write(last);
		}
	}
}
