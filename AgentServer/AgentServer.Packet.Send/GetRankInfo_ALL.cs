using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetRankInfo_ALL : NetPacket
	{
		public GetRankInfo_ALL(eRequestRankKind rankKind, List<CRankListData> ranklist, byte last)
		{
			ns.WriteOP(Opcodes.eServer_RANK_ACK);
			ns.Write((byte)rankKind);
			ns.Write((byte)ranklist.Count);
			if (rankKind == eRequestRankKind.eRequestRankKind_NORMAL)
			{
				foreach (CRankListData item in ranklist)
				{
					ns.Write(item.m_ranking);
					ns.WriteBIG5Fixed_intSize(item.m_nickname);
					ns.Write(item.m_experienceValue);
					ns.Write(item.m_experienceValue2);
					ns.Write((item.m_ranking < 100) ? item.m_ranking : 0);
				}
			}
			else
			{
				foreach (CRankListData item2 in ranklist)
				{
					ns.Write(item2.m_ranking);
					ns.WriteBIG5Fixed_intSize(item2.m_nickname);
					ns.Write(item2.m_experienceValue);
					ns.Write(0);
				}
			}
			ns.Write(last);
		}
	}
}
