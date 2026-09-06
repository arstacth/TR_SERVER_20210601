using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using TRCommon;

namespace AgentServer.Packet.Send
{
	public sealed class Myroom_GetSlotInfoOK : NetPacket
	{
		public Myroom_GetSlotInfoOK(List<MyRoomSlotInfo> m_vSlotInfo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_MYROOM_ACK);
			ns.WriteOP(eMyRoomProtocol.eMyRoomProtocol_GET_USERSLOT_INFO_ACK);
			ns.Write(m_vSlotInfo.Count);
			foreach (MyRoomSlotInfo item in m_vSlotInfo)
			{
				ns.Write(item.m_iSlotNum);
				ns.WriteBIG5Fixed_intSize(item.m_strSlotName);
				for (int i = 0; i < 15; i++)
				{
					ns.Write(item.m_AvatarInfo.m_nItemPartArry[i]);
				}
				ns.Fill(136);
			}
			ns.Write(last);
		}
	}
}
