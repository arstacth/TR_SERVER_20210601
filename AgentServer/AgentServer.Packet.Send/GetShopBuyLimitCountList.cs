using System.Linq;
using AgentServer.Holders;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetShopBuyLimitCountList : NetPacket
	{
		public GetShopBuyLimitCountList(byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_BUY_LIMIT_COUNT_LIST_ACK);
			ns.Write(0);
			ns.Write(ShopHolder.ShopBuyLimitCountList.Count((ShopBuyLimitCount c) => c.isEdit));
			foreach (ShopBuyLimitCount item in ShopHolder.ShopBuyLimitCountList.Where((ShopBuyLimitCount c) => c.isEdit))
			{
				ns.Write(item.ShopDisplayNum);
				ns.Write(item.DayPurchasingLimit);
				ns.Write(item.MonthPurchasingLimit);
				ns.Write(item.TotalPurchasingLimit);
			}
			ns.Write(last);
		}
	}
}
