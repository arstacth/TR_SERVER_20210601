using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.User;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetMyCoupleRankInfo_ACK : NetPacket
	{
		public GetMyCoupleRankInfo_ACK(byte type, CoupleRankInfo i, byte last)
		{
			ns.WriteOP(Opcodes.eServer_RANK_MY_NICKNAME_ACK);
			ns.Write((i == null) ? 65 : 0);
			ns.Write(type);
			if (i != null)
			{
				ns.Write(i.coupleNum);
				ns.Write(i.point);
				ns.Write(i.rank);
				ns.Write(i.level);
				ns.WriteBIG5Fixed_intSize(i.femaleNickName);
				ns.WriteBIG5Fixed_intSize(i.maleNickName);
			}
			ns.Write(last);
		}
	}
}
