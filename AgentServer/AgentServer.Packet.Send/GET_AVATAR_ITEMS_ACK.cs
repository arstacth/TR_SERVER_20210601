using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using TRCommon;

namespace AgentServer.Packet.Send
{
	public sealed class GET_AVATAR_ITEMS_ACK : NetPacket
	{
		public GET_AVATAR_ITEMS_ACK(int charid, int position, byte remainpage, short startindex, List<NetItemInfo> AvatarItems, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GET_AVATAR_ITEMS_ACK);
			ns.Write((ushort)charid);
			ns.Write(position);
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
			ns.Write(last);
		}
	}
}
