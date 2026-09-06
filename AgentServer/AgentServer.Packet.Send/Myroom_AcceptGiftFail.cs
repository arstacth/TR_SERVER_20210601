using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Myroom_AcceptGiftFail : NetPacket
	{
		public Myroom_AcceptGiftFail(byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_ACCEPT_GIFT_ACK);
			ns.Write(77);
			ns.Write((byte)0);
			ns.Write(last);
		}
	}
}
