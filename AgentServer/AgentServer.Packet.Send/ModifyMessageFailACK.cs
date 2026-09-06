using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ModifyMessageFailACK : NetPacket
	{
		public ModifyMessageFailACK(byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(42);
			ns.Write((short)8);
			ns.Write(last);
		}
	}
}
