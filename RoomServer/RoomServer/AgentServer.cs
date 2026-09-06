using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using LocalCommons.Network;
using LocalCommons.Utilities;
using NetMsg.LBS;
using NetMsg.Room;
using RoomServer.Holders;
using RoomServer.Packet;
using RoomServer.Structuring;
using Serilog;

namespace RoomServer
{
	public class AgentServer : ReceiveActor
	{
		public static Dictionary<int, IActorRef> AgentServerList = new Dictionary<int, IActorRef>();

		private static int ID = 1;

		public static ConcurrentDictionary<int, Account> CurrentAccounts { get; } = new ConcurrentDictionary<int, Account>();


		public AgentServer()
		{
			Receive<AgentConnectRequest>(delegate
			{
				int num = ID++;
				Log.Information("{0} AgentServer Connected!", num);
				AgentServerList.Add(num, base.Sender);
				Form1.UpdateLableStatic(num);
				base.Sender.Tell(new AgentConnectResponse
				{
					AgentID = num,
					ConnectedRoomID = ServerStatus.MyRoomServerID
				}, base.Self);
			});
			Receive(delegate(RM_Packet req)
			{
				Handle_RoomPacket(req.Session, req.data);
			});
			Receive(delegate(ReloadSetting re)
			{
				ReloadHandle(re);
			});
			Receive(delegate(TowerEventInfo re)
			{
				TowerEventHolder.TowerEventStatus(re.Type);
			});
			Receive(delegate(byte[] packet)
			{
				byte[] array = new byte[packet.Length];
				Buffer.BlockCopy(packet, 0, array, 0, packet.Length);
				HandlePacket(base.Sender, array);
			});
		}

		private void ReloadHandle(ReloadSetting re)
		{
			switch (re.Code)
			{
			case 5:
				MapHolder.LoadMapInfo();
				MapHolder.LoadMapRoomKind();
				break;
			case 6:
				GameRewardHolder.LoadGameRewardInfo();
				break;
			case 8:
				ServerSettingHolder.LoadServerSettingInfo();
				break;
			case 7:
				break;
			}
		}

		private void HandlePacket(IActorRef Sender, byte[] data)
		{
			PacketReader packetReader = new PacketReader(data, 0);
			short num = packetReader.ReadLEInt16();
			byte last = packetReader.Buffer.LastOrDefault();
			switch ((byte)num)
			{
			case 1:
				GameRoomHandle.Handle_CreateGameRoom(Sender, packetReader, last);
				break;
			case 4:
				GameRoomHandle.Handle_LeaveRoom(packetReader, last);
				break;
			case 6:
				GameRoomHandle.Handle_EnterRoom(Sender, packetReader, last);
				break;
			case 8:
				GameRoomHandle.Handle_KickPlayer(packetReader, last);
				break;
			case 9:
				GameRoomHandle.Handle_GameEndInfo(packetReader, last);
				break;
			case 10:
				GameRoomHandle.Handle_PlayerList(packetReader, last);
				break;
			case 11:
				FarmHandle.Handle_EnterFarm(Sender, packetReader, last);
				break;
			case 15:
				FarmHandle.Handle_CreatePublicFarm(Sender, packetReader, last);
				break;
			case 16:
				FarmHandle.Handle_ReloadFarmMapInfo(packetReader, last);
				break;
			case 17:
				FarmHandle.Handle_ClearUserFarmMapInfo(packetReader, last);
				break;
			case 18:
				FarmHandle.Handle_ModifyFarmMapInfo(packetReader, last);
				break;
			case 19:
				FarmHandle.Handle_IncreaseAnimalSize(packetReader, last);
				break;
			case 20:
				FarmHandle.Handle_RestoreAnimalDefaultSize(packetReader, last);
				break;
			case 21:
				FarmHandle.Handle_ChangeFarmSkybox(packetReader, last);
				break;
			case 22:
				Handle_Agent_To_RoomUser(packetReader);
				break;
			case 38:
				Handle_Agent_To_RoomUser_Me(packetReader);
				break;
			case 23:
				FarmHandle.Handle_ChangeFarmMapTypeBySlot(packetReader, last);
				break;
			case 29:
				GameRoomHandle.Handle_UpdateGuild(packetReader, last);
				break;
			case 30:
				GuildMatchHandle.Handle_LookingForGuildMatch(packetReader, last);
				break;
			case 31:
				GuildMatchHandle.Handle_PickGuildForMatch(packetReader, last);
				break;
			case 32:
				GuildMatchHandle.Handle_CancelLookingForGuildMatch(packetReader, last);
				break;
			case 33:
				GuildMatchHandle.Handle_ChooseGuildForMatch(packetReader, last);
				break;
			case 34:
				GameRoomHandle.Handle_PassVertification(packetReader, last);
				break;
			case 36:
				FishingHandle.Handle_UpdateFarmFishingReward(packetReader, last);
				break;
			}
			packetReader.Offset = 2;
			if (num > 10000 && num == 10017)
			{
				RoomAgentProtocol_WRAP_ROOM_REQ(packetReader, last);
			}
		}

