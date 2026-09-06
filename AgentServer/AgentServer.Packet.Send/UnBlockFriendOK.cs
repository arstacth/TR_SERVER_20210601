using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class UnBlockFriendOK : NetPacket
	{
		public UnBlockFriendOK(Account User, string nickname, byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMMUNITY_SERVER_PROTOCOL);
			ns.WriteOP(18);
			ns.WriteBIG5Fixed_intSize(nickname);
			ns.Write(last);
		}
	}
}
