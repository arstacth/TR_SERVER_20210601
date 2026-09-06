using System.Collections.Generic;
using System.Linq;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Shu;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_HatchOK : NetPacket
	{
		public Shu_HatchOK(long eggitemid, DBShuInfo infos, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(1);
			ns.Write(0);
			ns.Write(eggitemid);
			ns.Write(infos.characterItemID[0]);
			int num = infos.shuitems.Values.Sum((List<ShuItemInfo> s) => s.Count);
			bool flag = false;
			if (infos.shuitems.TryGetValue(0L, out var value))
			{
				flag = true;
			}
			else
			{
				num++;
			}
			ns.Write(num);
			if (flag)
			{
				ns.Write(0);
				ns.Write(eggitemid);
				ns.Write(value[0].gotDateTime);
				ns.Write(value[0].count);
				ns.Write(value[0].state);
			}
			else
			{
				ns.Write(0);
				ns.Write(eggitemid);
				ns.Fill(16);
			}
			foreach (KeyValuePair<long, List<ShuItemInfo>> item in infos.shuitems.Where((KeyValuePair<long, List<ShuItemInfo>> w) => w.Key != 0))
			{
				foreach (ShuItemInfo item2 in item.Value)
				{
					ns.Write(item2.itemdescnum);
					ns.Write(item2.itemID);
					ns.Write(item2.gotDateTime);
					ns.Write(item2.count);
					ns.Write(item2.state);
				}
			}
			ns.Write(0);
			ns.Write(infos.characterItemID.Count);
			foreach (long item3 in infos.characterItemID)
			{
				infos.shuchars.TryGetValue(item3, out var value2);
				infos.shuavatars.TryGetValue(item3, out var value3);
				infos.shustatus.TryGetValue(item3, out var value4);
				ns.Write(item3);
				ns.Write(item3);
				ns.Write(value2.avatarItemNum);
				ns.Write((short)48);
				foreach (ShuAvatarInfo item4 in value3.OrderBy((ShuAvatarInfo o) => o.Position))
				{
					ns.Write(item4.itemID);
				}
				ns.Write((short)16);
				foreach (ShuStatusInfo item5 in value4)
				{
					ns.Write(item5.value);
				}
				ns.Write(value2.MotionList);
				ns.Write(value2.PurchaseMotionList);
				ns.WriteBIG5Fixed_intSize(value2.Name);
				ns.Write(value2.state);
			}
			ns.Write(last);
		}
	}
}
