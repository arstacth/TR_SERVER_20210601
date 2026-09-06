using System.Collections.Generic;
using AgentServer.Structuring.Guild;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GuildGetListACK : NetPacket
	{
		public GuildGetListACK(List<GuildInfo> infos, int totalcount, short searchArgumentType, string searchtext, short limitLevel, short guildLevel, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(9);
			ns.Write(infos.Count);
			foreach (GuildInfo info in infos)
			{
				ns.Write((long)info.guildNum);
				ns.Write(info.memberCount);
				ns.Write(info.memberLimit);
				ns.Write((short)info.level);
				ns.Write(71516488);
				ns.Write(info.joinMethod);
				ns.Write(info.joinLimitLevelOver);
				ns.Write(info.joinLimitLevelBelow);
				ns.Write(info.foundationDate);
				ns.WriteBIG5Fixed_intSize(info.guildName);
				ns.WriteBIG5Fixed_intSize(info.masterName);
				ns.WriteBIG5Fixed_intSize(info.message);
				ns.Write(177239205);
			}
			ns.Write(totalcount);
			if (searchArgumentType == 0)
			{
				ns.WriteBIG5Fixed_intSize(searchtext);
				ns.Write(0);
			}
			else
			{
				ns.Write(0);
				ns.WriteBIG5Fixed_intSize(searchtext);
			}
			ns.Write(limitLevel);
			ns.Write(guildLevel);
			ns.Write(last);
		}
	}
}
