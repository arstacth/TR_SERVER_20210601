using LocalCommons.Network;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class FishingPacket : NetPacket
	{
		public FishingPacket(byte action, byte last)
		{
			ns.WriteOP(Opcodes.eServer_FISHING_PROC_FISHING_ACK);
			ns.Write(0);
			ns.Write(action);
			ns.Write(last);
		}
	}
}
