using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ModifyJoinMethodACK : NetPacket
	{
		public ModifyJoinMethodACK(short joinMethod, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(32);
			ns.Write(joinMethod);
			ns.Write(last);
		}
	}
}
