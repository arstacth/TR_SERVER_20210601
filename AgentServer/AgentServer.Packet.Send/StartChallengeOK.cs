using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class StartChallengeOK : NetPacket
	{
		public StartChallengeOK(byte last)
		{
			ns.WriteOP(Opcodes.eServer_CHALLENGE_MAP_START_ACK);
			ns.Write(0);
			ns.Write(last);
		}
	}
}
