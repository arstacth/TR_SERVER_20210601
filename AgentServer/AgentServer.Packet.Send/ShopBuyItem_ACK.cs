using System.Collections.Generic;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class ShopBuyItem_ACK : NetPacket
	{
		public ShopBuyItem_ACK(List<ShopBuyItemInfo> buyitemOKlist, bool isFarmOrShuItem, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_BUY_PRODUCTS_TOGETHER_ACK);
			ns.Write(0);
			ns.Write(buyitemOKlist.Count);
			foreach (ShopBuyItemInfo item in buyitemOKlist)
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
			if (isFarmOrShuItem)
			{
				ns.Write(buyitemOKlist.Count);
				foreach (ShopBuyItemInfo item2 in buyitemOKlist)
				{
					ns.Write(item2.ItemID);
					ns.Write(item2.supplyItemDescNum);
				}
			}
			else
			{
				ns.Write(0);
			}
			ns.Write((byte)0);
			ns.Write(buyitemOKlist.Count);
			foreach (ShopBuyItemInfo item3 in buyitemOKlist)
			{
				ns.Write(item3.ItemNum);
				ns.Write(0);
			}
			ns.Write(0);
			ns.Write(last);
		}
	}
}
