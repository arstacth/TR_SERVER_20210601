using AgentServer.Structuring.Guild;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GuildInFoACK : NetPacket
	{
		public GuildInFoACK(bool isSelf, GuildInfo info, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GUILD_OPERATION_REQ);
			ns.WriteOP(50);
			ns.Write(isSelf);
			ns.Write(info.guildNum);
			ns.Write(0);
			ns.Write(info.memberCount);
			ns.Write(info.memberLimit);
			ns.Write((short)info.level);
			ns.Write((int)info.kind);
			ns.Write(info.joinMethod);
			ns.Write(info.joinLimitLevelOver);
			ns.Write(info.joinLimitLevelBelow);
			ns.Write(info.foundationDate);
			ns.WriteBIG5Fixed_intSize(info.guildName);
			ns.WriteBIG5Fixed_intSize(info.masterName);
			ns.WriteBIG5Fixed_intSize(info.message);
			ns.Write((short)0);
			ns.Write((short)0);
			ns.Write(info.exp);
			ns.Write(info.nextLevelExp);
			ns.Write(info.point);
			ns.Write(info.ladderPoint);
			ns.Write((short)info.attendanceCount);
			ns.Write((short)0);
			ns.Write(info.SkillPoint);
			ns.Write(info.SkillInfos.Count);
			foreach (GuildSkillInfo skillInfo in info.SkillInfos)
			{
				ns.Write(skillInfo.SkillNum);
				ns.Write(skillInfo.SkillLevel);
			}
			ns.Write(last);
		}
	}
}
