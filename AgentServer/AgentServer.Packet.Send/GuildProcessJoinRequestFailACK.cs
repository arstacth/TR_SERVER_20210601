using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GuildProcessJoinRequestFailACK : NetPacket
	{
		public GuildProcessJoinRequestFailACK(short err, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(3);
			ns.Write(err);
			ns.Write(last);
		}
	}
}
