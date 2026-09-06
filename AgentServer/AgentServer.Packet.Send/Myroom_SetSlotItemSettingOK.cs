using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using TRCommon;

namespace AgentServer.Packet.Send
{
	public sealed class Myroom_SetSlotItemSettingOK : NetPacket
	{
		public Myroom_SetSlotItemSettingOK(int slotNum, string SlotName, AvatarInfo avatarInfo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_MYROOM_ACK);
			ns.WriteOP(eMyRoomProtocol.eMyRoomProtocol_SET_SLOTITEM_SETTING_ACK);
			ns.Write(slotNum);
			ns.WriteBIG5Fixed_intSize(SlotName);
			for (int i = 0; i < 15; i++)
			{
				ns.Write(avatarInfo.m_nItemPartArry[i]);
			}
			ns.Fill(136);
			ns.Write(last);
		}
	}
}
