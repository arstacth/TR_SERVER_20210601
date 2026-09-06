using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.Shu;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_ExploreStartOK : NetPacket
	{
		public Shu_ExploreStartOK(ExploreInfo info, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(16);
			ns.Write(0);
			ns.Write(info.zoneNum);
			ns.Write(byte.MaxValue);
			ns.Write((short)1320);
			ns.Write(4960783);
			ns.Write(info.endDateTime);
			ns.Write(info.characterItemID);
			ns.Write(last);
		}
	}
}
