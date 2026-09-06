using System.Collections.Generic;
using System.Linq;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Shu;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_GetUserCharacterItemList : NetPacket
	{
		public Shu_GetUserCharacterItemList(DBShuInfo infos, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(2);
			ns.Write(0);
			ns.Write(0L);
			int num = infos.shuitems.Values.Sum((List<ShuItemInfo> s) => s.Count);
			ns.Write(num);
			if (num > 0)
			{
				foreach (List<ShuItemInfo> value4 in infos.shuitems.Values)
				{
					foreach (ShuItemInfo item in value4)
					{
						ns.Write(item.itemdescnum);
						ns.Write(item.itemID);
						ns.Write(item.gotDateTime);
						ns.Write(item.count);
						ns.Write(item.state);
					}
				}
				ns.Write(0);
				ns.Write(infos.characterItemID.Count);
				foreach (long item2 in infos.characterItemID)
				{
					infos.shuchars.TryGetValue(item2, out var value);
					infos.shuavatars.TryGetValue(item2, out var value2);
					infos.shustatus.TryGetValue(item2, out var value3);
					ns.Write(item2);
					ns.Write(item2);
					ns.Write(value.avatarItemNum);
					ns.Write((short)48);
					foreach (ShuAvatarInfo item3 in value2.OrderBy((ShuAvatarInfo o) => o.Position))
					{
						ns.Write(item3.itemID);
					}
					ns.Write((short)16);
					foreach (ShuStatusInfo item4 in value3)
					{
						ns.Write(item4.value);
					}
					ns.Write(value.MotionList);
					ns.Write(value.PurchaseMotionList);
					ns.WriteBIG5Fixed_intSize(value.Name);
					ns.Write(value.state);
				}
			}
			else
			{
				ns.Write(0L);
			}
			ns.Write(last);
		}
	}
}
