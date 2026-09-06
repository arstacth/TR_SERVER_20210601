using System.Linq;
using AgentServer.Holders;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetShopItemSellList : NetPacket
	{
		public GetShopItemSellList(byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_ITEL_SELL_LIST_ACK);
			ns.Write(0);
			ns.Write(ShopHolder.ShopItemSellList.Values.Where((ShopItemSell w) => w.isEdit).Count());
			foreach (ShopItemSell item in ShopHolder.ShopItemSellList.Values.Where((ShopItemSell w) => w.isEdit))
			{
				ns.Write(item.SellItemNum);
				ns.Write(item.ShopDisplayNum);
				ns.Write(item.ItemNum);
				ns.Write(item.OriginCostType);
				ns.Write(item.OriginPrice);
				ns.Write(item.CostType);
				ns.Write(item.Price);
				ns.Write(item.MileageType);
				ns.Write(item.Mileage);
				ns.Write(item.ShopDisplay);
			}
			ns.Write(last);
		}
	}
}
