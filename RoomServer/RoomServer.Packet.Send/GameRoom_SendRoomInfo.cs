using LocalCommons.Network;
using LocalCommons.Utilities;
using RoomServer.Structuring;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class GameRoom_SendRoomInfo : NetPacket
	{
		public GameRoom_SendRoomInfo(NormalRoom room, byte last, byte roompos = 0)
		{
			int roomKindID = room.RoomKindID;
			string name = room.Name;
			string password = room.Password;
			int isTeamPlay = room.IsTeamPlay;
			bool isStepOn = room.IsStepOn;
			int itemType = room.ItemType;
			ns.WriteOP(Opcodes.eServer_ENTER_ROOM_ACK);
			ns.Write(0);
			ns.Write(roomKindID);
			ns.WriteBIG5Fixed_shortSize(name);
			ns.Write(room.MaxPlayersCount);
			ns.WriteBIG5Fixed_shortSize(password);
			int value = 2;
			ns.Write(value);
			int iD = room.ID;
			ns.Write(iD);
			ns.Write(roompos);
			ns.Write(room.MapNum);
			ns.Write(5);
			ns.Write(isTeamPlay);
			ns.Write(isStepOn);
			ns.Write(itemType);
			ns.Write(room.MapNum);
			ns.Write(Utility.CurrentTimeMilliseconds());
			ns.Write(room.PosWeight);
			ns.Write(last);
		}
	}
}
