using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ShopBuyCountResetNotify_ACK : NetPacket
	{
		public ShopBuyCountResetNotify_ACK()
		{
			ns.WriteOP(Opcodes.eServer_SHOP_USER_BUY_COUNT_RESET_NOTIFY);
			ns.Write((byte)1);
		}
	}
}
