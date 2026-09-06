using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetFarmPortalInfoOK_Ack : NetPacket
	{
		public GetFarmPortalInfoOK_Ack(long OID, int FarmUniqueNum, string nickname, string memo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_FARM_ACK);
			ns.WriteOP(FarmProtocol.GetFarmPortalInfo_ACK);
			ns.Write(OID);
			ns.Write(FarmUniqueNum);
			ns.WriteBIG5Fixed_intSize(nickname);
			ns.WriteBIG5Fixed_intSize(memo);
			ns.Write(last);
		}
	}
}
