using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GuildCancelProposeACK : NetPacket
	{
		public GuildCancelProposeACK(string guildname, int guildnum, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(15);
			ns.WriteBIG5Fixed_intSize(guildname);
			ns.Write(guildnum);
			ns.Write(last);
		}
	}
}
