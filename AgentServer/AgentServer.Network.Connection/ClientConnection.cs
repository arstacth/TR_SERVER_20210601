using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AgentServer.EasyAntiCheat;
using AgentServer.Function;
using AgentServer.Holders;
using AgentServer.Packet;
using AgentServer.Packet.RoomServer;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Mission;
using Akka.Actor;
using Akka.IO;
using LocalCommons.Cryptography;
using LocalCommons.Network;
using LocalCommons.Utilities;
using MySql.Data.MySqlClient;
using NetMsg.LBS;
using NetMsg.Room;
using Serilog;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace AgentServer.Network.Connections
{
	public class ClientConnection : ReceiveActor
	{
		public class Net_OnConnection : NetPacket
		{
			public Net_OnConnection()
			{
				ns.Write((short)14);
				ns.Write((short)1);
			}
		}

		private readonly IActorRef _connection;

		private readonly object DDOS_Lock = new object();

		public static int TotalAgentLoginUser { get; set; } = 0;


		public static ConcurrentDictionary<int, Account> CurrentAccounts { get; } = new ConcurrentDictionary<int, Account>();


		public static ConcurrentDictionary<string, ConnectionInfo> DDOS_IP { get; } = new ConcurrentDictionary<string, ConnectionInfo>();


		public EndPoint EP { get; set; }

		public string IP { get; private set; }

		public Account CurrentAccount { get; set; }

		public int session { get; set; }

		public string authStatus { get; set; } = string.Empty;


		private void test()
		{
			test();
		}

		public ClientConnection(IActorRef client, EndPoint remote)
		{
			ClientConnection clientConnection = this;
			UntypedActor.Context.Watch(client);
			_connection = client;
			EP = remote;
			IP = ((IPEndPoint)EP).Address.ToString();
			Log.Information("Client: {0} connected", IP);
			DDOS_Filter(IP, 1);
			session = Session.Generate(ServerStatus.MyAgentID);
			Account account2 = (CurrentAccount = new Account
			{
				Connection = this,
				Session = session,
				bLogin = false
			});
			ServerStatus.LBServerActor.Tell(new OnlineUserUpdate
			{
				OnlineCount = CurrentAccounts.Count,
				LoginedCount = CurrentAccounts.Count((KeyValuePair<int, Account> c) => c.Value.isLogin)
			});
			MemoryStream memoryStream = new MemoryStream();
			Receive(delegate(Tcp.Received received)
			{
				int count = received.Data.Count;
				memoryStream.Write(received.Data.ToArray(), 0, count);
				byte[] array = memoryStream.ToArray();
				int num = 0;
				if (count > 0)
				{
					while (true)
					{
						int num2 = 0;
						num2 = ((array.Length - num >= 9) ? BitConverter.ToInt32(array, num) : (-1));
						if (array.Length - num < num2 || num2 == -1)
						{
							break;
						}
						BitConverter.ToUInt32(array, num + 4);
						int num3 = num2 - 9;
						byte[] array2 = new byte[num3];
						Buffer.BlockCopy(array, num + 8, array2, 0, num3);
						clientConnection.HandleReceived(array2);
						num += num2;
					}
					memoryStream.Close();
					memoryStream.Dispose();
					memoryStream = new MemoryStream();
					memoryStream.Write(array, num, array.Length - num);
				}
			});
			Receive(delegate(UserEnterRoomOK rsp)
			{
				clientConnection.CurrentAccount.CurrentRoomId = rsp.RoomID;
				clientConnection.CurrentAccount.InGame = true;
				clientConnection.CurrentAccount.RoomPos = rsp.Pos;
				clientConnection.CurrentAccount.RoomServerID = rsp.RoomServerID;
			});
			Receive<UserLeaveRoomOK>(delegate
			{
				clientConnection.CurrentAccount.CurrentRoomId = 0;
				clientConnection.CurrentAccount.InGame = false;
				clientConnection.CurrentAccount.RoomPos = 0;
				clientConnection.CurrentAccount.RoomServerID = 0;
			});
			Receive(delegate(UpdateItemInfo rsp)
			{
				clientConnection.CurrentAccount.activeItem.updateItemCount(rsp.ItemNum, rsp.Count);
			});
			Receive(delegate(UpdateUserInfo rsp)
			{
				int level = clientConnection.CurrentAccount.Level;
				clientConnection.CurrentAccount.Exp = rsp.EXP;
				clientConnection.CurrentAccount.TR = rsp.GameMoney;
				LobbyHandle.LevelUPCheck(clientConnection.CurrentAccount, level);
			});
			Receive<ReloadDailyMission>(delegate
			{
				if (clientConnection.CurrentAccount.LoginDateTime < MissionHolder.EndReloadTime)
				{
					clientConnection.CurrentAccount.DailyMissionStartTime = DateTime.Now;
					clientConnection.SendAsync(new GetUserDailyMission_ACK(clientConnection.CurrentAccount, 1));
					clientConnection.SendAsync(new OneDayMissionReload_ACK(clientConnection.CurrentAccount, 1));
				}
			});
			Receive(delegate(ParkDivinationCouple rsp)
			{
				if (rsp.CoupleNum == clientConnection.CurrentAccount.CoupleInfo.CoupleNum && rsp.UserNum != clientConnection.CurrentAccount.UserNum)
				{
					ParkHandle.divinationUpdateCoupleAbility(clientConnection.CurrentAccount, rsp.ItemNum);
				}
			});
			Receive(delegate(NetPacket packet)
			{
				clientConnection.SendAsync(packet);
			});
			Receive(delegate(byte[] packet)
			{
				clientConnection.SendAsync(packet);
			});
			Receive<Tcp.ConnectionClosed>(delegate
			{
				try
				{
					clientConnection.ClientConnection_DisconnectedEvent();
					Log.Information("Client: {0} disconnected", remote);
				}
				catch (Exception)
				{
					Log.Warning("Client: {0} disconnected,But the remove fail", remote);
				}
				UntypedActor.Context.Stop(clientConnection.Self);
			});
			Receive<Terminated>(delegate
			{
				try
				{
					clientConnection.ClientConnection_DisconnectedEvent();
					Log.Information("Client: {0} died", remote);
				}
				catch
				{
					Log.Warning("Client: {0} die,But the remove fail", remote);
				}
				UntypedActor.Context.Stop(clientConnection.Self);
			});
		}

		public void SendAsync(NetPacket packet)
		{
			try
			{
				if (packet != null && !CurrentAccount.isDisconnected)
				{
					_connection.Tell(Tcp.Write.Create(ByteString.FromBytes(EncryptPacket(packet.ToArray()))));
				}
			}
			catch (Exception ex)
			{
				Log.Error("ClientConnection SendAsync Error:{0} isLogin:{1}", ex.ToString(), CurrentAccount.isLogin);
			}
		}

		public void SendAsync(byte[] packet)
		{
			try
			{
				if (packet != null && !CurrentAccount.isDisconnected)
				{
					byte[] array = EncryptPacket(packet);
					_connection.Tell(Tcp.Write.Create(ByteString.FromBytes(array)));
				}
			}
			catch (Exception ex)
			{
				Log.Error("ClientConnection SendAsync byte[] Error:{0} isLogin:{1}", ex.ToString(), CurrentAccount.isLogin);
			}
		}

		private byte[] EncryptPacket(byte[] packet)
		{
			int num;
			int num2;
			if (CurrentAccount.bLogin)
			{
				num = ((CurrentAccount.EncryptKey != null) ? 1 : 0);
				if (num != 0)
				{
					num2 = packet.Length + 9;
					goto IL_003a;
				}
			}
			else
			{
				num = 0;
			}
			num2 = packet.Length + 8;
			goto IL_003a;
			IL_003a:
			int num3 = num2;
			PacketWriter packetWriter = PacketWriter.CreateInstance(num3, LittleEndian: true);
			if (num != 0)
			{
				packet = Encrypt.newEncryptByte(CurrentAccount.EncryptKey, CurrentAccount.XorKey, packet);
			}
			packetWriter.Write(num3);
			packetWriter.Write(Utility.CheckSum(packet));
			packetWriter.Write(packet, 0, packet.Length);
			byte[] result = packetWriter.ToArray();
			PacketWriter.ReleaseInstance(packetWriter);
			packetWriter = null;
			return result;
		}

		public void ClientConnection_DisconnectedEvent()
		{
			try
			{
				EACServer.OnLeaveGame(session);
				LoginTrafficManager.Logout(session, CurrentAccount.UserID, bSendToAllAgentServer: true);
				CurrentAccount.isDisconnected = true;
				CurrentAccounts.TryRemove(session, out var _);
				ServerStatus.LBServerActor.Tell(new OnlineUserUpdate
				{
					OnlineCount = CurrentAccounts.Count,
					LoginedCount = CurrentAccounts.Count((KeyValuePair<int, Account> c) => c.Value.isLogin)
				});
				if (CurrentAccount.isLogin && !CurrentAccount.isBlocked)
				{
					CurrentAccount.isFishing = false;
					if (CurrentAccount.FishingCancelSource != null)
					{
						CurrentAccount.FishingCancelSource.Cancel();
					}
					MissionUpdate();
					ServerStatus.ToAllRoomServer(new RM_PlayerLeaveRoom(CurrentAccount, isDisconnect: true, 1));
					CurrentAccount.DisconnectedEvent();
					if (Partys.GetParty(CurrentAccount.CurrentPartyID, out var party))
					{
						party.LeaveParty(CurrentAccount, 0, 1);
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			finally
			{
				if (CurrentAccount.isLogin && !CurrentAccount.isBlocked)
				{
					HandleLogout(CurrentAccount);
					CurrentAccount.bLogin = false;
				}
			}
		}

		public void Disconnect()
		{
			_connection.Tell(Tcp.Close.Instance);
		}

		public async void Disconnect(int delay)
		{
			CurrentAccount.isDisconnected = true;
			await Task.Delay(delay);
			_connection.Tell(Tcp.Close.Instance);
		}

		private void HandleLogout(Account User)
		{
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_logout";
				mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("nickName", MySqlDbType.VarString).Value = User.NickName;
				mySqlCommand.Parameters.Add("puid", MySqlDbType.VarString).Value = User.UserID;
				mySqlCommand.Parameters.Add("pexp", MySqlDbType.Int64).Value = User.Exp;
				mySqlCommand.Parameters.Add("ip", MySqlDbType.VarString).Value = User.LastIp;
				mySqlCommand.Parameters.Add("logintime", MySqlDbType.DateTime).Value = User.LoginDateTime;
				mySqlCommand.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Log.Error("Logout sql error: {0}", ex.Message);
			}
		}

		private void MissionUpdate()
		{
			if (MissionHolder.MissionReloading || DateTime.Now.Date != MissionHolder.EndReloadTime.Date || !CurrentAccount.DailyMission.MissionInfo.Any((KeyValuePair<int, MissionInfo> a) => a.Value.challengeState < 2))
			{
				return;
			}
			string text = string.Empty;
			string text2 = string.Empty;
			string text3 = string.Empty;
			foreach (KeyValuePair<int, MissionInfo> item in from w in CurrentAccount.DailyMission.MissionInfo
				where w.Value.challengeState == 0
				select w into o
				orderby o.Key
				select o)
			{
				foreach (KeyValuePair<int, MissionConditionInfo> item2 in item.Value.ConditionInfo)
				{
					if (MissionHolder.ConditionInfoByConditionNum.TryGetValue(item2.Key, out var value) && value.type == 71)
					{
						int achievedPoint = CurrentAccount.DailyMission.MissionInfo[item.Key].ConditionInfo[item2.Key].achievedPoint;
						int num = (int)(DateTime.Now - CurrentAccount.DailyMissionStartTime).TotalSeconds + achievedPoint;
						text += $"{item.Key},";
						text2 += $"{item2.Key},";
						text3 += $"{num},";
					}
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				Mission.mission_ConditionUpdateAchievedPoints(CurrentAccount.UserNum, text, text2, text3, out var _);
			}
		}

		private void HandleReceived(byte[] data)
		{
			if (!ServerStatus.isReady || CurrentAccount == null || CurrentAccount.isDisconnected)
			{
				return;
			}
			PacketReader packetReader = new PacketReader(data, 0);
			ushort num = 0;
			try
			{
				if (CurrentAccount.isLogin)
				{
					packetReader.Decrypt(CurrentAccount.EncryptKey, CurrentAccount.XorKey);
				}
				num = packetReader.ReadLEUInt16();
				if (num >= 1922)
				{
					Log.Error("[PACKET]invalid protocol ({0}) IP: {1}", num, IP);
					DDOS_Filter(IP, 2);
					CurrentAccount.isDisconnected = true;
					Disconnect();
					return;
				}
				if (!CurrentAccount.isLogin && num != 699 && num > 19)
				{
					Log.Warning("未登入 protocol ip:{0}", IP);
					DDOS_Filter(IP, 2);
					CurrentAccount.isDisconnected = true;
					Disconnect();
					return;
				}
				byte b = packetReader.Buffer.LastOrDefault();
				if (CurrentAccount.GotClientKey && CurrentAccount.isLogin)
				{
					if (CurrentAccount.SequenceNum == 0)
					{
						CurrentAccount.SequenceNum = (byte)((b > 128) ? 1 : (b = (byte)(b * 2)));
					}
					else
					{
						if (CurrentAccount.SequenceNum != b)
						{
							Log.Warning("[{0}] error in sequence num({3})({1}), ip:{2}", CurrentAccount.NickName, b, IP, num);
							return;
						}
						CurrentAccount.SequenceNum = (byte)((b > 128) ? 1 : (b = (byte)(b * 2)));
					}
				}
				switch (num)
				{
				case 302:
				case 485:
				case 532:
				case 751:
				case 803:
				case 1128:
				case 1243:
				case 1245:
				case 1698:
					if (!ServerStatus.CanShopOperation)
					{
						SendAsync(new ShopClosed(b));
						return;
					}
					break;
				}
				switch (num)
				{
				case 15:
					LoginHandle.Handle_LoginCheck(this, packetReader);
					break;
				case 19:
					LoginHandle.Handle_GetClientKey(this, packetReader);
					break;
				case 1:
					CurrentAccount.bLogin = true;
					LoginHandle.Handle_LoginSuccess(this, packetReader, b);
					break;
				case 8:
					LoginHandle.Handle_NOTIFY_MY_UDP(this, packetReader, b);
					break;
				case 10:
					MissionHolder.optional_collectionMission_Load(CurrentAccount);
					break;
				case 403:
					GMCommandHandle.Handle_ClientCheckAutoBan(this, packetReader, b);
					break;
				case 458:
					LoginHandle.Handle_GetNickName(this, packetReader, b);
					break;
				case 1060:
					LobbyHandle.Handle_eServer_GET_EXP_REQ(this, packetReader, b);
					break;
				case 1342:
					LobbyHandle.Handle_eServer_GET_ITEM_COLLECTION_INFO_REQ(this, packetReader, b);
					break;
				case 1642:
					LobbyHandle.Handle_eServer_ITEM_COLLECTION_USER_LIST_REQ(this, packetReader, b);
					break;
				case 1706:
					LobbyHandle.Handle_eServer_ITEM_COLLECTION_ADD_REQ(this, packetReader, b);
					break;
				case 1257:
					HotTimeHandle.Handle_eServer_GET_HOTTIME_INFO_REQ(this, b);
					break;
				case 195:
					MyRoomHandle.Handle_ItemOnOff(this, packetReader, b);
					break;
				case 85:
					FirstLoginHandle.Handle_SetNewNickName(this, packetReader, b);
					break;
				case 1303:
					MyRoomHandle.Handle_ItemMsgPop(this, packetReader, b);
					break;
				case 1563:
					ItemHandle.Handle_GetActiveFuncItem(this, packetReader, b);
					break;
				case 706:
					ItemHandle.Handle_GetActiveFuncItem_Position(this, packetReader, b);
					break;
				case 275:
					ItemHandle.Handle_GetActiveFuncItem_List(this, packetReader, b);
					break;
				case 509:
					ItemHandle.Handle_GetCurrentAvatarInfo(this, packetReader, b);
					break;
				case 603:
					LoginHandle.Handle_82(this, b);
					break;
				case 760:
					FirstLoginHandle.Handle_SelectStartCharacter(this, packetReader, b);
					break;
				case 826:
					LobbyHandle.Handle_ShowPage(this, packetReader, b);
					break;
				case 648:
				{
					short num4 = 0;
					switch (packetReader.ReadLEInt16())
					{
					case 0:
						LoginHandle.Handle_GetCommunityAgentServer(this, b);
						break;
					case 2:
						CommunityHandle.Handle_AddFriend(this, packetReader, b);
						break;
					case 5:
						CommunityHandle.Handle_GetFriendListAccepted(this, b);
						break;
					case 8:
						CommunityHandle.Handle_AcceptFriend(this, packetReader, b);
						break;
					case 11:
						CommunityHandle.Handle_DeclineFriend(this, packetReader, b);
						break;
					case 14:
						CommunityHandle.Handle_BlockFriend(this, packetReader, b);
						break;
					case 17:
						CommunityHandle.Handle_UnBlockFriend(this, packetReader, b);
						break;
					case 20:
						CommunityHandle.Handle_DeleteFriend(this, packetReader, b);
						break;
					case 26:
						CommunityHandle.Handle_CancelAddFriend(this, packetReader, b);
						break;
					case 29:
						CommunityHandle.Handle_GetRequestedToMe(this, b);
						break;
					case 31:
						CommunityHandle.Handle_GetFriendGroup(this, packetReader, b);
						break;
					case 34:
						CommunityHandle.Handle_GroupMoveMember(this, packetReader, b);
						break;
					case 38:
						CommunityHandle.Handle_0x7426(this, b);
						break;
					case 40:
						CommunityHandle.Handle_GetGuildMemberList(this, packetReader, b);
						break;
					case 42:
						CommunityHandle.Handle_UpdateGuildMemberList(this, b);
						break;
					case 44:
						CommunityHandle.Handle_ModifyMemo(this, packetReader, b);
						break;
					}
					break;
				}
				case 1694:
					LobbyHandle.HandlePingTime(this, 1, b);
					break;
				case 699:
					LobbyHandle.HandlePingTime(this, 2, b);
					break;
				case 165:
					CommandHandle.Handle_UseShoutItem(this, packetReader, b);
					break;
				case 529:
					HotTimeHandle.Handle_SetHotTimeInfo(this, packetReader, b);
					break;
				case 1520:
					HotTimeHandle.Handle_DeleteHotTimeEvent(this, packetReader, b);
					break;
				case 1593:
					HotTimeHandle.Handle_ApplyHotTimeEvent(this, packetReader, b);
					break;
				case 1584:
					RankHandle.Handle_GetRankInfo(this, packetReader, b);
					break;
				case 966:
					RankHandle.Handle_SearchRank(this, packetReader, b);
					break;
				case 541:
					RankHandle.Handle_GetMyRankInfo(this, packetReader, b);
					break;
				case 446:
					LobbyHandle.Handle_GetUserInfo(this, packetReader, b);
					break;
				case 1016:
					LobbyHandle.Handle_SetGameOption(this, packetReader, b);
					break;
				case 544:
					CommunityHandle.Handle_GetUserAlarmInfo(this, b);
					break;
				case 481:
					ItemHandle.Handle_GetAvatarItems(this, packetReader, b);
					break;
				case 1208:
					LoginHandle.Handle_FF7F01(this, b);
					break;
				case 88:
					UnknownHandle.Handle_FF9701(this, b);
					break;
				case 418:
					LoginHandle.Handle_GetUserCash(this, b);
					break;
				case 619:
					ShopHandle.Handle_GetShopCategoryList(this, b);
					break;
				case 794:
					ShopHandle.Handle_GetShopDisplayList(this, b);
					break;
				case 346:
					ShopHandle.Handle_GetUserVip(this, b);
					break;
				case 576:
					ShopHandle.Handle_GetUserBuyList(this, packetReader, b);
					break;
				case 547:
					ShopHandle.Handle_GetShopPurchasingLimitList(this, packetReader, b);
					break;
				case 329:
					ShopHandle.Handle_GetShopCategoryDisplayItem(this, packetReader, b);
					break;
				case 790:
					UnknownHandle.Handle_FFCF01(this, b);
					break;
				case 170:
					UnknownHandle.Handle_FFD501(this, b);
					break;
				case 382:
					LoginHandle.Handle_GetExtraAbilities(this, b);
					break;
				case 287:
					switch (packetReader.ReadLEInt16())
					{
					case 0:
						MyRoomHandle.Handle_FFCF0100(this, packetReader, b);
						break;
					case 2:
						MyRoomHandle.Handle_MyRoomGetCharacterList(this, b);
						break;
					case 5:
						MyRoomHandle.Handle_MyRoomGetMyCards(this, packetReader, b);
						break;
					case 8:
						MyRoomHandle.Handle_MyRoomGetCharacterSetting(this, packetReader, b);
						break;
					case 11:
						MyRoomHandle.Handle_SaveCharSetting(this, packetReader, b);
						break;
					case 14:
						MyRoomHandle.Handle_CharacterStatReset(this, packetReader, b);
						break;
					case 17:
						MyRoomHandle.Handle_CharacterStatConfirm(this, packetReader, b);
						break;
					case 20:
						MyRoomHandle.Handle_SaveDefaultCharacter(this, packetReader, b);
						break;
					case 26:
						MyRoomHandle.Handle_UseLuckyBag(this, packetReader, b);
						break;
					case 32:
						MyRoomHandle.Handle_FeedPet(this, packetReader, b);
						break;
					case 35:
						MyRoomHandle.Handle_PetRebirth(this, packetReader, b);
						break;
					case 38:
						MyRoomHandle.Handle_PetUpgrade(this, packetReader, b);
						break;
					case 41:
						MyRoomHandle.Handle_RepairItem(this, packetReader, b);
						break;
					case 63:
						CoupleHandle.Handle_UseCoupleExpAddItem(this, packetReader, b);
						break;
					case 66:
						MyRoomHandle.Handle_SetSlotItemSetting(this, packetReader, b);
						break;
					case 69:
						MyRoomHandle.Handle_GetUserSlotInfo(this, b);
						break;
					case 74:
						MyRoomHandle.Handle_GetFavoriteList(this, b);
						break;
					case 76:
						MyRoomHandle.Handle_AddFavorite(this, packetReader, b);
						break;
					case 78:
						MyRoomHandle.Handle_RemoveFavorite(this, packetReader, b);
						break;
					}
					break;
				case 1595:
					switch (packetReader.ReadLEInt16())
					{
					case 0:
						FarmHandle.Handle_EnterFarm(this, packetReader, b);
						break;
					case 1:
						FarmHandle.Handle_CreatePublicFarm(this, packetReader, b);
						break;
					case 10:
						FarmHandle.Handle_GetFarmPoint(this, b);
						break;
					case 12:
						FarmHandle.Handle_GetPublicFarmList(this, packetReader, b);
						break;
					case 24:
						GuildFarmHandle.Handle_EnterGuildFarm(this, packetReader, b);
						break;
					case 25:
						GuildFarmHandle.Handle_GetGuildFarmInfo(this, packetReader, b);
						break;
					case 27:
						GuildFarmHandle.Handle_GetGuildFarmObjectAttr(this, packetReader, b);
						break;
					case 29:
						GuildFarmHandle.Handle_ModifyGuildFarmNoticeBoardInfo(this, packetReader, b);
						break;
					case 32:
						GuildFarmHandle.Handle_GetGuildFarmItemList(this, packetReader, b);
						break;
					case 38:
						FarmHandle.Handle_GetMyFarmItem(this, packetReader, b);
						break;
					case 42:
						FarmHandle.Handle_GetFarmItemList(this, packetReader, b);
						break;
					case 45:
						FarmHandle.Handle_GetFarmItemAttr(this, packetReader, b);
						break;
					case 49:
						FarmHandle.Handle_ReloadFarmMapInfo(this, packetReader, b);
						break;
					case 53:
						FarmHandle.Handle_FFD10136(this, packetReader, b);
						break;
					case 60:
						FarmHandle.Handle_ClearUserFarmMapInfo(this, packetReader, b);
						break;
					case 62:
						FarmHandle.Handle_ModifyFarmMapInfo(this, packetReader, b);
						break;
					case 67:
						FarmHandle.Handle_SearchFarm(this, packetReader, b);
						break;
					case 69:
						FarmHandle.Handle_SearchFarmByUserNum(this, packetReader, b);
						break;
					case 85:
						FarmHandle.Handle_JoinFarmRoom(this, packetReader, b);
						break;
					case 93:
						FarmHandle.Handle_IncreaseAnimalSize(this, packetReader, b);
						break;
					case 95:
						FarmHandle.Handle_RestoreAnimalDefaultSize(this, packetReader, b);
						break;
					case 97:
						FarmHandle.Handle_ChangeFarmType(this, packetReader, b);
						break;
					case 104:
						FarmHandle.Handle_ModifyObjectValueInfo(this, packetReader, b);
						break;
					case 109:
						Log.Information("expired farm item?");
						break;
					case 107:
						FarmHandle.Handle_ChangeFarmSkybox(this, packetReader, b);
						break;
					case 113:
						FarmHandle.Handle_GetMasterUserInfo(this, packetReader, b);
						break;
					case 124:
						FarmHandle.Handle_FFD1017E(this, b);
						break;
					case 127:
						FarmHandle.Handle_SetFarmPortalInfo(this, packetReader, b);
						break;
					case 130:
						FarmHandle.Handle_GetFarmPortalInfo(this, packetReader, b);
						break;
					case 136:
						FarmHandle.Handle_GetFarmSlotListInfo(this, b);
						break;
					case 140:
						FarmHandle.Handle_ClearFarmSlotInfo(this, packetReader, b);
						break;
					case 142:
						FarmHandle.Handle_ChangeFarmMapTypeBySlot(this, packetReader, b);
						break;
					}
					break;
				case 1144:
					LobbyHandle.Handle_SinglePlay(this, packetReader, b);
					break;
				case 1005:
					LobbyHandle.Handle_SinglePlayGoalResult(this, packetReader, b);
					break;
				case 1399:
					Mission.Handle_GetUserMissionInfo(this, packetReader, b);
					break;
				case 1652:
					Mission.Handle_GetUserDailyMissionInfo(this, packetReader, b);
					break;
				case 577:
					Mission.Handle_GetDailyMissionFinishedInfo(this, b);
					break;
				case 890:
					UnknownHandle.Handle_FF5602(this, b);
					break;
				case 1542:
					Mission.Handle_UserOptionalMissionInfo(this, packetReader, b);
					break;
				case 327:
					Mission.Handle_CollectionMissionInfo(this, packetReader, b);
					break;
				case 1163:
					Mission.Handle_AddChallengingMission(this, packetReader, b);
					break;
				case 1001:
					Mission.Handle_RemoveChallengingMission(this, packetReader, b);
					break;
				case 1674:
					Mission.Handle_UpdateMission(this, packetReader, b);
					break;
				case 816:
					Mission.Handle_CompleteMission(this, packetReader, b);
					break;
				case 755:
					Mission.Handle_MissionGiveReward(this, packetReader, b);
					break;
				case 1207:
					Mission.Handle_GetGuildMissionList(this, packetReader, b);
					break;
				case 1689:
					Mission.Handle_SetGuildMasterMission(this, packetReader, b);
					break;
				case 1122:
					Mission.Handle_AcquireEmblem(this, packetReader, b);
					break;
				case 219:
					Mission.Handle_QuestAdd(this, packetReader, b);
					break;
				case 127:
					Mission.Handle_QuestRemove(this, packetReader, b);
					break;
				case 156:
					Mission.Handle_QuestReward(this, packetReader, b);
					break;
				case 1362:
					UnknownHandle.Handle_FF6602(this, b);
					break;
				case 1343:
					UnknownHandle.Handle_FF6802(this, b);
					break;
				case 1709:
					FarmHandle.Handle_FarmExchangeItem(this, packetReader, b);
					break;
				case 1664:
					UnknownHandle.Handle_FFA905(this, b);
					break;
				case 1696:
				{
					byte b3 = packetReader.ReadByte();
					packetReader.Offset--;
					switch (b3)
					{
					case 1:
						ShuSystemHandle.Handle_Shu_Hatch(this, packetReader, b);
						break;
					case 3:
						ShuSystemHandle.Handle_Shu_GetItemInfoByStr(this, packetReader, b);
						break;
					case 5:
						ShuSystemHandle.Handle_Shu_GetUserItemInfo(this, packetReader, b);
						break;
					case 9:
						ShuSystemHandle.Handle_Shu_ManagerAction(this, packetReader, b);
						break;
					case 10:
						ShuSystemHandle.Handle_Shu_ChangeName(this, packetReader, b);
						break;
					case 11:
						ShuSystemHandle.Handle_Shu_ChangeAvatarInfo(this, packetReader, b);
						break;
					case 12:
						ShuSystemHandle.Handle_Shu_ChangeCurrentShu(this, packetReader, b);
						break;
					case 13:
						ShuSystemHandle.Handle_Shu_UseItem(this, packetReader, b);
						break;
					case 14:
						ShuSystemHandle.Handle_Shu_GetGift(this, packetReader, b);
						break;
					case 15:
						ShuSystemHandle.Handle_Shu_ExploreCheck(this, packetReader, b);
						break;
					case 16:
						ShuSystemHandle.Handle_Shu_ExploreStart(this, packetReader, b);
						break;
					case 17:
						ShuSystemHandle.Handle_Shu_ExploreStop(this, packetReader, b);
						break;
					case 18:
						ShuSystemHandle.Handle_Shu_ExploreReward(this, packetReader, b);
						break;
					}
					break;
				}
				case 164:
					LobbyHandle.Handle_GetUserPoint(this, packetReader, b);
					break;
				case 1290:
					AvatarLock.Handle_AvatarLock_Save(this, packetReader, b);
					break;
				case 845:
					AvatarLock.Handle_AvatarLock_Load(this, packetReader, b);
					break;
				case 264:
					GameRoomHandle.Handle_CreateGameRoom(this, packetReader, b);
					break;
				case 1312:
					GameRoomHandle.Handle_LeaveRoom(this, packetReader, b);
					break;
				case 44:
					GameRoomHandle.Handle_GetVertification(this, b);
					break;
				case 1623:
					GameRoomHandle.Handle_PassVertification(this, packetReader, b);
					break;
				case 440:
					GameRoomHandle.Handle_GetRoomKindAttr(this, packetReader, b);
					break;
				case 373:
					GameRoomHandle.Handle_GetRoomList(this, packetReader, b);
					break;
				case 1401:
					GameRoomHandle.Handle_EnterRoom(this, packetReader, b);
					break;
				case 733:
					GameRoomHandle.Handle_RandomEnterRoom(this, packetReader, b);
					break;
				case 838:
					GameRoomHandle.Handle_PlayTogether(this, packetReader, b);
					break;
				case 1211:
					GameRoomHandle.Handle_RoomControl(this, packetReader, b);
					break;
				case 802:
					GameRoomHandle.Handle_KickPlayer(this, packetReader, b);
					break;
				case 751:
					GameRoomHandle.Handle_PlayerList(this, packetReader, b);
					break;
				case 897:
					GameRoomHandle.Handle_GameEndInfo(this, packetReader, b);
					break;
				case 128:
					ShopHandle.Handle_OpenSelectivePackage(this, packetReader, b);
					break;
				case 803:
					ShopHandle.Handle_BuyItem(this, packetReader, b);
					break;
				case 1128:
					ShopHandle.Handle_GiftItem(this, packetReader, b);
					break;
				case 52:
					ItemHandle.Handle_GetAvatarItemOne(this, packetReader, b);
					break;
				case 1619:
					ItemHandle.Handle_GetAvatarItemList(this, packetReader, b);
					break;
				case 450:
					ShopHandle.Handle_GetCurrentGameMoney(this, packetReader, b);
					break;
				case 1245:
					MyRoomHandle.Handle_GetGiftList(this, packetReader, b);
					break;
				case 1198:
					MyRoomHandle.Handle_AcceptGift(this, packetReader, b);
					break;
				case 190:
					MyRoomHandle.Handle_MyroomGetUserItemAttr(this, packetReader, b);
					break;
				case 224:
					ParkHandle.Handle_GetMachineInfo(this, packetReader, b);
					break;
				case 1320:
					ParkHandle.Handle_Alchemist_MachineSelect(this, packetReader, b);
					break;
				case 532:
					ParkHandle.Handle_MachineReceiveORGiftItem(this, packetReader, b);
					break;
				case 626:
					ParkHandle.Handle_MachineKeepItem(this, packetReader, b);
					break;
				case 1679:
					MyRoomHandle.Handle_GetStorageItemList(this, packetReader, b);
					break;
				case 1039:
					MyRoomHandle.Handle_StorageItemGift(this, packetReader, b);
					break;
				case 1027:
					MyRoomHandle.Handle_StorageItemReceive(this, packetReader, b);
					break;
				case 873:
					ExchangeHandle.Handle_GetExchangeSystemInfo(this, packetReader, b);
					break;
				case 1472:
					ExchangeHandle.Handle_ExchangeItem(this, packetReader, b);
					break;
				case 781:
					CombinationShopHandle.Handle_GetUseInfo(this, packetReader, b);
					break;
				case 336:
					CombinationShopHandle.Handle_GetLimitCountInfo(this, packetReader, b);
					break;
				case 236:
					CombinationShopHandle.Handle_ShopExchange(this, packetReader, b);
					break;
				case 1533:
					CombinationShopHandle.Handle_ItemDetailInfo(this, packetReader, b);
					break;
				case 1311:
					GMCommandHandle.Handle_Notice(this, packetReader, b);
					break;
				case 757:
					GMCommandHandle.Handle_DisconnectUser(this, packetReader, b);
					break;
				case 1236:
					GMCommandHandle.Handle_FindGo(this, packetReader, b);
					break;
				case 867:
					GMCommandHandle.Handle_BlockUser(this, packetReader);
					break;
				case 86:
					EventPickBoardHandle.Handle_GetEventPickBoardInfo(this, packetReader, b);
					break;
				case 930:
					EventPickBoardHandle.Handle_EventPickBoard_Use(this, packetReader, b);
					break;
				case 720:
					EventPickBoardHandle.Handle_EventPickBoard_Give(this, packetReader, b);
					break;
				case 855:
					EnchantSystem.Handle_GetEnchantItemInfo(this, packetReader, b);
					break;
				case 698:
					EnchantSystem.Handle_StoneMount(this, packetReader, b);
					break;
				case 886:
					EnchantSystem.Handle_StoneRemove(this, packetReader, b);
					break;
				case 233:
					EnchantSystem.Handle_SealErase(this, packetReader, b);
					break;
				case 990:
					EnchantSystem.Handle_Hardening(this, packetReader, b);
					break;
				case 315:
					ItemTradeHandle.Handle_ItemTrading_Check(this, packetReader, b);
					break;
				case 1246:
					ItemTradeHandle.Handle_ItemTrading_Trade(this, packetReader, b);
					break;
				case 1493:
					ItemTradeHandle.Handle_ItemTrading_Complete(this, packetReader, b);
					break;
				case 1685:
					LobbyHandle.Handle_SetHotKey(this, packetReader, b);
					break;
				case 776:
					LobbyHandle.Handle_GetHotKey(this, b);
					break;
				case 821:
					AssaultModeHandle.Handle_GetAnubisOpenTime(this, b);
					break;
				case 1049:
					AssaultModeHandle.Handle_GetAnubisPoint(this, b);
					break;
				case 717:
					LobbyHandle.Handle_CardPackOpen(this, packetReader, b);
					break;
				case 719:
					AssaultModeHandle.Handle_GetAssaultRaidOpenTime(this, b);
					break;
				case 248:
					AssaultModeHandle.Handle_GetDungeonRaidPoint(this, b);
					break;
				case 265:
					AssaultModeHandle.Handle_GetAssaultModeLimitAttackInfo(this, b);
					break;
				case 557:
					SingleChallengeHandle.Handle_GetUserInfo(this, packetReader, b);
					break;
				case 528:
					SingleChallengeHandle.Handle_StartChallenge(this, packetReader, b);
					break;
				case 331:
					SingleChallengeHandle.Handle_ChallengeAction(this, packetReader, b);
					break;
				case 1497:
					CubeHandle.Handle_CubeCheck(this, packetReader, b);
					break;
				case 1457:
					CubeHandle.Handle_CubeOpen(this, packetReader, b);
					break;
				case 187:
					CubeHandle.Handle_CubeItemAccept(this, packetReader, b);
					break;
				case 1456:
					DyeingHandle.Handle_ItemDyeing(this, packetReader, b);
					break;
				case 1087:
					DyeingHandle.Handle_ItemDyeingRestore(this, packetReader, b);
					break;
				case 400:
					MessageBoxHandle.Handle_SendMessage(this, packetReader, b);
					break;
				case 840:
					MessageBoxHandle.Handle_SearchNickName(this, packetReader, b);
					break;
				case 1153:
					MessageBoxHandle.Handle_GetReceiveList(this, packetReader, b);
					break;
				case 814:
					MessageBoxHandle.Handle_ReadMessage(this, packetReader, b);
					break;
				case 1570:
					MessageBoxHandle.Handle_ReportMessage(this, packetReader, b);
					break;
				case 983:
					MessageBoxHandle.Handle_DeleteMessage(this, packetReader, b);
					break;
				case 1354:
					MessageBoxHandle.Handle_KeepMessage(this, packetReader, b);
					break;
				case 209:
					MessageBoxHandle.Handle_GetOption(this, b);
					break;
				case 569:
					MessageBoxHandle.Handle_OptionChange(this, packetReader, b);
					break;
				case 573:
					CoupleHandle.Handle_CheckProposeInfo(this, packetReader, b);
					break;
				case 1675:
					CoupleHandle.Handle_ChangeCoupleRing(this, packetReader, b);
					break;
				case 168:
					CoupleHandle.Handle_InitProposeInfo(this, packetReader, b);
					break;
				case 1327:
					CoupleHandle.Handle_CreateCoupleInfo(this, packetReader, b);
					break;
				case 261:
					CoupleHandle.Handle_ModifyCoupleInfo(this, packetReader, b);
					break;
				case 348:
					CoupleHandle.Handle_ModifyCoupleName(this, packetReader, b);
					break;
				case 1338:
					CoupleHandle.Handle_RemoveCoupleInfo(this, b);
					break;
				case 46:
					CoupleHandle.Handle_GetCoupleInfo(this, packetReader, b);
					break;
				case 1045:
					CoupleHandle.Handle_UpdateCoupleInfo(this, b);
					break;
				case 1530:
					CoupleHandle.Handle_WeddingSuitForDivorce(this, packetReader, b);
					break;
				case 1477:
					CoupleHandle.Handle_WeddingDivorceReject(this, packetReader, b);
					break;
				case 962:
					CoupleHandle.Handle_WeddingDivorce(this, packetReader, b);
					break;
				case 479:
					CoupleHandle.Handle_GetFamilyInfo(this, packetReader, b);
					break;
				case 516:
					CoupleHandle.Handle_MakeFamilyCheck(this, packetReader, b);
					break;
				case 1388:
					CoupleHandle.Handle_MakeFamily(this, packetReader, b);
					break;
				case 923:
					CoupleHandle.Handle_DissolveFamily(this, packetReader, b);
					break;
				case 1328:
					TalesKnightHandle.Handle_GetMyTalesKnightUnitInfo(this, packetReader, b);
					break;
				case 940:
					TalesKnightHandle.Handle_GetMyTalesKnightsGroupName(this, packetReader, b);
					break;
				case 1502:
					TalesKnightHandle.Handle_GetMyTalesKnightsGroupInfo(this, packetReader, b);
					break;
				case 1280:
					TalesKnightHandle.Handle_UpdateTalesKnightsGroup(this, packetReader, b);
					break;
				case 1326:
					TalesKnightHandle.Handle_UpdateTalesKnightsGroup_Name(this, packetReader, b);
					break;
				case 1435:
					TalesKnightHandle.Handle_HasTalesKnightUnit(this, packetReader, b);
					break;
				case 243:
					TalesKnightHandle.Handle_GetTalesKnightStageInfo(this, packetReader, b);
					break;
				case 520:
					TalesKnightHandle.Handle_TalesKnightStageEnter(this, packetReader, b);
					break;
				case 200:
					TalesKnightHandle.Handle_TalesKnights_AttackCheck(this, packetReader, b);
					break;
				case 121:
					TalesKnightHandle.Handle_TalesKnightsRewardReceive(this, packetReader, b);
					break;
				case 616:
					TalesKnightHandle.Handle_MyKnightsCallComeBack(this, packetReader, b);
					break;
				case 1015:
					TalesKnightHandle.Handle_TalesKnightsAdd_MaxLevel(this, packetReader, b);
					break;
				case 155:
					TalesKnightHandle.Handle_TalesKnightsUseExpUpItem(this, packetReader, b);
					break;
				case 665:
					UnknownHandle.Handle_FF8406(this, b);
					break;
				case 1432:
					FishingHandle.Handle_GetFishRecordInfo(this, b);
					break;
				case 964:
					FishingHandle.Handle_GetFishNetInfo(this, b);
					break;
				case 1376:
					FishingHandle.Handle_Fishing(this, packetReader, b);
					break;
				case 579:
					FishingHandle.Handle_CollectFishedItem(this, packetReader, b);
					break;
				case 912:
					FishingHandle.Handle_GetFarmFishingReward(this, packetReader, b);
					break;
				case 1120:
					FishingHandle.Handle_SetFarmFishingReward(this, packetReader, b);
					break;
				case 320:
					FishingHandle.Handle_RemoveFarmFishingReward(this, packetReader, b);
					break;
				case 1307:
					FishingHandle.Handle_MiniGameFishing(this, packetReader, b);
					break;
				case 1183:
					EventPickBoardHandle.Handle_GetHuMongPickBoardInfo(this, packetReader, b);
					break;
				case 1035:
					EventPickBoardHandle.Handle_HuMongPickBoard_PickItem(this, packetReader, b);
					break;
				case 1415:
					EventPickBoardHandle.Handle_HuMongPickBoard_GiveItem(this, packetReader, b);
					break;
				case 1436:
				{
					packetReader.Offset += 4;
					byte b2 = packetReader.ReadByte();
					packetReader.Offset += 3;
					switch (b2)
					{
					case 0:
						PartyHandle.Handle_PartyInvite(this, packetReader, b);
						break;
					case 1:
						PartyHandle.Handle_AcceptPartyInvite(this, packetReader, b);
						break;
					case 2:
						PartyHandle.Handle_PartyLeave(this, packetReader, b);
						break;
					case 4:
						PartyHandle.Handle_PartyKickUser(this, packetReader, b);
						break;
					case 8:
						PartyHandle.Handle_PartyChangeLeader(this, packetReader, b);
						break;
					case 14:
						PartyHandle.Handle_PartyRecruit(this, packetReader, b);
						break;
					case 15:
						PartyHandle.Handle_PartyRecruitCancel(this, packetReader, b);
						break;
					case 16:
						PartyHandle.Handle_GetPartyIndex(this, packetReader, b);
						break;
					case 17:
						PartyHandle.Handle_GetPartyUserList(this, packetReader, b);
						break;
					case 18:
						PartyHandle.Handle_PartyJoinRequest(this, packetReader, b);
						break;
					case 23:
						PartyHandle.Handle_PartyJoinRequestReject(this, packetReader, b);
						break;
					case 26:
						PartyHandle.Handle_GetPartyJoinRequestList(this, packetReader, b);
						break;
					}
					break;
				}
				case 1471:
				{
					short num3 = packetReader.ReadLEInt16();
					switch (num3)
					{
					case 0:
						GuildHandle.Handle_CheckGuildName(this, packetReader, b);
						break;
					case 2:
						GuildHandle.Handle_MakeGuild(this, packetReader, b);
						break;
					case 5:
						GuildHandle.Handle_DelGuild(this, packetReader, b);
						break;
					case 8:
						GuildHandle.Handle_GetGuildList(this, packetReader, b);
						break;
					case 11:
						GuildHandle.Handle_RequestJoin(this, packetReader, b);
						break;
					case 14:
						GuildHandle.Handle_CancelPropose(this, packetReader, b);
						break;
					case 17:
						GuildHandle.Handle_ProcessJoinRequest(this, packetReader, b);
						break;
					case 20:
						GuildHandle.Handle_ProcessLeave(this, packetReader, b);
						break;
					case 23:
						GuildHandle.Handle_GetJoinRequestList(this, b);
						break;
					case 28:
						GuildHandle.Handle_ModifyJoinLimitLevel(this, packetReader, b);
						break;
					case 31:
						GuildHandle.Handle_ModifyJoinMethod(this, packetReader, b);
						break;
					case 37:
						GuildHandle.Handle_ModifyMemberGrade(this, packetReader, b);
						break;
					case 40:
						GuildHandle.Handle_ModifyMessage(this, packetReader, b);
						break;
					case 46:
						GuildHandle.Handle_LevelUP(this, b);
						break;
					case 49:
						GuildHandle.Handle_GetGuildInfo(this, packetReader, b);
						break;
					case 59:
						GuildHandle.Handle_GetContributionPoint(this, packetReader, b);
						break;
					case 62:
						GuildHandle.Handle_GetGuildPoint(this, packetReader, b);
						break;
					case 65:
						GuildHandle.Handle_UseGiftBox(this, packetReader, b);
						break;
					case 68:
						Console.Write(0);
						break;
					case 72:
						GuildHandle.Handle_AddGuildSkill(this, packetReader, b);
						break;
					case 74:
						GuildHandle.Handle_ResetGuildSkill(this, packetReader, b);
						break;
					default:
						Log.Debug("Unknown Guild subopcode: 0x{0:X2}", num3);
						break;
					}
					break;
				}
				case 1833:
					TutorialChannel.Handle_GetUserInfo(this, packetReader, b);
					break;
				case 1835:
					TutorialChannel.Handle_RequestReward(this, packetReader, b);
					break;
				case 734:
					ArinHandle.Handle_ItemStrengthen_StrengthenSlot(this, packetReader, b);
					break;
				case 1461:
					ArinHandle.Handle_ItemStrengthen_CleanSlot(this, packetReader, b);
					break;
				case 1038:
					ArinHandle.Handle_IncreaseStrengthenCount(this, packetReader, b);
					break;
				case 721:
					ArinHandle.Handle_ItemTransform(this, packetReader, b);
					break;
				case 1007:
					Anniversary.Handle_ObjectAction(this, packetReader, b);
					break;
				case 904:
					Anniversary.Handle_GetObjectValue(this, packetReader, b);
					break;
				case 580:
					Anniversary.Handle_GetReveivedRewardGradeList(this, packetReader, b);
					break;
				case 1523:
					Anniversary.Handle_ReceiveReward(this, packetReader, b);
					break;
				case 690:
					Archives.Handle_GetUserInfo(this, b);
					break;
				case 464:
					Archives.Handle_GetReward(this, packetReader, b);
					break;
				case 59:
					Archives.Handle_Archives_Exchange(this, packetReader, b);
					break;
				case 1805:
					switch (packetReader.ReadLEInt32())
					{
					case 0:
						DiceBoardHandle.Handle_GetDiceBoardUserInfoAndRewardInfo(this, packetReader, b);
						break;
					case 1:
						DiceBoardHandle.Handle_DiceBoard_Draw(this, packetReader, b);
						break;
					case 2:
						DiceBoardHandle.Handle_DiceBoard_FillGauge(this, packetReader, b);
						break;
					case 3:
						DiceBoardHandle.Handle_GetDiceBoardList(this, packetReader, b);
						break;
					case 5:
						DiceBoardHandle.Handle_GetDiceBoardUserInfo(this, packetReader, b);
						break;
					case 6:
						DiceBoardHandle.Handle_DiceBoard_Reset(this, packetReader, b);
						break;
					}
					break;
				case 1806:
				{
					int num2 = packetReader.ReadLEInt32();
					packetReader.Offset += 4;
					switch (num2)
					{
					case 1:
						ThankOfferingHandle.Handle_Reward(this, packetReader, b);
						break;
					case 2:
						ThankOfferingHandle.Handle_GetUserPoint(this, packetReader, b);
						break;
					case 4:
						ThankOfferingHandle.Handle_GetRank(this, packetReader, b);
						break;
					}
					break;
				}
				case 1300:
					MyRoomHandle.Handle_UseExtraAbilityItem(this, packetReader, b);
					break;
				case 1416:
					ParkHandle.Handle_CheckDivinationFree(this, packetReader, b);
					break;
				case 1302:
					ParkHandle.Handle_ParkUseDivinationFree(this, packetReader, b);
					break;
				case 563:
					GuildMatchHandle.Handle_GuildMatch_GetRankRange(this, packetReader, b);
					break;
				case 854:
					GuildMatchHandle.Handle_GuildMatch_GetRankMyGuild(this, packetReader, b);
					break;
				case 1421:
					GameRoomHandle.Handle_JoinGuildMatch(this, b);
					break;
				case 804:
					GuildMatchHandle.Handle_LookingForGuildMatch(this, packetReader, b);
					break;
				case 357:
					GuildMatchHandle.Handle_PickGuildForMatch(this, packetReader, b);
					break;
				case 691:
					GuildMatchHandle.Handle_CancelLookingForGuildMatch(this, b);
					break;
				case 381:
					GuildMatchHandle.Handle_ChooseGuildForMatch(this, packetReader, b);
					break;
				case 735:
					GameRoomHandle.Handle_EnterRoomForGuild(this, packetReader, b);
					break;
				case 463:
					LobbyHandle.Handle_GetOfficialCompetitionOpenTime(this, packetReader, b);
					break;
				case 1578:
					CompetitionEventHandle.Handle_PartyUserInfo(this, packetReader, b);
					break;
				case 1438:
					CompetitionEventHandle.Handle_PartyJoin(this, packetReader, b);
					break;
				case 1195:
					CompetitionEventHandle.Handle_GetPartyPointInfo(this, b);
					break;
				case 1391:
					CompetitionEventHandle.Handle_PointReward(this, packetReader, b);
					break;
				case 105:
					CompetitionEventHandle.Handle_TodayGameInfo(this, b);
					break;
				case 1006:
					CompetitionEventHandle.Handle_FronTier_SchduleInfo(this, b);
					break;
				case 335:
					CompetitionEventHandle.Handle_GetRoomKindPlayerNum(this, b);
					break;
				case 1762:
					CompetitionEventHandle.Handle_SEASON_CHANNEL_SCHEDULE_REQ(this, b);
					break;
				case 1744:
					TowerEvent.Handle_GetUserJoinInfo(this, packetReader, b);
					break;
				case 1746:
					TowerEvent.Handle_EnterEvent(this, packetReader, b);
					break;
				case 1751:
					TowerEvent.Handle_GiveUP(this, packetReader, b);
					break;
				case 1753:
					TowerEvent.Handle_UserGetItemInfo(this, packetReader, b);
					break;
				case 1453:
					DolimpanHandle.Handle_GetMyInfo(this, b);
					break;
				case 91:
					DolimpanHandle.Handle_Play(this, packetReader, b);
					break;
				case 141:
					DolimpanHandle.Handle_Reward(this, packetReader, b);
					break;
				case 1055:
					DolimpanHandle.Handle_Reset(this, packetReader, b);
					break;
				case 632:
					DolimpanHandle.Handle_Charge(this, packetReader, b);
					break;
				case 303:
					switch (packetReader.ReadLEInt16())
					{
					case 0:
						GuildPlantHandle.Handle_GetGuildManageTR(this, packetReader, b);
						break;
					case 3:
						GuildPlantHandle.Handle_InvestGuildManageTR(this, packetReader, b);
						break;
					case 6:
						GuildPlantHandle.Handle_GetStorageExtend(this, packetReader, b);
						break;
					case 9:
						GuildPlantHandle.Handle_RegisterItem(this, packetReader, b);
						break;
					case 12:
						GuildPlantHandle.Handle_GetMakeProgressItem(this, packetReader, b);
						break;
					case 16:
						GuildPlantHandle.Handle_GetMakeStandByItemList(this, packetReader, b);
						break;
					case 19:
						GuildPlantHandle.Handle_ChangeMyConstributionPointItem(this, packetReader, b);
						break;
					case 22:
						GuildPlantHandle.Handle_GetInvestorManageTRList(this, packetReader, b);
						break;
					case 25:
						GuildPlantHandle.Handle_GetExpenseList(this, packetReader, b);
						break;
					case 28:
						GuildPlantHandle.Handle_GetItemContributionRankList(this, packetReader, b);
						break;
					case 31:
						GuildPlantHandle.Handle_GetGivePossibleUserList(this, packetReader, b);
						break;
					case 34:
						GuildPlantHandle.Handle_GiveGift(this, packetReader, b);
						break;
					case 37:
						GuildPlantHandle.Handle_GetPlantItemList(this, packetReader, b);
						break;
					case 40:
						GuildPlantHandle.Handle_BuyItem(this, packetReader, b);
						break;
					}
					break;
				case 1243:
					AlchemistHandle.Handle_AlchemistMix(this, packetReader, b);
					break;
				case 997:
					AlchemistHandle.Handle_AlchemistEnchantGrade(this, packetReader, b);
					break;
				case 849:
					AlchemistHandle.Handle_AlchemistDisjoint(this, packetReader, b);
					break;
				case 1178:
					AlchemistHandle.Handle_AlchemistHistory(this, packetReader, b);
					break;
				case 237:
					AlchemistHandle.Handle_AlchemistQuickJoin(this, packetReader, b);
					break;
				case 1904:
					AttendanceHandle.Handle_AttendanceInfo(this, packetReader, b);
					break;
				case 1906:
					AttendanceHandle.Handle_AttendanceReward(this, packetReader, b);
					break;
				case 1809:
					EACServer.OnReceivedEACMessage(this, packetReader);
					break;
				default:
					if (!CurrentAccount.isLogin)
					{
						DDOS_Filter(IP, 2);
					}
					break;
				case 364:
				case 1419:
				case 1441:
				case 1490:
				case 1756:
				case 1760:
				case 1803:
				case 1912:
					break;
				}
			}
			catch (Exception ex)
			{
				string propertyValue = Utility.ByteArrayToString(packetReader.Buffer.ToArray());
				Log.Error("HandleReceived Error:{0} {1} {2}", ex.ToString(), num, propertyValue);
			}
		}

		private void DDOS_Filter(string ip, int type)
		{
			try
			{
				if (!Conf.BlockDDOS)
				{
					return;
				}
				lock (DDOS_Lock)
				{
					if (DDOS_IP.TryGetValue(ip, out var value))
					{
						if (value.UnknownOpcodeTime >= Conf.JudgeTime || (DateTime.Compare(DateTime.Now, value.FirstTimeConnect.AddSeconds(10.0)) <= 0 && value.ConnectTime >= Conf.MaxConnectTime))
						{
							IRule rule = FirewallManager.Instance.Rules.FirstOrDefault((IRule r) => r.Name == "DDOS Block");
							if (rule != null)
							{
								List<IAddress> list = rule.RemoteAddresses.ToList();
								list.Add(SingleIP.FromIPAddress(IPAddress.Parse(ip)));
								rule.RemoteAddresses = list.ToArray();
								Log.Warning("Detected ip:{0} try to DDOS server!", ip);
							}
						}
						switch (type)
						{
						case 1:
							DDOS_IP[ip].ConnectTime++;
							break;
						case 2:
							DDOS_IP[ip].UnknownOpcodeTime++;
							break;
						}
						if (DateTime.Compare(DateTime.Now, value.FirstTimeConnect.AddSeconds(10.0)) > 0)
						{
							DDOS_IP[ip].FirstTimeConnect = DateTime.Now;
							DDOS_IP[ip].UnknownOpcodeTime = ((type == 2) ? 1 : 0);
							DDOS_IP[ip].ConnectTime = ((type == 1) ? 1 : 0);
						}
					}
					else
					{
						DDOS_IP.TryAdd(ip, new ConnectionInfo
						{
							UnknownOpcodeTime = ((type == 2) ? 1 : 0),
							ConnectTime = ((type == 1) ? 1 : 0),
							FirstTimeConnect = DateTime.Now
						});
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("Error on filtering DDOS: ip:{1}\r\n{0}", ex.ToString(), ip);
			}
		}
	}
}
