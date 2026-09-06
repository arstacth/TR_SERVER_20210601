using LocalCommons.Network;
using RoomServer.Structuring;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class GameRoom_ChangeSetting : NetPacket
	{
		public GameRoom_ChangeSetting(NormalRoom room, byte last)
		{
			ns.WriteOP(RoomOpcodes.eRoom_ROOM_OPTION_MODIFY_ROOMINFO_ACK);
			ns.WriteBIG5Fixed_intSize(room.Name);
			ns.WriteBIG5Fixed_intSize(room.Password);
			ns.Write(room.IsStepOn);
			ns.Write(room.ItemType);
			ns.Write(last);
		}
	}
}
