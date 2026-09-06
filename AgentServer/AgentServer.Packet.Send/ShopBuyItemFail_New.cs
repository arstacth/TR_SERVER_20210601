using System.Collections.Generic;
using System.Linq;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ShopBuyItemFail_New : NetPacket
	{
		public ShopBuyItemFail_New(eShopFailed_REASON failedReason, List<ShopBuyItemInfo> buyitemOKlist, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_BUY_PRODUCTS_TOGETHER_ACK);
			ns.Write(74);
			ns.Write((byte)failedReason);
			ns.Write(buyitemOKlist.Count((ShopBuyItemInfo c) => c.BuySuccess));
			foreach (ShopBuyItemInfo item in buyitemOKlist.Where((ShopBuyItemInfo w) => w.BuySuccess))
			{
				ns.Write(item.ItemNum);
				ns.Write(item.unk3);
				ns.Write(-1L);
				ns.Write(0);
				ns.Write(item.unk4);
				ns.Write(-1L);
				ns.Fill(20);
				ns.Write(item.SellItemNum);
			}
			ns.Write(buyitemOKlist.Count((ShopBuyItemInfo c) => !c.BuySuccess));
			foreach (ShopBuyItemInfo item2 in buyitemOKlist.Where((ShopBuyItemInfo w) => !w.BuySuccess))
			{
				ns.Write(item2.ItemNum);
				ns.Write(item2.unk3);
				ns.Write(-1L);
				ns.Write(0);
				ns.Write(item2.unk4);
				ns.Write(-1L);
				ns.Fill(20);
				ns.Write(item2.SellItemNum);
			}
			ns.Write(0);
			ns.Write(buyitemOKlist.Count((ShopBuyItemInfo c) => c.BuySuccess));
			foreach (ShopBuyItemInfo item3 in buyitemOKlist.Where((ShopBuyItemInfo w) => w.BuySuccess))
			{
				ns.Write(item3.ItemNum);
				ns.Write(0);
			}
			ns.Write(last);
		}
	}
}
