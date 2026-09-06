using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using TRCommon;

namespace AgentServer.Packet.Send
{
	public sealed class GET_ACTIVE_FUNC_ITEM_LIST_ACK : NetPacket
	{
		public GET_ACTIVE_FUNC_ITEM_LIST_ACK(Dictionary<int, NetItemInfo> AvatarItems, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GET_ACTIVE_FUNC_ITEM_ACK);
			ns.Write(0);
			ns.Write(275);
			ns.Write(AvatarItems.Count);
			foreach (NetItemInfo value in AvatarItems.Values)
			{
				ns.Write(10273232);
				ns.Write(value.m_character);
				ns.Write(value.m_position);
				ns.Write(value.m_kind);
				ns.Write(value.m_iItemDescNum);
				ns.Write(value.m_expireTime);
				ns.Write(value.m_tGot);
				ns.Write(value.m_count);
				ns.Write(value.m_exp);
				ns.Write(value.m_bHasExpireTime);
				ns.Write(value.m_bUsing);
			}
			ns.Write(last);
		}
	}
}
