using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ModifyJoinMethodFailACK : NetPacket
	{
		public ModifyJoinMethodFailACK(byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(33);
			ns.Write((short)8);
			ns.Write(last);
		}
	}
}
