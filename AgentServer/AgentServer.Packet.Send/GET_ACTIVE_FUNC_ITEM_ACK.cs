using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using TRCommon;

namespace AgentServer.Packet.Send
{
	public sealed class GET_ACTIVE_FUNC_ITEM_ACK : NetPacket
	{
		public GET_ACTIVE_FUNC_ITEM_ACK(byte remainpage, short startindex, List<NetItemInfo> AvatarItems, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GET_ACTIVE_FUNC_ITEM_ACK);
			ns.Write(0);
			ns.Write(1563);
			ns.Write(remainpage);
			ns.Write(startindex);
			ns.Write((short)AvatarItems.Count);
			foreach (NetItemInfo AvatarItem in AvatarItems)
			{
				ns.Write(10273232);
				ns.Write(AvatarItem.m_character);
				ns.Write(AvatarItem.m_position);
				ns.Write(AvatarItem.m_kind);
				ns.Write(AvatarItem.m_iItemDescNum);
				ns.Write(AvatarItem.m_expireTime);
				ns.Write(AvatarItem.m_tGot);
				ns.Write(AvatarItem.m_count);
				ns.Write(AvatarItem.m_exp);
				ns.Write(AvatarItem.m_bHasExpireTime);
				ns.Write(AvatarItem.m_bUsing);
			}
			if (remainpage == 0)
			{
				ns.Write((short)0);
			}
			ns.Write(last);
		}
	}
}
