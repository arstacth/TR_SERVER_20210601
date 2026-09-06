using System.Linq;
using AgentServer.Holders;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetShopBuyAddBenefitList : NetPacket
	{
		public GetShopBuyAddBenefitList(byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_BUY_ADD_BENEFIT_LIST_ACK);
			ns.Write(0);
			ns.Write(ShopHolder.ShopBuyAddBenefitList.Count((ShopBuyAddBenefit c) => c.isEdit));
			foreach (ShopBuyAddBenefit item in ShopHolder.ShopBuyAddBenefitList.Where((ShopBuyAddBenefit c) => c.isEdit))
			{
				ns.Write(item.SellItemNum);
				ns.Write(item.ItemNum);
				ns.Write(item.Count);
			}
			ns.Write(last);
		}
	}
}
