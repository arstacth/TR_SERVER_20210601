using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class SinglePlay : NetPacket
	{
		public SinglePlay(int mapnum, byte last)
		{
			ns.WriteOP(Opcodes.eServer_ALONERUN_START_GAME_ACK);
			ns.Write(0);
			ns.Write(mapnum);
			ns.Write(last);
		}
	}
}
