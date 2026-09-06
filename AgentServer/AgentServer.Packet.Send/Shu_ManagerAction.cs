using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Shu;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_ManagerAction : NetPacket
	{
		public Shu_ManagerAction(int actionType, long shuitemid, DBShuActionInfo infos, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(9);
			ns.Write(0);
			ns.Write(shuitemid);
			ns.Write(actionType);
			ns.Write(infos.remainMP);
			ns.Write(infos.ActionResult.Count);
			foreach (ShuActionResultInfo item in infos.ActionResult)
			{
				ns.Write(item.statusType);
				ns.Write(item.giveValue);
			}
			ns.Write(infos.shustatus.Count);
			foreach (KeyValuePair<long, List<ShuStatusInfo>> item2 in infos.shustatus)
			{
				ns.Write(item2.Key);
				ns.Write((short)16);
				foreach (ShuStatusInfo item3 in item2.Value)
				{
					ns.Write(item3.value);
				}
			}
			ns.Write(last);
		}
	}
}
