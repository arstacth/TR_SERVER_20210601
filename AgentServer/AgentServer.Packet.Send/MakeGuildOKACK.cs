using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class MakeGuildOKACK : NetPacket
	{
		public MakeGuildOKACK(long TR, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(3);
			ns.Write(TR);
			ns.Write(last);
		}
	}
}
