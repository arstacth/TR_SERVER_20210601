using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class CompetitionEvent_PartyUserInfo : NetPacket
	{
		public CompetitionEvent_PartyUserInfo(int PartyType, int SubPartyType, List<CompetitionPartyUserInfo> CompetitionPartyInfo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMPETITION_EVENT_PARTY_USER_INFO_ACK);
			ns.Write(0);
			ns.Write(PartyType);
			ns.Write(SubPartyType);
			ns.Write(CompetitionPartyInfo.Count);
			foreach (CompetitionPartyUserInfo item in CompetitionPartyInfo)
			{
				ns.Write(item.eventType);
				ns.Write(item.point);
				ns.Write(item.accPoint);
				ns.Write(item.rewardLevel);
				ns.Write(item.ReceivedSeasonPassRewardLevel);
			}
			ns.Write(last);
		}
	}
}
