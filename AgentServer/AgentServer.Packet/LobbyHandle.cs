using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AgentServer.Database;
using AgentServer.Function;
using AgentServer.Holders;
using AgentServer.Network.Connections;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.User;
using Akka.Actor;
using LocalCommons.Network;
using LocalCommons.Utilities;
using Serilog;

namespace AgentServer.Packet
{
	public class LobbyHandle
	{
		public static void Handle_ShowPage(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			byte type = reader.ReadByte();
			currentAccount.TRNeedUpdateFromDB = true;
			currentAccount.EXPNeedUpdateFromDB = true;
			currentAccount.CashNeedUpdateFromDB = true;
			Client.SendAsync(new ShowPage(currentAccount, type, last));
		}

		public static void HandlePingTime(ClientConnection Client, int type, byte last)
		{
			Account User = Client.CurrentAccount;
			IEnumerable<Account> enumerable = ClientConnection.CurrentAccounts.Values.Where((Account players) => players.UserNum == User.UserNum && players.isLogin);
			if (User.UserNum > 0 && enumerable.Count() > 1)
			{
				Log.Information("User [{0}] has already logged in!", User.UserID);
				Client.SendAsync(new LoginError(6, last, 0));
				{
					foreach (Account item in enumerable)
					{
						if (item.Session != User.Session)
						{
							item.Connection.SendAsync(new DisconnectPacket(259, last));
							item.Connection.Disconnect(5000);
						}
					}
					return;
				}
			}
			long time = Utility.CurrentTimeMilliseconds();
			Client.SendAsync(new PingTime(User.bLogin, time, last));
			User.checkActiveItem(last);
		}

		public static void Handle_SinglePlay(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			if (MapHolder.MapInfos.TryGetValue(num, out var value) && value.CanTimeAttack)
			{
				Client.SendAsync(new SinglePlay(num, last));
				currentAccount.SinglePlayMapNum = num;
			}
		}

		public static void Handle_GetUserInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int fixedLength = reader.ReadLEInt16();
			string text = reader.ReadBig5StringSafe(fixedLength);
			TBDyeInfo(text, out var AvatarItemDyeing);
			FishingHandle.GetFishRecord(text, out var fishrecord);
			Client.SendAsync(new GetUserInfo(currentAccount, text, AvatarItemDyeing, fishrecord, last));
		}

		public static void Handle_GetUserPoint(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			requestUserPoint(currentAccount.UserNum, num, out var totlapoint, out var currentpoint);
			Client.SendAsync(new GetUserPoint(num, totlapoint, currentpoint, last));
		}

		public static void Handle_eServer_GET_ITEM_COLLECTION_INFO_REQ(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int fixedLength = reader.ReadLEInt16();
			string text = reader.ReadBig5StringSafe(fixedLength);
			bool bOtherUser = currentAccount.NickName != text;
			if (itemCollection_GetUserInfo(text, bOtherUser, 1, out var info, out var _))
			{
				Client.SendAsync(new eServer_GET_ITEM_COLLECTION_INFO_REQ(text, bOtherUser, info, last));
			}
		}

		public static void Handle_eServer_ITEM_COLLECTION_USER_LIST_REQ(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int fixedLength = reader.ReadLEInt16();
			string text = reader.ReadBig5StringSafe(fixedLength);
			bool bOtherUser = currentAccount.NickName != text;
			if (itemCollection_GetUserInfo(text, bOtherUser, 2, out var _, out var itemnums))
			{
				Client.SendAsync(new eServer_ITEM_COLLECTION_USER_LIST_REQ(text, itemnums, last));
			}
		}

		public static void Handle_eServer_ITEM_COLLECTION_ADD_REQ(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int fixedLength = reader.ReadLEInt16();
			string text = reader.ReadBig5StringSafe(fixedLength);
			if (new Regex("^[\\d,]+$").IsMatch(text) && itemCollection_UdateUserInfo(currentAccount.UserNum, 2, text))
			{
				Client.SendAsync(new eServer_ITEM_COLLECTION_ADD_ACK(last));
			}
		}

