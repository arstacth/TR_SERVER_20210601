using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ModifyMessageACK : NetPacket
	{
		public ModifyMessageACK(string Message, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(41);
			ns.WriteBIG5Fixed_intSize(Message);
			ns.Write(last);
		}
	}
}
