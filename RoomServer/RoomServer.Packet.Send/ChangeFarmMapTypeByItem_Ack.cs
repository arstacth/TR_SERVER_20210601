using LocalCommons.Network;
using RoomServer.Structuring;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class ChangeFarmMapTypeByItem_Ack : NetPacket
	{
		public ChangeFarmMapTypeByItem_Ack(int FarmUniqueNum, NormalRoom room, byte last)
		{
			ns.WriteOP(Opcodes.eServer_FARM_ACK);
			ns.WriteOP(FarmProtocol.ChangeFarmMapTypeByItem_ACK);
			ns.Write(0);
			ns.Write(FarmUniqueNum);
			ns.Write(room.FarmRoomInfo.FarmTypeNum);
			ns.Write(last);
		}
	}
}
