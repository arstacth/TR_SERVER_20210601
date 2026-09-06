using System.Collections.Generic;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class CompetitionEvent_PartyJoinOK : NetPacket
	{
		public CompetitionEvent_PartyJoinOK(CompetitionEventHandle.eCompetitionEventResult result, int joinPartyType, int SubPartyType, List<ExchangeItemInfo> exinfo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMPETITION_EVENT_PARTY_JOIN_ACK);
			ns.Write((int)result);
			if (result == CompetitionEventHandle.eCompetitionEventResult.eCompetitionEventResult_OK)
			{
				ns.Write(joinPartyType);
				ns.Write(SubPartyType);
				ns.Write(exinfo.Count);
				foreach (ExchangeItemInfo item in exinfo)
				{
					ns.Write(item.type);
					ns.Write(item.id);
					ns.Write(item.count);
					ns.Write(int.MaxValue);
				}
			}
			ns.Write(last);
		}
	}
}
