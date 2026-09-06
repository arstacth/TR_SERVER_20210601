using System;
using System.Collections.Generic;
using System.Linq;
using AgentServer.Holders;
using AgentServer.Network.Connections;
using AgentServer.Packet.RoomServer;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Room;
using LocalCommons.Network;
using LocalCommons.Utilities;
using NetMsg.Room;
using Serilog;

namespace AgentServer.Packet
{
	public class GameRoomHandle
	{
		public static void Handle_CreateGameRoom(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			if (currentAccount.InGame)
			{
				return;
			}
			reader.Offset += 8;
			reader.ReadLEInt32();
			int num = reader.ReadLEInt32();
			int fixedLength = reader.ReadLEInt16();
			string name = reader.ReadBig5StringSafe(fixedLength);
			int num2 = reader.ReadLEInt16();
			string password = string.Empty;
			if (num2 > 0)
			{
				password = reader.ReadBig5StringSafe(num2);
			}
			int num3 = reader.ReadLEInt32();
			reader.ReadLEInt32();
			int isTeamPlay = reader.ReadLEInt32();
			int itemType = reader.ReadLEInt32();
			bool isStepOn = reader.ReadBoolean();
			reader.ReadLEInt32();
			reader.ReadByte();
			if (num3 == 79 && (currentAccount.GuildNum <= 0 || currentAccount.GuildInfo == null))
			{
				Client.SendAsync(new GameRoom_CreateRoomError(9, last));
				return;
			}
			if (!RoomHolder.RoomKindInfos.TryGetValue(num3, out var value))
			{
				Client.SendAsync(new GameRoom_CreateRoomError(9, last));
				Log.Error("Invalid RoomKind ID:{0} userNum: {1}", num3, currentAccount.UserNum);
				return;
			}
			if (value.GameMode == 39)
			{
				if (!ServerSettingHolder.ServerSettings.useThankOfferingSystem)
				{
					Client.SendAsync(new GameRoom_CreateRoomError(9, last));
					return;
				}
				if (!ThankOfferingSystem.ThankOfferingSchedule.TryGetValue(ServerSettingHolder.ServerSettings.ThankOfferingSchedule_CurNum, out var value2))
				{
					Client.SendAsync(new GameRoom_CreateRoomError(9, last));
					return;
				}
				if (!(value2.StartTime <= DateTime.Now) || !(DateTime.Now <= value2.EndTime))
				{
					Client.SendAsync(new GameRoom_CreateRoomError(9, last));
					return;
				}
			}
			if (!Rooms.CheckRoomServer())
			{
				Client.SendAsync(new GameRoom_CreateRoomError(9, last));
				Log.Error("No room server connected!");
				return;
			}
			RoomSettings roomsetting = new RoomSettings
			{
				Name = name,
				Password = password,
				IsTeamPlay = isTeamPlay,
				ItemType = itemType,
				IsStepOn = isStepOn,
				MapNum = ((num <= 0) ? 1 : num),
				RoomKindID = num3,
				roomkindinfo = value
			};
			ServerStatus.ToRoomServer(new RM_SendPlayerInfo(currentAccount, roomsetting, 0, last), 0);
		}

		public static void Handle_LeaveRoom(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			NormalRoom room = Rooms.GetRoom(currentAccount.CurrentRoomId);
			try
			{
				if (currentAccount.InGame && room != null)
				{
					ServerStatus.ToRoomServer(new RM_PlayerLeaveRoom(currentAccount, isDisconnect: false, last), room.RoomServerID);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Player [{0}] error on leave room:\r\n{1}", currentAccount.NickName, ex.ToString());
			}
		}

		public static void Handle_RoomControl(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.Size - reader.Offset;
			byte[] array = new byte[num];
			Buffer.BlockCopy(reader.Buffer, reader.Offset, array, 0, num);
			ServerStatus.ToRoomServer(new RM_Packet
			{
				Session = currentAccount.Session,
				data = array
			}, currentAccount.RoomServerID);
		}

		public static void Handle_GameEndInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			float racedistance = reader.ReadLESingle();
			float mapMaxDistance = reader.ReadLESingle();
			short gameendtype = reader.ReadLEInt16();
			Utility.CurrentTimeMilliseconds();
			UserGameEndInfo info = new UserGameEndInfo
			{
				racedistance = racedistance,
				MapMaxDistance = mapMaxDistance,
				gameendtype = gameendtype
			};
			ServerStatus.ToRoomServer(new RM_GameEndInfo(currentAccount, info, last), currentAccount.RoomServerID);
		}

