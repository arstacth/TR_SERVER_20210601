using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class LoginGetNickName_0X1C : NetPacket
	{
		public LoginGetNickName_0X1C(Account User, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GET_NICKNAME_ACK);
			if (User.noNickName)
			{
				ns.Write(57);
			}
			else
			{
				ns.Write(0);
				ns.WriteBIG5Fixed_intSize(User.NickName);
			}
			ns.Write(last);
		}
	}
}
