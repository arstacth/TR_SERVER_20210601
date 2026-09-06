using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Guild_ResetGuildSkill_ACK : NetPacket
	{
		public Guild_ResetGuildSkill_ACK(long guildPoint, byte guildSkillPoint, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(75);
			ns.Write(1);
			ns.Write(guildPoint);
			ns.Write(guildSkillPoint);
			ns.Write(0);
			ns.Write(last);
		}
	}
}
