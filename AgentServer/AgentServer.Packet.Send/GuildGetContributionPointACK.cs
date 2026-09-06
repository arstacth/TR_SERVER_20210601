using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GuildGetContributionPointACK : NetPacket
	{
		public GuildGetContributionPointACK(int point, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(60);
			ns.Write(point);
			ns.Write(last);
		}
	}
}
