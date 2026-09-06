using System.Linq;
using AgentServer.Holders;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetShopDisplayDateLimitList : NetPacket
	{
		public GetShopDisplayDateLimitList(byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_DISPLAY_DATE_LIST_ACK);
			ns.Write(0);
			ns.Write(ShopHolder.ShopDisplayDateLimitList.Values.Count((ShopDisplayDateLimit c) => c.isEdit));
			foreach (ShopDisplayDateLimit item in ShopHolder.ShopDisplayDateLimitList.Values.Where((ShopDisplayDateLimit c) => c.isEdit))
			{
				ns.Write(item.ShopDisplayNum);
				ns.Write(item.DisplayStartDate);
				ns.Write(item.DisplayEndDate);
				ns.Write(item.BuyStartDate);
				ns.Write(item.BuyEndDate);
			}
			ns.Write(last);
		}
	}
}
