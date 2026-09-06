using LocalCommons.Network;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class ReloadFarmTypeNum : NetPacket
	{
		public ReloadFarmTypeNum(int FarmUniqueNum, int FarmTypeNum, byte last)
		{
			ns.WriteOP(Opcodes.eServer_FARM_ACK);
			ns.WriteOP(FarmProtocol.ReloadFarmMapInfo_ACK_3);
			ns.Write(0);
			ns.Write(FarmUniqueNum);
			ns.Write(FarmTypeNum);
			ns.Write(last);
		}
	}
}
