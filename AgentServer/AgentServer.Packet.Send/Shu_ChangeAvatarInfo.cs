using System.Collections.Generic;
using System.Linq;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Shu;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_ChangeAvatarInfo : NetPacket
	{
		public Shu_ChangeAvatarInfo(long shuitemid, DBShuChangeAVInfo infos, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(11);
			ns.Write(0);
			ns.Write(shuitemid);
			ns.Write(infos.AvatarState.Count);
			foreach (ShuAvatarState item in infos.AvatarState)
			{
				ns.Write(item.itemID);
				ns.Write(item.state);
			}
			ns.Write(infos.shuavatars.Count);
			foreach (KeyValuePair<long, List<ShuAvatarInfo>> shuavatar in infos.shuavatars)
			{
				ns.Write(shuavatar.Key);
				ns.Write((short)48);
				foreach (ShuAvatarInfo item2 in shuavatar.Value.OrderBy((ShuAvatarInfo o) => o.Position))
				{
					ns.Write(item2.itemID);
				}
			}
			ns.Write(last);
		}
	}
}
