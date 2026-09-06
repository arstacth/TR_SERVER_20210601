using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class HuMongPickBoard_PickItem_Fail : NetPacket
	{
		public HuMongPickBoard_PickItem_Fail(eServerResult result, byte last)
		{
			ns.WriteOP(Opcodes.eServer_HUMONG_PICKBOARD_PICK_ACK);
			ns.Write((int)result);
			ns.Write(last);
		}
	}
}
