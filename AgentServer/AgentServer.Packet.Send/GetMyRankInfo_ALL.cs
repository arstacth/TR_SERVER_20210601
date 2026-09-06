using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetMyRankInfo_ALL : NetPacket
	{
		public GetMyRankInfo_ALL(eRequestRankKind rankKind, CRankListData rankData, byte last)
		{
			ns.WriteOP(Opcodes.eServer_RANK_MY_NICKNAME_ACK);
			ns.Write((rankData == null) ? 65 : 0);
			ns.Write((byte)rankKind);
			if (rankData != null)
			{
				if (rankKind == eRequestRankKind.eRequestRankKind_NORMAL)
				{
					ns.Write(rankData.m_ranking);
					ns.WriteBIG5Fixed_intSize(rankData.m_nickname);
					ns.Write(rankData.m_experienceValue);
					ns.Write(rankData.m_experienceValue2);
				}
				else
				{
					ns.Write(rankData.m_ranking);
					ns.WriteBIG5Fixed_intSize(rankData.m_nickname);
					ns.Write(rankData.m_experienceValue);
				}
			}
			ns.Write(last);
		}
	}
}
