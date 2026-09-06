using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GET_AVATAR_ACK : NetPacket
	{
		public GET_AVATAR_ACK(Account User, bool bRequestNickName, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GET_AVATAR_ACK);
			ns.Write(0);
			for (byte b = 0; b < 15; b = (byte)(b + 1))
			{
				ns.Write(User.advancedAvatarInfo.m_realAvatarInfo.m_nItemPartArry[b]);
			}
			for (byte b2 = 0; b2 < 7; b2 = (byte)(b2 + 1))
			{
				ns.Write(User.advancedAvatarInfo.m_realAvatarInfo.m_nGameAccArry[b2]);
			}
			for (byte b3 = 0; b3 < 1; b3 = (byte)(b3 + 1))
			{
				ns.Write(User.advancedAvatarInfo.m_realAvatarInfo.m_nEFItemArry[b3]);
			}
			for (byte b4 = 0; b4 < 12; b4 = (byte)(b4 + 1))
			{
				ns.Write(User.AvatarItemDyeing[b4].DyeingPart);
				ns.Write(User.AvatarItemDyeing[b4].Color1, 0, 3);
				ns.Write(User.AvatarItemDyeing[b4].Color2, 0, 3);
				ns.Write(User.AvatarItemDyeing[b4].Color3, 0, 3);
			}
			for (byte b5 = 0; b5 < 15; b5 = (byte)(b5 + 1))
			{
				ns.Write(User.advancedAvatarInfo.m_costumeAvatarInfo.m_nItemPartArry[b5]);
			}
			for (byte b6 = 0; b6 < 7; b6 = (byte)(b6 + 1))
			{
				ns.Write(User.advancedAvatarInfo.m_costumeAvatarInfo.m_nGameAccArry[b6]);
			}
			for (byte b7 = 0; b7 < 1; b7 = (byte)(b7 + 1))
			{
				ns.Write(User.advancedAvatarInfo.m_costumeAvatarInfo.m_nEFItemArry[b7]);
			}
			for (byte b8 = 12; b8 < 24; b8 = (byte)(b8 + 1))
			{
				ns.Write(User.AvatarItemDyeing[b8].DyeingPart);
				ns.Write(User.AvatarItemDyeing[b8].Color1, 0, 3);
				ns.Write(User.AvatarItemDyeing[b8].Color2, 0, 3);
				ns.Write(User.AvatarItemDyeing[b8].Color3, 0, 3);
			}
			ns.Write(User.advancedAvatarInfo.isUseCostume);
			ns.Write(value: false);
			ns.Write(bRequestNickName);
			ns.Write(last);
		}
	}
}
