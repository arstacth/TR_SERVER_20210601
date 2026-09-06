using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class LOGIN_AUTH_ACK_THAI : NetPacket
	{
		public LOGIN_AUTH_ACK_THAI(string UserID, bool LoginCheckOK)
		{
			ns.WriteOP(Opcodes.eServer_LOGIN_AUTH_ACK);
			if (LoginCheckOK)
			{
				ns.Write(0);
			}
			else
			{
				ns.Write(1003);
			}
			if (LoginCheckOK)
			{
				ns.Write(2);
				ns.WriteBIG5Fixed_intSize("PCRoomResultCode");
				ns.WriteBIG5Fixed_intSize(string.Empty);
				ns.WriteBIG5Fixed_intSize("strTangID");
				ns.WriteBIG5Fixed_intSize(UserID);
			}
			else
			{
				ns.Write(0);
			}
		}
	}
}
