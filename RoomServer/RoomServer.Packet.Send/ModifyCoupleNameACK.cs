using LocalCommons.Network;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class ModifyCoupleNameACK : NetPacket
	{
		public ModifyCoupleNameACK(string name, byte last)
		{
			ns.WriteOP(Opcodes.eServer_COUPLE_MODIFY_COUPLE_NAME_ACK);
			ns.Write((byte)0);
			ns.WriteBIG5Fixed_intSize(name);
			ns.Write(last);
		}
	}
}
