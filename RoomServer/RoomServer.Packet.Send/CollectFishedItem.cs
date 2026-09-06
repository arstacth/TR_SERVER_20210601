using System.Collections.Generic;
using LocalCommons.Network;
using RoomServer.Structuring.Fishing;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class CollectFishedItem : NetPacket
	{
		public CollectFishedItem(List<UserFishedItem> fishnetitems, byte last)
		{
			ns.WriteOP(Opcodes.eServer_FISHING_RECEIVE_FROM_KEEP_NET_ACK);
			ns.Fill(6);
			ns.Write(fishnetitems.Count);
			foreach (UserFishedItem fishnetitem in fishnetitems)
			{
				ns.Write(fishnetitem.ItemNum);
				ns.Write(fishnetitem.Size);
				ns.Write(fishnetitem.Count);
			}
			ns.Write(last);
		}
	}
}
