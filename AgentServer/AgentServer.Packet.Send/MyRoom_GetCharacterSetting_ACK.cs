using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using TRCommon;

namespace AgentServer.Packet.Send
{
	public sealed class MyRoom_GetCharacterSetting_ACK : NetPacket
	{
		public MyRoom_GetCharacterSetting_ACK(AvatarInfo AvatarInfo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_MYROOM_ACK);
			ns.WriteOP(eMyRoomProtocol.eServer_MYROOM_GET_MY_CHARACTER_SETTING_ACK);
			for (byte b = 0; b < 15; b = (byte)(b + 1))
			{
				ns.Write(AvatarInfo.m_nItemPartArry[b]);
			}
			ns.Fill(136);
			ns.Write(last);
		}
	}
}
