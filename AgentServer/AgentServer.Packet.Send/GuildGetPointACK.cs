using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GuildGetPointACK : NetPacket
	{
		public GuildGetPointACK(long point, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(63);
			ns.Write(point);
			ns.Write(last);
		}
	}
}
