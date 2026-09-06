using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class AddFriendFail : NetPacket
	{
		public AddFriendFail(Account User, string nickname, byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMMUNITY_SERVER_PROTOCOL);
			ns.WriteOP(4);
			ns.WriteBIG5Fixed_intSize(nickname);
			ns.Write((byte)0);
			ns.Write(last);
		}
	}
}
