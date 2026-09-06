using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GuildProcessLeaveFailACK : NetPacket
	{
		public GuildProcessLeaveFailACK(byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(22);
			ns.Write((short)10);
			ns.Write(last);
		}
	}
}