		public static void Handle_SetGameOption(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			bool flag = reader.ReadBoolean();
			bool flag2 = false;
			flag2 = ((!flag) ? FlagsHelper.IsSet(currentAccount.GameOption, num) : (!FlagsHelper.IsSet(currentAccount.GameOption, num)));
			if (flag2 && currentAccount.GameOption >= 0)
			{
				int flags = currentAccount.GameOption;
				if (flag)
				{
					FlagsHelper.Set(ref flags, num);
				}
				else
				{
					FlagsHelper.Unset(ref flags, num);
				}
				setGameOption(currentAccount, flags);
				Client.SendAsync(new SetGameOption(currentAccount.GameOption, last));
			}
			else
			{
				Log.Warning("[GameOption Invalid Set] UserNum:{0} CurrentOption:{1} SetValue:{2} isAdd:{3}", currentAccount.UserNum, currentAccount.GameOption, num, flag);
			}
		}

		public static void Handle_eServer_GET_EXP_REQ(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			short num = reader.ReadLEInt16();
			long value = 0L;
			if (num == 1)
			{
				value = currentAccount.Exp;
			}
			if (num == 1 && currentAccount.EXPNeedUpdateFromDB)
			{
				int level = currentAccount.Level;
				getExp(currentAccount, num);
				if (LevelUPCheck(currentAccount, level))
				{
					Client.SendAsync(new UserLevelUPEXPInfo(1, currentAccount.Level, currentAccount.Exp, last));
				}
				value = currentAccount.Exp;
			}
			Client.SendAsync(new eServer_GET_EXP_REQ(num, value, last));
		}

		public static void Handle_SetHotKey(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt16();
			string text = string.Empty;
			List<int> list = new List<int>();
			for (int i = 0; i < num; i++)
			{
				reader.ReadLEInt16();
				int num2 = reader.ReadLEInt32();
				list.Add(num2);
				text = ((i == 9) ? (text + $"{num2}") : (text + $"{num2},"));
			}
			if (HotKeySet(currentAccount.UserNum, text))
			{
				Client.SendAsync(new HotKeySetOK(list, last));
			}
		}

		public static void Handle_GetHotKey(ClientConnection Client, byte last)
		{
			HotKeyGet(Client.CurrentAccount.UserNum, out var hkstr);
			List<int> list = new List<int>();
			if (!string.IsNullOrEmpty(hkstr))
			{
				string[] array = hkstr.Split(',');
				foreach (string value in array)
				{
					list.Add(Convert.ToInt32(value));
				}
			}
			Client.SendAsync(new HotKeyGetOK(list, last));
		}

		public static void Handle_CardPackOpen(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int cardpacknum = reader.ReadLEInt32();
			short opentype = reader.ReadLEInt16();
			if (OpenCardPack(currentAccount, cardpacknum, opentype, out var cardpackinfos))
			{
				Client.SendAsync(new CardPackOpen(cardpacknum, opentype, cardpackinfos, last));
			}
		}

		public static void Handle_SinglePlayGoalResult(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int singlePlayMapNum = currentAccount.SinglePlayMapNum;
			int num = reader.ReadLEInt32();
			MapHolder.MapInfos.TryGetValue(singlePlayMapNum, out var value);
			int num2 = value.GoalInLimitTime * 1000;
			if (num2 != 0 && num < num2)
			{
				Log.Warning("Player[{0}] {1}ms goalin {2} map too fast in single play mode!", currentAccount.NickName, num, singlePlayMapNum);
				GMCommandHandle.AutoBan(currentAccount.NickName, 1, 10, 0, currentAccount.LastIp);
				Client.SendAsync(new DisconnectPacket(260, last));
				Client.Disconnect(5000);
			}
			else
			{
				Client.SendAsync(new SinglePlayGoal_ACK(num, last));
			}
		}

