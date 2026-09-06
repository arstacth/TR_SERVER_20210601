using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class CreatePublicFarmFail : NetPacket
	{
		public CreatePublicFarmFail(byte last)
		{
			ns.WriteOP(Opcodes.eServer_ENTER_ROOM_ACK);
			ns.Write(67);
			ns.Write(46);
			ns.Write(75);
			ns.Write(0L);
			ns.Write(last);
		}
	}
}
