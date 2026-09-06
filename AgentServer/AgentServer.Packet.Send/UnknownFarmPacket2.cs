using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class UnknownFarmPacket2 : NetPacket
	{
		public UnknownFarmPacket2(byte last)
		{
			ns.WriteOP(Opcodes.eServer_FARM_ACK);
			ns.WriteOP(FarmProtocol.FarmProtocol_UNK2_ACK);
			ns.Write(0);
			ns.Write(last);
		}
	}
}
