using System.Linq;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Shu;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_ChangeCurrentShu : NetPacket
	{
		public Shu_ChangeCurrentShu(long beforeCharacterItemID, long shuitemid, DBShuInfo infos, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(12);
			ns.Write(0);
			ns.Write(beforeCharacterItemID);
			ns.Write(shuitemid);
			ns.Write(0);
			if (shuitemid != -1)
			{
				ns.Write(infos.characterItemID.Count);
				foreach (long item in infos.characterItemID)
				{
					infos.shuchars.TryGetValue(item, out var value);
					infos.shuavatars.TryGetValue(item, out var value2);
					infos.shustatus.TryGetValue(item, out var value3);
					ns.Write(item);
					ns.Write(item);
					ns.Write(value.avatarItemNum);
					ns.Write((short)48);
					foreach (ShuAvatarInfo item2 in value2.OrderBy((ShuAvatarInfo o) => o.Position))
					{
						ns.Write(item2.itemID);
					}
					ns.Write((short)16);
					foreach (ShuStatusInfo item3 in value3)
					{
						ns.Write(item3.value);
					}
					ns.Write(value.MotionList);
					ns.Write(value.PurchaseMotionList);
					ns.WriteBIG5Fixed_intSize(value.Name);
					ns.Write(value.state);
				}
			}
			else
			{
				ns.Write(0);
			}
			ns.Write(last);
		}
	}
}
