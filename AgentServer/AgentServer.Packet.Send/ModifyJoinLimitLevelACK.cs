using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ModifyJoinLimitLevelACK : NetPacket
	{
		public ModifyJoinLimitLevelACK(short joinLimitLevel, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(29);
			ns.Write(joinLimitLevel);
			ns.Write(last);
		}
	}
}
