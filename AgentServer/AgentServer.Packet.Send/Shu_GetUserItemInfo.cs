using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Shu;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_GetUserItemInfo : NetPacket
	{
		public Shu_GetUserItemInfo(int kind, List<ShuItemInfo> iteminfos, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(5);
			ns.Write(0);
			ns.Write(kind);
			ns.Write(iteminfos.Count);
			foreach (ShuItemInfo iteminfo in iteminfos)
			{
				ns.Write(iteminfo.itemdescnum);
				ns.Write(iteminfo.itemID);
				ns.Write(iteminfo.gotDateTime);
				ns.Write(iteminfo.count);
				ns.Write(iteminfo.state);
			}
			ns.Write(last);
		}
	}
}
