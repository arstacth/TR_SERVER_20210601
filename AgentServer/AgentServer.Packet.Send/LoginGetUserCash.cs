using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class LoginGetUserCash : NetPacket
	{
		public LoginGetUserCash(Account User, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_BALANCE_CASH_AFTER_CANCELLATION_ACK);
			ns.Write(0);
			ns.Write(User.Cash);
			ns.Write(last);
		}
	}
}
