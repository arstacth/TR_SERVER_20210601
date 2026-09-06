using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Park;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetHuMongPickBoardInfo : NetPacket
	{
		public GetHuMongPickBoardInfo(HuMongPickBoardData PickBoardData, byte last)
		{
			ns.WriteOP(Opcodes.eServer_HUMONG_PICKBOARD_STATE_ACK);
			ns.Write(0);
			ns.Write(PickBoardData.HuMongPickBoardNum);
			ns.Write(PickBoardData.PickInfo.Length);
			for (short num = 1; num <= PickBoardData.PickInfo.Length; num = (short)(num + 1))
			{
				ns.Write(num);
				ns.Write(PickBoardData.PickInfo[num - 1]);
			}
			ns.Write(last);
		}

		public GetHuMongPickBoardInfo(eServerResult result, byte last)
		{
			ns.WriteOP(Opcodes.eServer_HUMONG_PICKBOARD_STATE_ACK);
			ns.Write((int)result);
			ns.Write(last);
		}
	}
}
