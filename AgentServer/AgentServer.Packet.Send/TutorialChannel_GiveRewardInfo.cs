using System.Collections.Generic;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class TutorialChannel_GiveRewardInfo : NetPacket
	{
		public TutorialChannel_GiveRewardInfo(List<ExchangeItemInfo> exinfo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_TUTORIAL_CHANNEL_GIVE_REWARD_ACK);
			ns.Write(0);
			ns.Write(exinfo.Count);
			foreach (ExchangeItemInfo item in exinfo)
			{
				ns.Write(item.type);
				ns.Write(item.id);
				ns.Write(item.count);
				ns.Write(int.MaxValue);
			}
			ns.Write(last);
		}
	}
}