		public static void Handle_PlayerList(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			NormalRoom room = Rooms.GetRoom(currentAccount.CurrentRoomId);
			if (room != null)
			{
				ServerStatus.ToRoomServer(new RM_GetPlayerPosList(currentAccount, last), room.RoomServerID);
			}
		}

		public static void Handle_GetRoomList(ClientConnection Client, PacketReader reader, byte last)
		{
			Account User = Client.CurrentAccount;
			int roomkindid = reader.ReadLEInt32();
			reader.ReadLEInt32();
			int page = reader.ReadLEInt32();
			byte getCount = reader.ReadByte();
			List<NormalRoom> list = Rooms.RoomList.Values.Where((NormalRoom room) => room.RoomKindID == roomkindid && room.PlayerCount > 0).ToList();
			if (roomkindid == 79)
			{
				list = list.Where((NormalRoom room) => room.guildName != User.GuildInfo.guildName).ToList();
			}
			Client.SendAsync(new GameRoom_GetRoomList(list, roomkindid, page, getCount, last));
		}

		public static void Handle_EnterRoom(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int roomsession = reader.ReadLEInt32();
			int num = reader.ReadLEInt16();
			string pw = string.Empty;
			if (num > 0)
			{
				pw = reader.ReadBig5StringSafe(num);
			}
			NormalRoom room = Rooms.GetRoom(roomsession);
			if (room != null)
			{
				ServerStatus.ToRoomServer(new RM_PlayerEnterRoom(currentAccount, pw, room.ID, room.RoomKindID, 0, 0, last), room.RoomServerID);
			}
			else
			{
				Client.SendAsync(new GameRoom_EnterRoomError(1, 0, last));
			}
		}

		public static void Handle_EnterRoomForGuild(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int guildmatchroomid = reader.ReadLEInt32();
			int roomsession = reader.ReadLEInt32();
			reader.ReadLEInt32();
			int fixedLength = reader.ReadLEInt16();
			string pw = reader.ReadBig5StringSafe(fixedLength);
			NormalRoom room = Rooms.GetRoom(roomsession);
			if (room != null)
			{
				ServerStatus.ToRoomServer(new RM_PlayerEnterRoom(currentAccount, pw, room.ID, room.RoomKindID, 0, guildmatchroomid, last), room.RoomServerID);
			}
			else
			{
				Client.SendAsync(new GameRoom_EnterRoomError(1, 155, last));
			}
		}

		public static void Handle_KickPlayer(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int fixedLength = reader.ReadLEInt16();
			string kickedplayername = reader.ReadBig5StringSafe(fixedLength);
			NormalRoom room = Rooms.GetRoom(currentAccount.CurrentRoomId);
			if (room != null)
			{
				ServerStatus.ToRoomServer(new RM_KickPlayer(currentAccount, kickedplayername, last), room.RoomServerID);
			}
		}

