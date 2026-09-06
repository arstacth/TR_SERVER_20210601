using AgentServer.Holders;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class SEASON_CHANNEL_SCHEDULE_ACK : NetPacket
	{
		public SEASON_CHANNEL_SCHEDULE_ACK(byte last)
		{
			ns.WriteOP(Opcodes.eServer_SEASON_CHANNEL_SCHEDULE_ACK);
			ns.Write(1);
			ns.Write(1);
			ns.WriteBIG5Fixed_intSize(ServerSettingHolder.ServerSettings.competitionEventUsingRoomKind);
			ns.WriteBIG5Fixed_intSize(ServerSettingHolder.ServerSettings.competitionEventUsingRoomKind);
			ns.WriteBIG5Fixed_intSize(string.Empty);
			ns.Write(last);
		}
	}
}
