using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class SingleChallengeAction : NetPacket
	{
		public SingleChallengeAction(int ActionType, byte last)
		{
			ns.WriteOP(Opcodes.eServer_CHALLENGE_MAP_END_ACK);
			ns.Write(ActionType);
			ns.Write(0);
			ns.Write(int.MaxValue);
			ns.Write(0);
			ns.Write(int.MaxValue);
			ns.Write(0);
			ns.Write(last);
		}
	}
}
