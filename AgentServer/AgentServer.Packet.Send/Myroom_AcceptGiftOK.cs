using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Myroom_AcceptGiftOK : NetPacket
	{
		public Myroom_AcceptGiftOK(List<int> itemList, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_ACCEPT_GIFT_ACK);
			ns.Write(0);
			ns.Write(itemList.Count);
			foreach (int item in itemList)
			{
				ns.Write(item);
			}
			ns.Write(last);
		}
	}
}
