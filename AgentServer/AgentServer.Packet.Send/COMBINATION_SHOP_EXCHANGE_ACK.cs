using System.Collections.Generic;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class COMBINATION_SHOP_EXCHANGE_ACK : NetPacket
	{
		public COMBINATION_SHOP_EXCHANGE_ACK(eCombinationShopResult result, bool bIsExhcangeSucess, List<ExchangeItemInfo> consumeList, List<ExchangeItemInfo> resultList, byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMBINATION_SHOP_EXCHANGE_ACK);
			ns.Write((int)result);
			if (result == eCombinationShopResult.eCombinationShopResult_OK)
			{
				ns.Write(bIsExhcangeSucess);
				ns.Write(resultList.Count);
				foreach (ExchangeItemInfo result2 in resultList)
				{
					ns.Write(result2.type);
					ns.Write(result2.id);
					ns.Write(result2.count);
					ns.Write(int.MaxValue);
				}
			}
			ns.Write(last);
		}
	}
}
