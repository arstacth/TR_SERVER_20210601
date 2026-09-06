using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class CheckGuildNameACK : NetPacket
	{
		public CheckGuildNameACK(byte type, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(1);
			ns.Write(type);
			ns.Write(last);
		}
	}
}
