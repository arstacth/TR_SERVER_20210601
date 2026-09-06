using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ModifyMemoOK_ACK : NetPacket
	{
		public ModifyMemoOK_ACK(string nickname, string memo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMMUNITY_SERVER_PROTOCOL);
			ns.WriteOP(45);
			ns.Write(0);
			ns.WriteBIG5Fixed_intSize(nickname);
			ns.WriteBIG5Fixed_intSize(memo);
			ns.Write(last);
		}
	}
}
