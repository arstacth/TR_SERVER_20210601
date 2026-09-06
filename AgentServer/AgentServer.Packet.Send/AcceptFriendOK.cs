using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class AcceptFriendOK : NetPacket
	{
		public AcceptFriendOK(Account User, string nickname, byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMMUNITY_SERVER_PROTOCOL);
			ns.WriteOP(9);
			ns.WriteBIG5Fixed_intSize(nickname);
			ns.Write(0);
			ns.Write(0L);
			ns.Write(last);
		}
	}
}
