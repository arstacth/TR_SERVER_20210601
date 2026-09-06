using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class LevelUPNotice : NetPacket
	{
		public LevelUPNotice(string lvupstring, byte last)
		{
			ns.WriteOP(Opcodes.eServer_NOTICE_MSG_ACK);
			ns.Write(1);
			ns.Write(3);
			ns.WriteBIG5Fixed_intSize(lvupstring);
			ns.Write(last);
		}
	}
}
