using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Holders
{
	public sealed class ESTIMATED_REMAIN_TIME_FOR_LOGIN_ACK : NetPacket
	{
		public ESTIMATED_REMAIN_TIME_FOR_LOGIN_ACK(int waitUserNum, int totalWaitLoginNum, byte last)
		{
			ns.WriteOP(Opcodes.eServer_ESTIMATED_REMAIN_TIME_FOR_LOGIN_ACK);
			ns.Write(waitUserNum);
			ns.Write(1);
			ns.Write(last);
		}
	}
}