		public static void Handle_RandomEnterRoom(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int roomkindid = reader.ReadLEInt32();
			reader.ReadByte();
			reader.ReadByte();
			reader.ReadByte();
			int mapnum = reader.ReadLEInt32();
			reader.ReadLEInt32();
			IEnumerable<NormalRoom> source = Rooms.RoomList.Values.Where((NormalRoom rm) => rm.RoomKindID == roomkindid && rm.PlayerCount < rm.SlotCount && !rm.isPlaying && !rm.HasPassword && !rm.hasAfreecaTV);
			if (RoomHolder.RoomKindInfos.TryGetValue(roomkindid, out var value))
			{
				if (value.Channel == 46 || value.Channel == 47)
				{
					int num = currentAccount.PartyType & 1;
					if (RoomHolder.RoomKindPlayerNum.TryGetValue(roomkindid, out var value2))
					{
						int teammaxcount = value2.MaxUser / 2;
						switch (num)
						{
						case 1:
							source = Rooms.RoomList.Values.Where((NormalRoom rm) => rm.RoomKindID == roomkindid && rm.RedTeamCount < teammaxcount && !rm.isPlaying && !rm.HasPassword && !rm.hasAfreecaTV);
							break;
						case 0:
							source = Rooms.RoomList.Values.Where((NormalRoom rm) => rm.RoomKindID == roomkindid && rm.BlueTeamCount < teammaxcount && !rm.isPlaying && !rm.HasPassword && !rm.hasAfreecaTV);
							break;
						}
					}
				}
				if (mapnum > 0)
				{
					source = source.Where((NormalRoom w) => w.MapNum == mapnum);
				}
			}
			if (source.Count() == 0)
			{
				Client.SendAsync(new GameRoom_RandomEnterRoomError(roomkindid, last));
				return;
			}
			NormalRoom normalRoom = source.OrderBy((NormalRoom _) => Guid.NewGuid()).FirstOrDefault();
			ServerStatus.ToRoomServer(new RM_PlayerEnterRoom(currentAccount, string.Empty, normalRoom.ID, normalRoom.RoomKindID, 0, 0, last), normalRoom.RoomServerID);
		}

		public static void Handle_PlayTogether(ClientConnection Client, PacketReader packet, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			packet.ReadLEInt32();
			int roomsession = packet.ReadLEInt32();
			int num = packet.ReadLEInt32();
			int num2 = packet.ReadLEInt16();
			string pw = string.Empty;
			if (num2 > 0)
			{
				pw = packet.ReadBig5StringSafe(num2);
			}
			if (!Rooms.ExistRoom(roomsession))
			{
				Client.SendAsync(new GameRoom_EnterRoomError(1, num, last));
				return;
			}
			if (RoomHolder.RoomKindInfos.TryGetValue(num, out var value) && (value.Channel == 46 || value.Channel == 47))
			{
				Client.SendAsync(new GameRoom_EnterRoomError(1, num, last));
				return;
			}
			NormalRoom room = Rooms.GetRoom(roomsession);
			if (RoomHolder.RoomKindInfos.TryGetValue(room.RoomKindID, out var value2) && (value2.Channel == 46 || value2.Channel == 47))
			{
				Client.SendAsync(new GameRoom_EnterRoomError(1, num, last));
			}
			else
			{
				ServerStatus.ToRoomServer(new RM_PlayerEnterRoom(currentAccount, pw, room.ID, room.RoomKindID, 0, 0, last), room.RoomServerID);
			}
		}

		public static void Handle_FF3E02(ClientConnection Client, PacketReader reader, byte last)
		{
			_ = Client.CurrentAccount;
			int unk = reader.ReadLEInt32();
			int unk2 = reader.ReadLEInt32();
			reader.ReadLEInt32();
			int unk3 = reader.ReadLEInt32();
			int unk4 = reader.ReadLEInt32();
			Client.SendAsync(new GameRoom_FF3F02(unk, unk2, unk3, unk4, last));
		}

		public static void Handle_GetRoomKindAttr(ClientConnection Client, PacketReader reader, byte last)
		{
			int num = reader.ReadLEInt32();
			if (RoomHolder.RoomKindAttr.TryGetValue(num, out var value))
			{
				Client.SendAsync(new GetRoomKindAttr_ACK(flag: true, num, value, last));
			}
			else
			{
				Client.SendAsync(new GetRoomKindAttr_ACK(flag: false, num, null, last));
			}
		}

		public static void Handle_GetVertification(ClientConnection Client, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			currentAccount.VertificationCode = (byte)new Random(Guid.NewGuid().GetHashCode()).Next(0, 100);
			currentAccount.NeedVertificated = true;
			Client.SendAsync(new GameRoom_GetVertificationInfo(currentAccount, last));
		}

		public static void Handle_PassVertification(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			byte code = reader.ReadByte();
			ServerStatus.ToRoomServer(new RM_PassVertification(currentAccount, code, last), currentAccount.RoomServerID);
		}

		public static void Handle_JoinGuildMatch(ClientConnection Client, byte last)
		{
			_ = Client.CurrentAccount;
			Client.SendAsync(new GameRoom_JoinGuildMatch(last));
		}
	}
}
