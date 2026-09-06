using System.Collections.Generic;
using AgentServer.Holders;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Room;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class CompetitionEvent_RoomKindPlayerNum : NetPacket
	{
		public CompetitionEvent_RoomKindPlayerNum(byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMPETITION_EVENT_PLAYERCOUNT_LIST_ACK);
			ns.Write(RoomHolder.RoomKindPlayerNum.Count);
			foreach (KeyValuePair<int, RoomKind_UserMinMax> item in RoomHolder.RoomKindPlayerNum)
			{
				ns.Write(item.Key);
				ns.Write(item.Value.MinUser);
				ns.Write(item.Value.MaxUser);
			}
			ns.Write(last);
		}
	}
}
