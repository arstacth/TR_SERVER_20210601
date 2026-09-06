using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ModifyJoinLimitLevelFailACK : NetPacket
	{
		public ModifyJoinLimitLevelFailACK(byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(30);
			ns.Write((short)8);
			ns.Write(last);
		}
	}
}