		private void Handle_RoomPacket(int Session, byte[] data)
		{
			PacketReader packetReader = new PacketReader(data, 0);
			short num = packetReader.ReadLEInt16();
			byte last = packetReader.Buffer.LastOrDefault();
			if (!CurrentAccounts.TryGetValue(Session, out var value))
			{
				return;
			}
			switch (num)
			{
			case 483:
				RoomServerHandle.Handle_SlotControl(value, packetReader, last);
				break;
			case 247:
				RoomServerHandle.Handle_ChangeSetting(value, packetReader, last);
				break;
			case 1278:
				RoomServerHandle.Handle_Ready(value, packetReader, last);
				break;
			case 142:
				RoomServerHandle.Handle_ChangeMap(value, packetReader, last);
				break;
			case 1115:
				RoomServerHandle.Handle_StartGame(value, packetReader, last);
				break;
			case 420:
				RoomServerHandle.Handle_ChangeStatus(value, packetReader, last);
				break;
			case 1548:
				RoomServerHandle.Handle_StartLoading(value, packetReader, last);
				break;
			case 1224:
				RoomServerHandle.Handle_EndLoading(value, packetReader, last);
				break;
			case 927:
				RoomServerHandle.Handle_GameStart(value, packetReader, last);
				break;
			case 1378:
				RoomServerHandle.Handle_GoalInData(value, packetReader, last);
				break;
			case 214:
				GameModeHandle.GameMode_TimeOver(value, packetReader, last);
				break;
			case 1588:
				RoomServerHandle.Handle_RoomChat(value, packetReader, last);
				break;
			case 934:
				RoomServerHandle.Handle_MapControl(value, packetReader, last);
				break;
			case 74:
				GameModeHandle.GameMode_LapTimeCountdwon(value, packetReader, last);
				break;
			case 1677:
				RoomServerHandle.Handle_TriggerMapEvent(value, packetReader, last);
				break;
			case 1555:
				RoomServerHandle.Handle_StepOnButton(value, packetReader, last);
				break;
			case 1227:
				RoomServerHandle.Handle_RegisterItem(value, packetReader, last);
				break;
			case 1549:
				GameModeHandle.GameMode_MiniGame_Respawn(value, packetReader, last);
				break;
			case 1607:
				GameModeHandle.GameMode_MiniGame_GetPoint(value, packetReader, last);
				break;
			case 718:
				GameModeHandle.GameMode_MiniGame_RoundTime(value, packetReader, last);
				break;
			case 967:
				GameModeHandle.GameMode_GameOver(value, packetReader, last);
				break;
			case 799:
				GameModeHandle.GameMode_FootStep_GoalIn(value, packetReader, last);
				break;
			case 1220:
				GameModeHandle.GameMode_Amsan_LapTime(value, packetReader, last);
				break;
			case 1390:
				GameModeHandle.GameMode_Amsan_StepButton(value, packetReader, last);
				break;
			case 408:
				GameModeHandle.GameMode_Amsan_StepButton_Push(value, packetReader, last);
				break;
			case 908:
				GameModeHandle.GameMode_Amsan_FinalButton(value, packetReader, last);
				break;
			case 981:
				GameModeHandle.GameMode_Amsan_LapTimeControl(value, packetReader, last);
				break;
			case 572:
				GameModeHandle.GameMode_RandomGameOver(value, packetReader, last);
				break;
			case 424:
				GameModeHandle.GameMode_RandomGameOver_Die(value, packetReader, last);
				break;
			case 882:
				RoomServerHandle.Handle_GiveUpItem(value, packetReader, last);
				break;
			case 73:
				RoomServerHandle.Handle_DrawItem(value, packetReader, last);
				break;
			case 1092:
				RoomServerHandle.Handle_UseItem(value, packetReader, last);
				break;
			case 1103:
				RoomServerHandle.Handle_RegItem2(value, packetReader, last);
				break;
			case 1713:
				RoomServerHandle.Handle_RegItem(value, packetReader, last);
				break;
			case 135:
				RoomServerHandle.Handle_ChangeTeam(value, packetReader, last);
				break;
			case 244:
				RoomServerHandle.Handle_ChangeRelayTeam(value, packetReader, last);
				break;
			case 71:
				RoomServerHandle.Handle_RandomChooseRelayTeam(value, last);
				break;
			case 604:
				RoomServerHandle.Handle_ChangeSlotStateRelay(value, packetReader, last);
				break;
			case 1693:
				RoomServerHandle.Handle_WaitPassBaton(value, last);
				break;
			case 1329:
				RoomServerHandle.Handle_WaitPassBaton2(value, last);
				break;
			case 194:
				RoomServerHandle.Handle_StartPassBaton(value, packetReader, last);
				break;
			case 434:
				RoomServerHandle.Handle_PassBaton(value, packetReader, last);
				break;
			case 900:
				GameModeHandle.GameMode_CatchFish(value, packetReader, last);
				break;
			case 273:
				GameModeHandle.RunQuizMode_RequestQuizList(value, last);
				break;
			case 438:
				GameModeHandle.GameMode_TurtleEatItem(value, packetReader, last);
				break;
			case 1380:
				GameModeHandle.GameMode_ReqChangeTeamLeader(value, last);
				break;
			case 715:
				GameModeHandle.CorunMode_TriggerObjectEvent(value, packetReader, last);
				break;
			case 1138:
				GameModeHandle.CorunMode_TriggerCheckInObjectEvent(value, packetReader, last);
				break;
			case 1498:
				GameModeHandle.CorunMode_SetClearLimitTime(value, packetReader, last);
				break;
			case 399:
				GameModeHandle.CorunMode_EnterTimeSection(value, packetReader, last);
				break;
			case 1116:
				GameModeHandle.CorunMode_ClearTimeSection(value, packetReader, last);
				break;
			case 1655:
				GameModeHandle.CorunMode_SetBossEnergy(value, packetReader, last);
				break;
			case 421:
				GameModeHandle.CorunMode_DecreaseBossEnergy(value, packetReader, last);
				break;
			case 984:
				GameModeHandle.CorunMode_SetObjectBossEnergy(value, packetReader, last);
				break;
			case 1077:
				GameModeHandle.CorunMode_DecreaseObjectBossEnergy(value, packetReader, last);
				break;
			case 1475:
				GameModeHandle.CorunMode_IncreaseObjectBossEnergy(value, packetReader, last);
				break;
			case 805:
				AssaultModeHandle.AssaultMode_SetObjectInfo(value, packetReader, last);
				break;
			case 558:
				AssaultModeHandle.AssaultMode_SetCharacterEnergy(value, last);
				break;
			case 1540:
				AssaultModeHandle.AssaultMode_DecreaseCharacterEnergy(value, packetReader, last);
				break;
			case 374:
				AssaultModeHandle.AssaultMode_ChargeCharacterEnergy(value, packetReader, last);
				break;
			case 1333:
				AssaultModeHandle.AssaultMode_DecreaseObjectEnergy(value, packetReader, last);
				break;
			case 50:
				AssaultModeHandle.AssaultMode_BounsItemMake(value, packetReader, last);
				break;
			case 1671:
				AssaultModeHandle.AssaultMode_BounsItemEat(value, packetReader, last);
				break;
			case 444:
				AssaultModeHandle.AssaultMode_Rebirth(value, packetReader, last);
				break;
			case 936:
				AssaultModeHandle.Handle_InitMapBonusItem(value, packetReader, last);
				break;
			case 947:
				AssaultModeHandle.Handle_MapBonusItemEat(value, packetReader, last);
				break;
			case 1248:
				RoomServerHandle.Handle_setUserState(value, packetReader, last);
				break;
			case 1802:
			{
				int num2 = packetReader.ReadLEInt32();
				switch (num2)
				{
				case 2:
					FarmRoomHandle.Handle_FarmCraft_ModifyFarmMapInfo(value, packetReader, last);
					break;
				case 5:
					FarmRoomHandle.Handle_ReloadMapInfo(value, packetReader, last);
					break;
				case 6:
					FarmRoomHandle.Handle_SaveUserFarmSlotInfo(value, packetReader, last);
					break;
				case 7:
					FarmRoomHandle.Handle_ChangeFarmMapTypeBySlot_New(value, packetReader, last);
					break;
				case 8:
					FarmRoomHandle.Handle_ChangeFarmTypeByItem_New(value, packetReader, last);
					break;
				default:
					Log.Information("farm room opcode: {0}, {1}", num2, Utility.ByteArrayToString(packetReader.Buffer));
					break;
				}
				break;
			}
			case 1176:
				FarmRoomHandle.Handle_FarmAction(value, packetReader, last);
				break;
			case 159:
				FarmRoomHandle.Handle_ChangeFarmRoomName(value, packetReader, last);
				break;
			case 493:
				FarmRoomHandle.Handle_ChangeFarmRoomPassword(value, packetReader, last);
				break;
			case 562:
				FarmRoomHandle.Handle_PublicFarmRoom(value, packetReader, last);
				break;
			case 739:
				CoupleHandle.Handle_WeddingSetItem(value, packetReader, last);
				break;
			case 56:
				CoupleHandle.Handle_WeddingReady(value, last);
				break;
			case 1824:
				GameModeHandle.SubjectKing_GetQuestion(value, last);
				break;
			case 1837:
				GameModeHandle.ItemRacing_GetAbility(value, packetReader, last);
				break;
			case 1839:
				GameModeHandle.ItemRacing_UseAbility(value, packetReader, last);
				break;
			case 1841:
				GameModeHandle.ItemRacing_RemoveAbility(value, packetReader, last);
				break;
			case 972:
				RoomServerHandle.Handle_ChangeGuildMatchRoomName(value, packetReader, last);
				break;
			case 609:
				RoomServerHandle.Handle_StartGuildMatching(value, packetReader, last);
				break;
			case 1267:
				RoomServerHandle.Handle_CancelGuildMatching(value, packetReader, last);
				break;
			case 674:
				RoomServerHandle.Handle_ProcessInviteForGuildMatch(value, packetReader, last);
				break;
			case 1082:
				RoomServerHandle.Handle_MapGenerateItem(value, packetReader, last);
				break;
			case 1036:
				RoomServerHandle.Handle_PickMapItem(value, packetReader, last);
				break;
			case 325:
				RoomServerHandle.Handle_GiveUpMapItem(value, packetReader, last);
				break;
			case 1853:
				GameModeHandle.TypingRun_ReqText(value, packetReader, last);
				break;
			case 1855:
				GameModeHandle.TypingRun_EnterText(value, packetReader, last);
				break;
			case 761:
				GameModeHandle.GetBonusStage_RewardList(value, packetReader, last);
				break;
			case 675:
				GameModeHandle.GetBonusStage_ExtraPoint(value, packetReader, last);
				break;
			case 1866:
				GameModeHandle.IceFlower_GetQuestion(value, packetReader, last);
				break;
			case 1857:
				GameModeHandle.TypingRun_GoblinRacingQuestion(value, packetReader, last);
				break;
			case 1859:
				GameModeHandle.TypingRun_GoblinRacingTypingDone(value, packetReader, last);
				break;
			case 1880:
				GameModeHandle.Bomb_CountingStart(value, last);
				break;
			case 1882:
				GameModeHandle.Bomb_RaceTransferBomb(value, packetReader, last);
				break;
			case 1748:
				TowerEvent.Handle_GetBox(value, packetReader, last);
				break;
			case 1750:
				TowerEvent.Handle_GetBox2(value, packetReader, last);
				break;
			default:
				switch (num)
				{
				case 1746:
					TowerEvent.Handle_EnterEvent(value, packetReader, last);
					break;
				case 1751:
					TowerEvent.Handle_GiveUP(value, last);
					break;
				case 1753:
					TowerEvent.Handle_UserGetItemInfo(value, packetReader, last);
					break;
				}
				break;
			case 100:
			case 653:
			case 971:
			case 1086:
			case 1299:
				break;
			}
		}

