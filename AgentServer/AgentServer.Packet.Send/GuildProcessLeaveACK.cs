using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GuildProcessLeaveACK : NetPacket
	{
		public GuildProcessLeaveACK(string name, byte bySelf, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(21);
			ns.WriteBIG5Fixed_intSize(name);
			ns.Write(bySelf);
			ns.Write(last);
		}
	}
}
