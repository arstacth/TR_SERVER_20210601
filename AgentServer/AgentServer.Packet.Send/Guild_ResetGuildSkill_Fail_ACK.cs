using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Guild_ResetGuildSkill_Fail_ACK : NetPacket
	{
		public Guild_ResetGuildSkill_Fail_ACK(int err, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(75);
			ns.Write(err);
			ns.Write(last);
		}
	}
}
