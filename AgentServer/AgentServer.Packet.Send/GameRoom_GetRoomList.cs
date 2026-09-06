using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;

namespace AgentServer.Packet.Send
{
	public sealed class GameRoom_GetRoomList : NetPacket
	{
		public GameRoom_GetRoomList(List<NormalRoom> rooms, int roomkindid, int page, byte getCount, byte last)
		{
			int num = ((roomkindid == 79) ? 6 : getCount);
			IEnumerable<NormalRoom> enumerable = rooms.Skip(page * num).Take(num);
			int value = Convert.ToInt32(Math.Ceiling((double)rooms.Count / Convert.ToDouble(num))) - 1;
			ns.WriteOP(Opcodes.eServer_ROOM_LIST_ACK);
			ns.Write(roomkindid);
			ns.Write(value);
			ns.Write(page);
			ns.Write((short)enumerable.Count());
			foreach (NormalRoom item in enumerable)
			{
				ns.Write(item.ID);
				ns.WriteBIG5Fixed_intSize(item.Name);
				ns.Write(!item.HasPassword);
				ns.Write(item.PlayerCount);
				ns.Write(item.SlotCount);
				ns.Write(!item.isPlaying);
				ns.Write(item.IsStepOn);
				ns.Write(item.ItemType);
				ns.Write(item.MapNum);
				ns.Write(6);
				ns.Write((item.IsTeamPlay == 2) ? ((byte)1) : ((byte)0));
				ns.Write(item.hasAfreecaTV);
				ns.Write(item.hasPiero);
				ns.Write(item.GMItem);
				ns.Write((byte)0);
				ns.Write((byte)0);
				ns.Write((byte)1);
				ns.Write(-1);
				ns.Write(item.BuffType);
				ns.Fill(3);
				ns.Write(item.ItemNum);
				ns.Write(item.BonusStageLevel);
				if (roomkindid == 79)
				{
					ns.WriteBIG5Fixed_intSize(item.guildName);
					ns.Write(0);
					ns.Write(item.GuildMatchRoomID);
					ns.Write(1);
				}
				ns.Write((byte)0);
			}
			ns.Seek(ns.Position - 1, SeekOrigin.Begin);
			ns.Write(last);
		}
	}
}
