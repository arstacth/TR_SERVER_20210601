using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class Shu_UseItemFail : NetPacket
	{
		public Shu_UseItemFail(long shucharitemid, long shuitemid, int itemnum, int usecount, byte last)
		{
			ns.WriteOP(Opcodes.eServer_SHU_PROTOCOL);
			ns.Write(13);
			ns.Write(19);
			ns.Write(shucharitemid);
			ns.Write(shuitemid);
			ns.Write(itemnum);
			ns.Write(usecount);
			ns.Write(last);
		}
	}
}