		public static void Handle_GetOfficialCompetitionOpenTime(ClientConnection Client, PacketReader reader, byte last)
		{
			reader.ReadLEInt32();
			Client.SendAsync(new GetOfficialCompetitionOpenTime_ACK(last));
		}

		public static bool LevelUPCheck(Account User, int beforelevel)
		{
			User.GetMyLevel();
			bool num = User.Level != beforelevel;
			if (num && User.Level >= 71 && (new List<int> { 71, 78, 85 }.Contains(User.Level) || User.Level >= 92))
			{
				ActorRefImplicitSenderExtensions.Tell(message: new LevelUPNotice($"{User.NickName},{User.Level}", 1), receiver: ServerStatus.LBServerActor);
			}
			return num;
		}

		public static bool itemCollection_GetUserInfo(string UserName, bool bOtherUser, short reqType, out UserItemCollectionInfo info, out List<int> itemnums)
		{
			info = new UserItemCollectionInfo();
			itemnums = new List<int>();
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_itemCollection_GetUserInfo");
				mySqlCommandHelper.AddParamVarString("nickName", UserName);
				mySqlCommandHelper.AddParamShort("bOtherUser", (short)(bOtherUser ? 1 : 0));
				mySqlCommandHelper.AddParamShort("reqType", reqType);
				mySqlCommandHelper.Execute();
				switch (reqType)
				{
				case 1:
					if (mySqlCommandHelper.HasResult())
					{
						info.point = mySqlCommandHelper.GetInt("point");
						info.rank = mySqlCommandHelper.GetInt("rank");
						info.noticedLevel = mySqlCommandHelper.GetByteConvert("noticedLevel");
						return true;
					}
					info.point = 0;
					info.rank = 0;
					info.noticedLevel = 0;
					return true;
				case 2:
					if (mySqlCommandHelper.HasRows)
					{
						while (mySqlCommandHelper.HasResult())
						{
							itemnums.Add(mySqlCommandHelper.GetInt("itemNum"));
						}
						return true;
					}
					break;
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_itemCollection_GetUserInfo Error: {0}", ex.Message);
			}
			return false;
		}

		private static bool itemCollection_UdateUserInfo(int UserNun, short reqType, string val)
		{
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_itemCollection_UpdateUserInfo");
				mySqlCommandHelper.AddParamInt("userNum", UserNun);
				mySqlCommandHelper.AddParamShort("reqType", reqType);
				mySqlCommandHelper.AddParamVarString("val", val);
				mySqlCommandHelper.ExecuteSingle();
				if (mySqlCommandHelper.HasResult())
				{
					if (mySqlCommandHelper.GetInt("ret") == 0)
					{
						return true;
					}
					return false;
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_itemCollection_UpdateUserInfo Error: {0}", ex.Message);
			}
			return false;
		}