		private void Handle_Agent_To_RoomUser(PacketReader reader)
		{
			int key = reader.ReadLEInt32();
			reader.ReadLEInt32();
			if (CurrentAccounts.TryGetValue(key, out var value))
			{
				NormalRoom room = Rooms.GetRoom(value.CurrentRoomId);
				if (room != null)
				{
					ushort length = reader.ReadLEUInt16();
					byte[] np = reader.ReadByteArray(length);
					room.BroadcastToAll(np);
				}
			}
		}

		private void Handle_Agent_To_RoomUser_Me(PacketReader reader)
		{
			int key = reader.ReadLEInt32();
			reader.ReadLEInt32();
			if (CurrentAccounts.TryGetValue(key, out var value))
			{
				Rooms.GetRoom(value.CurrentRoomId);
				ushort length = reader.ReadLEUInt16();
				byte[] msg = reader.ReadByteArray(length);
				value.SendAsync(msg);
			}
		}

		private void RoomAgentProtocol_WRAP_ROOM_REQ(PacketReader reader, byte last)
		{
			reader.ReadLEInt16();
			switch (reader.ReadLEInt16())
			{
			case 922:
				ItemHandle.ActiveFuncItem_Timeout(reader, last);
				break;
			case 232:
				AvatarLockHandle.Handle_CHANGE_USER_AVATAR_LOCK(reader, last);
				break;
			case 1192:
				ItemHandle.Change_UserItemAttr(reader, last);
				break;
			case 101:
				MyRoomHandle.Handle_ItemOnOff(reader, last);
				break;
			case 1094:
				MyRoomHandle.Handle_ChangeUserActiveItems(reader, last);
				break;
			case 661:
				ItemHandle.Change_UserActiveItemOne(reader, last);
				break;
			case 906:
				ItemHandle.UpdateAvatarInfo(reader, last);
				break;
			}
		}
	}
}
