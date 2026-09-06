using LocalCommons.Network;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class FarmCraft_ModifyFarmMapOK : NetPacket
	{
		public FarmCraft_ModifyFarmMapOK(byte last)
		{
			ns.WriteOP(RoomOpcodes.eServer_FARM_CRAFT_PROTOCOL);
			ns.Write(2);
			ns.Write(0);
			ns.Write(last);
		}
	}
}
