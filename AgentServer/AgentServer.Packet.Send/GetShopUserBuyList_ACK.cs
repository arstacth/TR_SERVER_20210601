using System.Collections.Generic;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetShopUserBuyList_ACK : NetPacket
	{
		public GetShopUserBuyList_ACK(List<ShopBuyCountList> UserBuyCountList, List<GameDataShopPurchasingLimit> ShopPurchasingLimit, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHOP_USER_BUY_COUNT_LIST_ACK);
			ns.Write(0);
			ns.Write(UserBuyCountList.Count);
			foreach (ShopBuyCountList UserBuyCount in UserBuyCountList)
			{
				ns.Write(UserBuyCount.ShopDisplayNum);
				ns.Write(UserBuyCount.DayPurchasingLimit);
				ns.Write(UserBuyCount.MonthPurchasingLimit);
				ns.Write(UserBuyCount.TotalPurchasingLimit);
			}
			ns.Write(0);
			ns.Write(ShopPurchasingLimit.Count);
			foreach (GameDataShopPurchasingLimit item in ShopPurchasingLimit)
			{
				ns.Write(item.ShopDisplayNum);
				ns.Write(item.TotalPurchasingLimit);
			}
			ns.Write(last);
		}
	}
}
