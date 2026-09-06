using System.Collections.Generic;
using System.Linq;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Shu;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_UseItem : NetPacket
	{
		public Shu_UseItem(int position, long shucharitemid, long shuitemid, int itemnum, int usecount, DBShuUseItemInfo infos, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(13);
			ns.Write(0);
			ns.Write(shucharitemid);
			ns.Write(shuitemid);
			ns.Write(itemnum);
			ns.Write(usecount);
			ns.Write(position);
			switch (position)
			{
			case 2001:
				ns.Write(infos.shustatus.Count);
				foreach (KeyValuePair<long, List<ShuStatusInfo>> item in infos.shustatus)
				{
					ns.Write(item.Key);
					ns.Write((short)16);
					foreach (ShuStatusInfo item2 in item.Value)
					{
						ns.Write(item2.value);
					}
				}
				break;
			case 2002:
				ns.Write(infos.remainMP);
				break;
			case 2003:
				ns.Write(0);
				ns.Write(infos.characterItemID.Count);
				foreach (long item3 in infos.characterItemID)
				{
					infos.shuchars.TryGetValue(item3, out var value);
					infos.shuavatars.TryGetValue(item3, out var value2);
					infos.shustatus.TryGetValue(item3, out var value3);
					ns.Write(item3);
					ns.Write(item3);
					ns.Write(value.avatarItemNum);
					ns.Write((short)48);
					foreach (ShuAvatarInfo item4 in value2.OrderBy((ShuAvatarInfo o) => o.Position))
					{
						ns.Write(item4.itemID);
					}
					ns.Write((short)16);
					foreach (ShuStatusInfo item5 in value3)
					{
						ns.Write(item5.value);
					}
					ns.Write(value.MotionList);
					ns.Write(value.PurchaseMotionList);
					ns.WriteBIG5Fixed_intSize(value.Name);
					ns.Write(value.state);
				}
				break;
			}
			if (infos.ItemInfos.Count > 0)
			{
				ns.Write(infos.ItemInfos.Count);
				foreach (ShuItemInfo itemInfo in infos.ItemInfos)
				{
					ns.Write(itemInfo.itemdescnum);
					ns.Write(itemInfo.itemID);
					ns.Write(itemInfo.gotDateTime);
					ns.Write(itemInfo.count);
					ns.Write(itemInfo.state);
				}
			}
			else
			{
				ns.Write(1);
				ns.Write(0);
				ns.Write(shuitemid);
				ns.Fill(16);
			}
			ns.Write(last);
		}
	}
}
