using System.Linq;
using AgentServer.Holders;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetShopDisplayItemList : NetPacket
	{
		public GetShopDisplayItemList(byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_DISPLAY_LIST_ACK);
			ns.Write(0);
			ns.Write(ShopHolder.ShopDisplayItemList.Count((ShopDisplayItem c) => c.isEdit));
			foreach (ShopDisplayItem item in ShopHolder.ShopDisplayItemList.Where((ShopDisplayItem c) => c.isEdit))
			{
				ns.Write(item.ShopDisplayNum);
				ns.Write(item.Category1);
				ns.Write(item.Category2);
				ns.Write(item.Category3);
				ns.Write(item.DisplaySortNum);
				ns.Write(item.ItemNum);
				ns.Write(item.Gift);
				ns.WriteBIG5Fixed_intSize(item.ItemTag);
				ns.WriteBIG5Fixed_intSize(item.Desc);
			}
			ns.Write(last);
		}
	}
}
