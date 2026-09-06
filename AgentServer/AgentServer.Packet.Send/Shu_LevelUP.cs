using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_LevelUP : NetPacket
	{
		public Shu_LevelUP(long shucharitemid, int beforelv, int afterlv, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(20);
			ns.Write(0);
			ns.Write(beforelv);
			ns.Write(afterlv);
			ns.Write(shucharitemid);
			ns.Write(last);
		}
	}
}