		private static void setGameOption(Account User, int optionvalue)
		{
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_setGameOption");
				mySqlCommandHelper.AddParamInt("userNum", User.UserNum);
				mySqlCommandHelper.AddParamInt("option", optionvalue);
				mySqlCommandHelper.ExecuteNonQuery();
				User.GameOption = optionvalue;
			}
			catch (Exception ex)
			{
				Log.Error("usp_setGameOption Error: {0}", ex.Message);
			}
		}

		private static void requestUserPoint(int UserNum, int rewardGroup, out int totlapoint, out int currentpoint)
		{
			totlapoint = 0;
			currentpoint = 0;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_requestUserPoint");
				mySqlCommandHelper.AddParamInt("userNum", UserNum);
				mySqlCommandHelper.AddParamInt("rewardGroup", rewardGroup);
				mySqlCommandHelper.ExecuteSingle();
				if (mySqlCommandHelper.HasResult())
				{
					totlapoint = mySqlCommandHelper.GetInt("pointAccumulated");
					currentpoint = mySqlCommandHelper.GetInt("pointCurrent");
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_requestUserPoint Error: {0}", ex.Message);
			}
		}

		private static bool HotKeySet(int UserNum, string HotKey)
		{
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_HotKeySet");
				mySqlCommandHelper.AddParamInt("UserNum", UserNum);
				mySqlCommandHelper.AddParamVarString("HotKey", HotKey);
				mySqlCommandHelper.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Log.Error("usp_HotKeySet Error: {0}", ex.Message);
			}
			return true;
		}

		private static void HotKeyGet(int UserNum, out string hkstr)
		{
			hkstr = string.Empty;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_HotKeyGet");
				mySqlCommandHelper.AddParamInt("UserNum", UserNum);
				mySqlCommandHelper.ExecuteSingle();
				if (mySqlCommandHelper.HasResult())
				{
					hkstr = mySqlCommandHelper.GetString("fdHotKey");
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_HotKeyGet Error: {0}", ex.Message);
			}
		}

		private static bool OpenCardPack(Account User, int cardpacknum, short opentype, out List<CardPackResultInfo> cardpackinfos)
		{
			cardpackinfos = new List<CardPackResultInfo>();
			try
			{
				using (MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_cardPack_CardOpen"))
				{
					mySqlCommandHelper.AddParamInt("pUsernum", User.UserNum);
					mySqlCommandHelper.AddParamInt("pUserLuck", (int)User.Luck);
					mySqlCommandHelper.AddParamInt("pCardPackNum", cardpacknum);
					mySqlCommandHelper.AddParamInt("pOpenType", opentype);
					mySqlCommandHelper.Execute();
					if (mySqlCommandHelper.HasRows)
					{
						long tR = User.TR;
						while (mySqlCommandHelper.HasResult())
						{
							tR = mySqlCommandHelper.GetLong("remainGameMoney");
							if (mySqlCommandHelper.GetInt("fdType") == 2)
							{
								CardPackResultInfo item = new CardPackResultInfo
								{
									RewardType = mySqlCommandHelper.GetInt("fdRewardType"),
									RewardItem = mySqlCommandHelper.GetInt("fdRewardItem"),
									RewardCount = mySqlCommandHelper.GetInt("fdRewardCount")
								};
								cardpackinfos.Add(item);
							}
						}
						User.TR = tR;
						return true;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				Log.Error("usp_cardPack_CardOpen Error: {0}", ex.Message);
			}
			return false;
		}

		public static void TBDyeInfo(string nickname, out List<UserItemDyeing> AvatarItemDyeing)
		{
			AvatarItemDyeing = new List<UserItemDyeing>();
			AvatarItemDyeing.AddRange(Enumerable.Repeat(new UserItemDyeing(), 24));
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_itemdyeing_tbinfo");
				mySqlCommandHelper.AddParamVarString("NickName", nickname);
				mySqlCommandHelper.Execute();
				while (mySqlCommandHelper.HasResult())
				{
					int index = mySqlCommandHelper.GetInt("fdNum") - 1;
					UserItemDyeing value = new UserItemDyeing
					{
						DyeingPart = mySqlCommandHelper.GetByte("fdPart"),
						Color1 = Utility.StringToByteArray(mySqlCommandHelper.GetString("fdColor1")),
						Color2 = Utility.StringToByteArray(mySqlCommandHelper.GetString("fdColor2")),
						Color3 = Utility.StringToByteArray(mySqlCommandHelper.GetString("fdColor3"))
					};
					AvatarItemDyeing[index] = value;
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_itemdyeing_tbinfo Error: {0}", ex.Message);
			}
		}

		private static void getExp(Account User, short levelKind)
		{
			try
			{
				using (MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_getExp"))
				{
					mySqlCommandHelper.AddParamInt("pUserNum", User.UserNum);
					mySqlCommandHelper.AddParamShort("levelKind", levelKind);
					mySqlCommandHelper.ExecuteSingle();
					if (mySqlCommandHelper.HasResult() && levelKind == 1)
					{
						User.Exp = mySqlCommandHelper.GetLong("exp");
					}
				}
				User.EXPNeedUpdateFromDB = false;
			}
			catch (Exception ex)
			{
				Log.Error("usp_getExp Error: {0}", ex.Message);
			}
		}
	}
}
