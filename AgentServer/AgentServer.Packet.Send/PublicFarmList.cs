using System;
using System.Collections.Generic;
using System.Linq;
using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class PublicFarmList : NetPacket
	{
		public PublicFarmList(List<NormalRoom> publicfarmlist, int page, byte getcount, byte last)
		{
			IEnumerable<NormalRoom> enumerable = publicfarmlist.Skip(page * getcount).Take(getcount);
			int value = Convert.ToInt32(Math.Ceiling((double)publicfarmlist.Count / Convert.ToDouble(getcount))) - 1;
			ns.WriteOP(Opcodes.eServer_FARM_ACK);
			ns.WriteOP(FarmProtocol.PublicFarmList_ACK);
			ns.Write(1);
			ns.Write(value);
			ns.Write(page);
			ns.Write(enumerable.Count());
			foreach (NormalRoom item in enumerable)
			{
				ns.Write(item.FarmIndex);
				ns.Write(item.FarmTypeNum);
				ns.WriteBIG5Fixed_intSize(item.Name);
				ns.Write(item.PlayerCount);
				ns.Write(item.MaxPlayersCount);
				ns.Write(1);
				ns.Write((short)0);
				ns.Write(item.PlayerCount);
				ns.Write(item.FarmEXP);
				ns.Write(1);
				ns.WriteBIG5Fixed_intSize(item.FarmMasterName);
				ns.Write(item.ChatFarmType);
				ns.Write(item.MaxPlayersCount);
				ns.Write((byte)0);
				ns.WriteBIG5Fixed_intSize(item.Name);
				ns.Write((byte)0);
				ns.Write(item.PlayerName.Count);
				foreach (string item2 in item.PlayerName)
				{
					ns.Fill(3);
					ns.Write((byte)0);
					ns.Write(item.FarmMasterName == item2);
					ns.WriteBIG5Fixed_intSize(item2);
				}
				ns.Write((byte)1);
				ns.Write(item.hasFishingReward);
			}
			ns.Write(last);
		}
	}
}
