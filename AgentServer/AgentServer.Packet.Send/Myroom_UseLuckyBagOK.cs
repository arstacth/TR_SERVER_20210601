using System.Collections.Concurrent;
using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Myroom_UseLuckyBagOK : NetPacket
	{
		public Myroom_UseLuckyBagOK(int itemnum, byte opennum, ConcurrentDictionary<int, byte> itemlist, byte last)
		{
			ns.WriteOP(Opcodes.eServer_MYROOM_ACK);
			ns.WriteOP(eMyRoomProtocol.eMyRoomProtocol_USE_LUCKY_BAG_ACK);
			ns.Write(0);
			ns.Write(itemlist.Count);
			foreach (KeyValuePair<int, byte> item in itemlist)
			{
				ns.Write(item.Key);
				ns.Write(item.Value);
			}
			ns.Write(itemnum);
			ns.Write(opennum);
			ns.Write(last);
		}
	}
}
