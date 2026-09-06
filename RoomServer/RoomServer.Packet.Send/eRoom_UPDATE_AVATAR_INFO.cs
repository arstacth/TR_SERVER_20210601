using LocalCommons.Network;
using RoomServer.Structuring;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class eRoom_UPDATE_AVATAR_INFO : NetPacket
	{
		public eRoom_UPDATE_AVATAR_INFO(Account User, byte last)
		{
			ns.WriteOP(RoomOpcodes.eRoom_UPDATE_AVATAR_INFO);
			ns.Write(User.RoomPos);
			for (byte b = 0; b < 15; b = (byte)(b + 1))
			{
				ns.Write(User.advancedAvatarInfo.m_realAvatarInfo.m_nItemPartArry[b]);
			}
			ns.Fill(16);
			for (int i = 0; i < 12; i++)
			{
				ns.Write(User.AvatarItemDyeing[i].DyeingPart);
				ns.Write(User.AvatarItemDyeing[i].Color1, 0, 3);
				ns.Write(User.AvatarItemDyeing[i].Color2, 0, 3);
				ns.Write(User.AvatarItemDyeing[i].Color3, 0, 3);
			}
			for (byte b2 = 0; b2 < 15; b2 = (byte)(b2 + 1))
			{
				ns.Write(User.advancedAvatarInfo.m_costumeAvatarInfo.m_nItemPartArry[b2]);
			}
			ns.Fill(16);
			for (int j = 12; j < 24; j++)
			{
				ns.Write(User.AvatarItemDyeing[j].DyeingPart);
				ns.Write(User.AvatarItemDyeing[j].Color1, 0, 3);
				ns.Write(User.AvatarItemDyeing[j].Color2, 0, 3);
				ns.Write(User.AvatarItemDyeing[j].Color3, 0, 3);
			}
			ns.Write(User.advancedAvatarInfo.isUseCostume);
			ns.Write(value: false);
			User.userItemAttr.encodeUserItemAttr(ns);
			User.userItemAttr.encodeUserCharAttr(ns);
			ns.Write(last);
		}
	}
}
