using LocalCommons.Network;
using LocalCommons.Utilities;
using RoomServer.Structuring;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class SendFarmInfo : NetPacket
	{
		public SendFarmInfo(Account User, NormalRoom room, int FarmUniqueNum, byte last)
		{
			ns.WriteOP(Opcodes.eServer_ENTER_ROOM_ACK);
			ns.Write(0);
			ns.Write(room.RoomKindID);
			ns.WriteBIG5Fixed_shortSize(room.Name);
			ns.Write(room.MaxPlayersCount);
			ns.WriteBIG5Fixed_shortSize(room.Password);
			ns.Write(2);
			ns.Write(room.ID);
			ns.Write(User.RoomPos);
			ns.Write(room.MapNum);
			ns.Write(30);
			ns.Write(room.IsTeamPlay);
			ns.Write(room.IsStepOn);
			ns.Write(room.ItemType);
			ns.Write(0);
			ns.Write(Utility.CurrentTimeMilliseconds());
			ns.Write(room.PosWeight);
			ns.Write(FarmUniqueNum);
			ns.Write(room.FarmRoomInfo.FarmWeatherTypeNum);
			ns.Write(room.FarmRoomInfo.FarmSkyTypeNum);
			ns.Write(room.FarmRoomInfo.FarmTypeNum);
			ns.WriteBIG5Fixed_intSize(room.FarmRoomInfo.FarmName);
			ns.WriteBIG5Fixed_intSize(room.FarmRoomInfo.MasterName);
			ns.Write(room.FarmRoomInfo.ExpireTime);
			ns.Write(room.FarmRoomInfo.CreateTime);
			ns.Write(room.FarmRoomInfo.Password == string.Empty);
			ns.Write(room.FarmRoomInfo.isPublic);
			ns.Write((byte)0);
			ns.Write(room.FarmRoomInfo.TotalCount);
			ns.Write(room.FarmRoomInfo.TodaysVisitorCount);
			ns.Fill(3);
			ns.Write(room.FarmRoomInfo.PremiumFarmUsing);
			ns.Write(room.FarmRoomInfo.PremiumFarmExpireDateTime);
			ns.Write(room.FarmRoomInfo.farmExp);
			ns.Write(0);
			if (room.FarmRoomInfo.isPublic)
			{
				ns.Write(room.FarmRoomInfo.Type);
				ns.Write(room.FarmRoomInfo.MaxUserLimit);
				ns.Write(room.FarmRoomInfo.unk1);
				ns.WriteBIG5Fixed_intSize(room.FarmRoomInfo.FarmName);
				ns.Write((byte)0);
			}
			else if (!room.FarmRoomInfo.isGuildFarm)
			{
				ns.Fill(6);
			}
			ns.Write(last);
		}
	}
}
