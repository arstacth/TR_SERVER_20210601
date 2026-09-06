using LocalCommons.Network;
using RoomServer.Structuring.Farm;
using RoomServer.Structuring.Opcode;

namespace RoomServer.Packet.Send
{
	public sealed class GetFarmCraftMapData : NetPacket
	{
		public GetFarmCraftMapData(int FarmUniqueNum, FarmCraftMapData farmcraftmapdata, byte last)
		{
			ns.WriteOP(RoomOpcodes.eServer_FARM_CRAFT_PROTOCOL);
			ns.Write(3);
			if (farmcraftmapdata.isFarmCraft)
			{
				ns.Write(0);
			}
			else
			{
				ns.Write(14);
			}
			if (farmcraftmapdata.isFarmCraft)
			{
				ns.Write(FarmUniqueNum);
				ns.Write((byte)0);
				ns.Write((byte)1);
				ns.Write(farmcraftmapdata.TotalBlock);
				ns.Write((ushort)farmcraftmapdata.CompressedData.Length);
				ns.Write(farmcraftmapdata.CompressedData, 0, farmcraftmapdata.CompressedData.Length);
			}
			ns.Write(last);
		}
	}
}
