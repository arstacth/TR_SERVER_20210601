using LocalCommons.Network;
using RoomServer.Structuring;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class GameRoom_SendPlayerInfo : NetPacket
	{
		public GameRoom_SendPlayerInfo(Account User, int RoomKindID, byte last)
		{
			ns.WriteOP(Opcodes.eServer_NEW_ROOM_USER_ACK);
			ns.Write(User.Session);
			ns.Write(User.RoomPos);
			ns.Write(User.UDPInfo, 0, 48);
			ns.WriteBIG5Fixed_intSize(User.NickName);
			if (User.GuildNum > 0)
			{
				ns.WriteBIG5Fixed_intSize(User.GuildInfo.guildName);
				ns.Write((long)User.GuildNum);
				ns.Write((short)User.GuildInfo.level);
				ns.Write((short)User.GuildUserInfo.grade);
			}
			else
			{
				ns.WriteBIG5Fixed_intSize(string.Empty);
				ns.Write(0L);
				ns.Write((short)0);
				ns.Write((short)0);
			}
			ns.Write((byte)100);
			ns.Write(User.IsReady);
			for (int i = 0; i < 15; i++)
			{
				ns.Write(User.advancedAvatarInfo.m_realAvatarInfo.m_nItemPartArry[i]);
			}
			ns.Fill(16);
			for (int j = 0; j < 12; j++)
			{
				ns.Write(User.AvatarItemDyeing[j].DyeingPart);
				ns.Write(User.AvatarItemDyeing[j].Color1, 0, 3);
				ns.Write(User.AvatarItemDyeing[j].Color2, 0, 3);
				ns.Write(User.AvatarItemDyeing[j].Color3, 0, 3);
			}
			for (int k = 0; k < 15; k++)
			{
				ns.Write(User.advancedAvatarInfo.m_costumeAvatarInfo.m_nItemPartArry[k]);
			}
			ns.Fill(16);
			for (int l = 12; l < 24; l++)
			{
				ns.Write(User.AvatarItemDyeing[l].DyeingPart);
				ns.Write(User.AvatarItemDyeing[l].Color1, 0, 3);
				ns.Write(User.AvatarItemDyeing[l].Color2, 0, 3);
				ns.Write(User.AvatarItemDyeing[l].Color3, 0, 3);
			}
			ns.Write(User.advancedAvatarInfo.isUseCostume);
			ns.Write(value: false);
			ns.Write((short)0);
			ns.Write(User.Exp);
			ns.Write(User.Team);
			ns.Write(User.RelayTeamPos);
			ns.Write(User.RoomPos);
			ns.Write(0);
			ns.Fill(16);
			ns.Write(User.CoupleInfo.CoupleNum);
			ns.Write(User.CoupleInfo.CoupleType);
			ns.WriteBIG5Fixed_intSize(User.CoupleInfo.MateName);
			ns.Write(User.CoupleInfo.CreateTime);
			ns.Write(User.CoupleInfo.MarriedTime);
			ns.Write(User.CoupleInfo.RingChangedTime);
			ns.Write(User.CoupleInfo.CoupleRingNum);
			ns.Write(User.CoupleInfo.MaxRingDays);
			ns.Write(User.CoupleInfo.CoupleLevel);
			ns.Write((short)0);
			ns.Write(User.CoupleInfo.CondDays);
			ns.Write(User.CoupleInfo.AccumulateExp);
			ns.Write(User.CoupleInfo.CoupleRank);
			ns.Write(User.CoupleInfo.CouplePoint);
			ns.Write(0);
			ns.Write((byte)0);
			ns.Write(value: false);
			User.activeItem.encodeActiveItems(ns);
			User.userItemAttr.encodeUserItemAttr(ns);
			User.userItemAttr.encodeUserCharAttr(ns);
			if (RoomKindID == 75)
			{
				ns.Write(User.MyFarmUniqueNum);
				ns.Write(User.MyFarmInfo.FarmWeatherTypeNum);
				ns.Write(User.MyFarmInfo.FarmSkyTypeNum);
				ns.Write(User.MyFarmInfo.FarmTypeNum);
				ns.WriteBIG5Fixed_intSize(User.MyFarmInfo.FarmName);
				ns.WriteBIG5Fixed_intSize(User.MyFarmInfo.MasterName);
				ns.Write(User.MyFarmInfo.ExpireTime);
				ns.Write(User.MyFarmInfo.CreateTime);
				ns.Write((byte)1);
				ns.Write(User.MyFarmInfo.isPublic);
				ns.Write((byte)0);
				ns.Write(User.MyFarmInfo.TotalCount);
				ns.Write(User.MyFarmInfo.TodaysVisitorCount);
				ns.Fill(3);
				ns.Write(User.MyFarmInfo.PremiumFarmUsing);
				ns.Write(User.MyFarmInfo.PremiumFarmExpireDateTime);
				ns.Write(User.MyFarmInfo.farmExp);
				ns.Write(0);
			}
			else
			{
				ns.Write(0);
				ns.Write(0);
				ns.Write(0);
				ns.Write(0);
				ns.WriteBIG5Fixed_intSize(string.Empty);
				ns.WriteBIG5Fixed_intSize(string.Empty);
				ns.Write(0L);
				ns.Write(0L);
				ns.Write((byte)1);
				ns.Write(value: false);
				ns.Write((byte)0);
				ns.Write(0);
				ns.Write(0);
				ns.Fill(3);
				ns.Write(value: false);
				ns.Write(0L);
				ns.Write(0);
				ns.Write(0);
			}
			ns.Write((byte)0);
			ns.Write(User.PartyType);
			ns.Write(User.SubPartyType);
			ns.Write(User.UseShu);
			if (User.UseShu)
			{
				ns.WriteBIG5Fixed_intSize(User.UserShuInfo.ShuName);
				ns.Write(User.UserShuInfo.ShuItemNum);
				ns.Write(User.UserShuInfo.Statusinfo[3]);
				ns.Write(1);
				ns.Write((short)12);
				foreach (short item in User.UserShuInfo.ShuAvatarKind)
				{
					ns.Write(item);
				}
				ns.Write((short)16);
				foreach (int item2 in User.UserShuInfo.Statusinfo)
				{
					ns.Write(item2);
				}
				ns.Write(User.UserShuInfo.MotionList);
			}
			ns.Write(User.TopRank);
			User.avatarLock.encode(ns);
			ns.Write(1058251047);
			ns.Write((byte)1);
			ns.Write(last);
		}
	}
}
