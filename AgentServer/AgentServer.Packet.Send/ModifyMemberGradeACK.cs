using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ModifyMemberGradeACK : NetPacket
	{
		public ModifyMemberGradeACK(string Name, short Grade, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(38);
			ns.WriteBIG5Fixed_intSize(Name);
			ns.Write(Grade);
			ns.Write(last);
		}
	}
}
