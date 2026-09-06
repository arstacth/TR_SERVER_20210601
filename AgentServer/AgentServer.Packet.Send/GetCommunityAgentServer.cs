using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GetCommunityAgentServer : NetPacket
	{
		public GetCommunityAgentServer(byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMMUNITY_SERVER_PROTOCOL);
			ns.WriteOP(1);
			ns.Write((byte)1);
			ns.WriteBIG5Fixed_intSize(Conf.ServerIP);
			ns.Write(Conf.CommunityAgentServerPort);
			ns.Write(last);
		}
	}
}
