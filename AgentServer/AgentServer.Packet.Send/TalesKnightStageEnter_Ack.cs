using System.Collections.Generic;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.TalesKnight;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class TalesKnightStageEnter_Ack : NetPacket
	{
		public TalesKnightStageEnter_Ack(List<TalesKnightsGroupName> Infos, byte last)
		{
			ns.WriteOP(Opcodes.eServer_TALESKNIGHT_ENTER_ADVENTURE_ACK);
			ns.Write(Infos.Count);
			foreach (TalesKnightsGroupName Info in Infos)
			{
				ns.WriteBIG5Fixed_intSize(Info.OrderName);
				ns.WriteBIG5Fixed_intSize(Info.Quests);
				ns.Write(Info.StageGroupNum);
				ns.Write(Info.CurStageNum);
				ns.Write(Info.RewardTime);
				ns.Write(Info.AccTime);
				ns.Write(Info.OrderNumber);
			}
			ns.Write(last);
		}
	}
}
