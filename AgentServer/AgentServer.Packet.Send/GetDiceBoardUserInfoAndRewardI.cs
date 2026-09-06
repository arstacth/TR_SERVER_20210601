using System.Collections.Generic;
using AgentServer.Structuring.Gacha;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetDiceBoardUserInfoAndRewardInfo_ACK : NetPacket
	{
		public GetDiceBoardUserInfoAndRewardInfo_ACK(int DiceBoardNum, int DiceBoardType, int Gauge, int CurrentPos, byte PauseRound, List<DiceBoardReward> DiceBoardRewardList, byte last)
		{
			ns.WriteOP(Opcodes.eServer_TALES_MARBLE_PROTOCOL);
			ns.Write(0);
			ns.Write(0);
			ns.Write(DiceBoardNum);
			ns.Write(Gauge);
			ns.Write(CurrentPos);
			ns.Write(0);
			ns.Write((byte)1);
			ns.Write((byte)0);
			ns.Write((byte)0);
			ns.Write((byte)0);
			ns.Write(DiceBoardType);
			ns.Write(PauseRound);
			ns.Write(DiceBoardRewardList.Count);
			foreach (DiceBoardReward DiceBoardReward in DiceBoardRewardList)
			{
				ns.Write(DiceBoardReward.Position);
				ns.Write(DiceBoardReward.RewardItem);
				ns.Write(3);
				ns.Write(DiceBoardReward.PositionType);
			}
			ns.Write(last);
		}
	}
}
