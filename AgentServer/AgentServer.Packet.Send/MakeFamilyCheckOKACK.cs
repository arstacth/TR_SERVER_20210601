using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class MakeFamilyCheckOKACK : NetPacket
	{
		public MakeFamilyCheckOKACK(string name, bool isParents, byte last)
		{
			ns.WriteOP(Opcodes.eServer_FAMILY_CHECK_PROPOSE_CONDITION_ACK);
			ns.WriteBIG5Fixed_intSize(name);
			ns.Write(isParents);
			ns.Write(last);
		}
	}
}
