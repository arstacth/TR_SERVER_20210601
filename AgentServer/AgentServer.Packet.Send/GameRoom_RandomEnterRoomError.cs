using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GameRoom_RandomEnterRoomError : NetPacket
	{
		public GameRoom_RandomEnterRoomError(int roomkindid, byte last)
		{
			ns.WriteOP(Opcodes.eServer_ENTER_ROOM_ACK);
			ns.Write(67);
			ns.Write(11);
			ns.Write(roomkindid);
			ns.Write(0L);
			ns.Write(last);
		}
	}
}
