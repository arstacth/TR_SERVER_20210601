using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ModifyMemberGradeFailACK : NetPacket
	{
		public ModifyMemberGradeFailACK(byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(39);
			ns.Write((short)8);
			ns.Write(last);
		}
	}
}
