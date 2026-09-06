using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AgentServer.Network.Connections;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Shu;
using LocalCommons.Network;
using LocalCommons.Utilities;
using MySql.Data.MySqlClient;
using TRCommon;

namespace AgentServer.Packet
{
	public class ShuSystemHandle
	{
		public static void Handle_Shu_GetUserItemInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			int kind = reader.ReadLEInt32();
			Shu_GetUserItemByCharacter(currentAccount, kind, out var iteminfos);
			Client.SendAsync(new Shu_GetUserItemInfo(kind, iteminfos.ToList(), last));
		}

		public static void Handle_Shu_GetItemInfoByStr(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			reader.ReadLEInt32();
			int num = reader.ReadLEInt32();
			string text = string.Empty;
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadLEInt32();
				text += $"{num2},";
			}
			Shu_GetUserItemInfo(currentAccount, string.Empty, text, out var iteminfos);
			Client.SendAsync(new Shu_GetItemInfoStr(iteminfos.ToList(), last));
		}

		public static void Handle_Shu_Hatch(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			long num = reader.ReadLEInt64();
			int eggItemNum = reader.ReadLEInt32();
			int num2 = reader.ReadLEInt32();
			string text = string.Empty;
			string text2 = string.Empty;
			for (int i = 0; i < num2; i++)
			{
				int num3 = reader.ReadLEInt32();
				int num4 = reader.ReadLEInt32();
				text += $"{num3},";
				text2 += $"{num4},";
			}
			Shu_Hatch(currentAccount, num, eggItemNum, text, text2, out var infos);
			Client.SendAsync(new Shu_HatchOK(num, infos, last));
		}

		public static void Handle_Shu_ChangeCurrentShu(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			long num = reader.ReadLEInt64();
			ChangeCurrentShu(currentAccount, num, out var beforeCharacterItemID, out var infos);
			infos.shuavatars.TryGetValue(num, out var value);
			infos.shuchars.TryGetValue(num, out var value2);
			infos.shustatus.TryGetValue(num, out var value3);
			UpdateUserShuInfo(currentAccount, num, value, value2, value3);
			Client.SendAsync(new Shu_ChangeCurrentShu(beforeCharacterItemID, num, infos, last));
		}

		public static void Handle_Shu_ManagerAction(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			long num = reader.ReadLEInt64();
			int actionType = reader.ReadLEInt32();
			if (ManagerAction(currentAccount, num, actionType, out var infos))
			{
				if (currentAccount.CurrentShuID != -1 && infos.shustatus.TryGetValue(currentAccount.CurrentShuID, out var value))
				{
					currentAccount.UserShuInfo.UpdateEachstatusinfo(value);
				}
				if (infos.beforeLevel != infos.afterLevel)
				{
					Client.SendAsync(new Shu_LevelUP(num, infos.beforeLevel, infos.afterLevel, last));
				}
				Client.SendAsync(new Shu_ManagerAction(actionType, num, infos, last));
			}
		}

		public static void Handle_Shu_ChangeAvatarInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			long num = reader.ReadLEInt64();
			reader.ReadLEInt16();
			string text = string.Empty;
			string text2 = string.Empty;
			for (int i = 0; i < 6; i++)
			{
				long num2 = reader.ReadLEInt64();
				text += $"{i},";
				text2 += $"{num2},";
			}
			ChangeAvatarInfo(currentAccount, num, text, text2, out var infos);
			infos.shuavatars.TryGetValue(num, out var value);
			UpdateUserShuInfo(currentAccount, num, value, null, null);
			Client.SendAsync(new Shu_ChangeAvatarInfo(currentAccount.CurrentShuID, infos, last));
		}

		public static void Handle_Shu_ChangeName(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			long num = reader.ReadLEInt64();
			int num2 = reader.ReadLEInt16();
			if (num2 >= 4 && num2 <= 12)
			{
				string empty = string.Empty;
				empty = reader.ReadBig5StringSafe(num2);
				if (ChangeName(currentAccount, num, empty, out var outname))
				{
					Client.SendAsync(new Shu_ChangeNameOK(num, outname, last));
				}
				if (currentAccount.CurrentShuID == num)
				{
					currentAccount.UserShuInfo.ShuName = outname;
				}
			}
		}

		public static void Handle_Shu_ExploreCheck(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			byte zonenum = reader.ReadByte();
			ExploreCheck(currentAccount, zonenum, out var infos);
			Client.SendAsync(new Shu_ExploreCheck(infos, last));
		}

		public static void Handle_Shu_ExploreStart(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			byte zoneid = reader.ReadByte();
			long characterID = reader.ReadLEInt64();
			if (ExploreStart(currentAccount, zoneid, characterID, out var info))
			{
				Client.SendAsync(new Shu_ExploreStartOK(info, last));
			}
		}

		public static void Handle_Shu_ExploreStop(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			byte zoneid = reader.ReadByte();
			if (ExploreStop(currentAccount, zoneid, out var characterItemID))
			{
				Client.SendAsync(new Shu_ExploreStopOK(zoneid, characterItemID, last));
			}
		}

		public static void Handle_Shu_ExploreReward(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			byte zonenum = reader.ReadByte();
			if (ExploreReward(currentAccount, zonenum, out var characterItemID, out var infos))
			{
				Client.SendAsync(new Shu_ExploreReward(zonenum, characterItemID, infos, last));
			}
		}

		public static void Handle_Shu_GetGift(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			long num = reader.ReadLEInt64();
			if (GetGift(currentAccount, num, out var exp, out var infos))
			{
				Client.SendAsync(new Shu_GetGiftOK(num, exp, infos, last));
			}
		}

		public static void Handle_Shu_UseItem(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			reader.Offset += 8;
			long num = reader.ReadLEInt64();
			long num2 = reader.ReadLEInt64();
			int num3 = reader.ReadLEInt32();
			int num4 = reader.ReadLEInt32();
			cpk_type shuItemPosition = ShopItemTable.getShuItemPosition(num3);
			if ((int)shuItemPosition <= 0)
			{
				return;
			}
			if (UseItem(currentAccount, shuItemPosition, num, num2, num3, num4, out var infos))
			{
				if ((int)shuItemPosition == 2001)
				{
					if (infos.beforeLevel != infos.afterLevel)
					{
						Client.SendAsync(new Shu_LevelUP(num, infos.beforeLevel, infos.afterLevel, last));
					}
					infos.shustatus.TryGetValue(num, out var value);
					UpdateUserShuInfo(currentAccount, num, null, null, value);
				}
				Client.SendAsync(new Shu_UseItem(shuItemPosition, num, num2, num3, num4, infos, last));
			}
			else
			{
				Client.SendAsync(new Shu_UseItemFail(num, num2, num3, num4, last));
			}
		}

		public static void Shu_GetUserCharacterItemList(Account User, out DBShuInfo infos)
		{
			infos = new DBShuInfo();
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_shu_getUserCharacterItemList";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("characterItemID", MySqlDbType.Int64).Value = 0;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
			if (!mySqlDataReader.HasRows)
			{
				return;
			}
			while (mySqlDataReader.Read())
			{
				ShuItemInfo item3 = new ShuItemInfo
				{
					itemdescnum = Convert.ToInt32(mySqlDataReader["avatarItemNum"]),
					itemID = Convert.ToInt64(mySqlDataReader["itemID"]),
					gotDateTime = Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["gotDateTime"])),
					count = Convert.ToInt32(mySqlDataReader["count"]),
					state = Convert.ToInt32(mySqlDataReader["state"])
				};
				long key = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shuitems.AddOrUpdate(key, new List<ShuItemInfo> { item3 }, delegate(long k, List<ShuItemInfo> v)
				{
					v.Add(item3);
					return v;
				});
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuCharInfo value = new ShuCharInfo
				{
					avatarItemNum = Convert.ToInt32(mySqlDataReader["avatarItemNum"]),
					Name = mySqlDataReader["name"].ToString(),
					state = Convert.ToInt32(mySqlDataReader["state"]),
					MotionList = Convert.ToInt64(mySqlDataReader["motionList"]),
					PurchaseMotionList = Convert.ToInt64(mySqlDataReader["purchaseMotionList"])
				};
				long num = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shuchars.TryAdd(num, value);
				infos.characterItemID.Add(num);
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuAvatarInfo item2 = new ShuAvatarInfo
				{
					Position = Convert.ToInt32(mySqlDataReader["position"]),
					itemID = Convert.ToInt64(mySqlDataReader["itemID"]),
					avatarItemNum = Convert.ToInt32(mySqlDataReader["avatarItemNum"])
				};
				long key2 = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shuavatars.AddOrUpdate(key2, new List<ShuAvatarInfo> { item2 }, delegate(long k, List<ShuAvatarInfo> v)
				{
					v.Add(item2);
					return v;
				});
			}
			foreach (KeyValuePair<long, List<ShuAvatarInfo>> shuavatar in infos.shuavatars)
			{
				int i;
				for (i = 3; i < 6; i++)
				{
					if (!shuavatar.Value.Exists((ShuAvatarInfo e) => e.Position == i))
					{
						ShuAvatarInfo item4 = new ShuAvatarInfo
						{
							Position = i,
							itemID = -1L,
							avatarItemNum = -1
						};
						shuavatar.Value.Add(item4);
					}
				}
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuStatusInfo item = new ShuStatusInfo
				{
					statustype = Convert.ToInt32(mySqlDataReader["statusType"]),
					value = Convert.ToInt32(mySqlDataReader["value"])
				};
				long key3 = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shustatus.AddOrUpdate(key3, new List<ShuStatusInfo> { item }, delegate(long k, List<ShuStatusInfo> v)
				{
					v.Add(item);
					return v;
				});
			}
		}

		public static void Shu_UserStatusInfo(Account User, out ConcurrentDictionary<long, List<int>> shustatus)
		{
			shustatus = new ConcurrentDictionary<long, List<int>>();
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_shu_getUserStatusInfo";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("characterItemID", MySqlDbType.Int64).Value = 0;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
			if (!mySqlDataReader.HasRows)
			{
				return;
			}
			while (mySqlDataReader.Read())
			{
				long key = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				int value = Convert.ToInt32(mySqlDataReader["value"]);
				shustatus.AddOrUpdate(key, new List<int> { value }, delegate(long k, List<int> v)
				{
					v.Add(value);
					return v;
				});
			}
		}

		public static void Shu_CheckSatiety(Account User)
		{
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_shu_checkSatiety";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("resultValue", MySqlDbType.Int32).Value = 1;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
			if (mySqlDataReader.HasRows)
			{
				mySqlDataReader.Read();
				Convert.ToDateTime(mySqlDataReader["nextCheckTime"]);
			}
		}

		public static void UpdateUserShuInfo(Account User, long currentshuid, List<ShuAvatarInfo> avatarinfos, ShuCharInfo charinfo, List<ShuStatusInfo> statusinfo)
		{
			User.CurrentShuID = currentshuid;
			if (User.CurrentShuID != -1)
			{
				if (avatarinfos != null)
				{
					User.UserShuInfo.updateavatar(avatarinfos);
				}
				if (charinfo != null)
				{
					User.UserShuInfo.updatecharinfo(charinfo);
				}
				if (statusinfo != null)
				{
					User.UserShuInfo.updatestatusinfo(statusinfo);
				}
			}
		}

		public static void Shu_GetUserItemInfo(Account User, string strItemID, string strItemNums, out List<ShuItemInfo> iteminfos)
		{
			iteminfos = new List<ShuItemInfo>();
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_shu_getUserItemInfo";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("strItemID", MySqlDbType.VarString).Value = strItemID;
			mySqlCommand.Parameters.Add("strItemNums", MySqlDbType.VarString).Value = strItemNums;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
			if (mySqlDataReader.HasRows)
			{
				while (mySqlDataReader.Read())
				{
					ShuItemInfo item = new ShuItemInfo
					{
						itemdescnum = Convert.ToInt32(mySqlDataReader["avatarItemNum"]),
						itemID = Convert.ToInt64(mySqlDataReader["itemID"]),
						gotDateTime = Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["gotDateTime"])),
						count = Convert.ToInt32(mySqlDataReader["count"]),
						state = Convert.ToInt32(mySqlDataReader["state"])
					};
					iteminfos.Add(item);
				}
			}
		}

		private static void Shu_GetUserItemByCharacter(Account User, int kind, out ConcurrentBag<ShuItemInfo> iteminfos)
		{
			iteminfos = new ConcurrentBag<ShuItemInfo>();
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_shu_getUserItemByCharacter";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("kind", MySqlDbType.Int32).Value = kind;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
			if (mySqlDataReader.HasRows)
			{
				while (mySqlDataReader.Read())
				{
					ShuItemInfo item = new ShuItemInfo
					{
						itemdescnum = Convert.ToInt32(mySqlDataReader["avatarItemNum"]),
						itemID = Convert.ToInt64(mySqlDataReader["itemID"]),
						gotDateTime = Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["gotDateTime"])),
						count = Convert.ToInt32(mySqlDataReader["count"]),
						state = Convert.ToInt32(mySqlDataReader["state"])
					};
					iteminfos.Add(item);
				}
			}
		}

		private static void Shu_Hatch(Account User, long eggItemID, int eggItemNum, string strPosition, string strItemNum, out DBShuInfo infos)
		{
			infos = new DBShuInfo();
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_shu_hatch";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("eggItemID", MySqlDbType.Int64).Value = eggItemID;
			mySqlCommand.Parameters.Add("eggItemNum", MySqlDbType.Int32).Value = eggItemNum;
			mySqlCommand.Parameters.Add("strPosition", MySqlDbType.VarString).Value = strPosition;
			mySqlCommand.Parameters.Add("strItemNum", MySqlDbType.VarString).Value = strItemNum;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
			while (mySqlDataReader.Read())
			{
				ShuItemInfo eggitem = new ShuItemInfo
				{
					itemdescnum = 0,
					itemID = eggItemID,
					gotDateTime = (Convert.IsDBNull(mySqlDataReader["gotDateTime"]) ? 0 : Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["gotDateTime"]))),
					count = ((!Convert.IsDBNull(mySqlDataReader["count"])) ? Convert.ToInt32(mySqlDataReader["count"]) : 0),
					state = ((!Convert.IsDBNull(mySqlDataReader["state"])) ? Convert.ToInt32(mySqlDataReader["state"]) : 0)
				};
				long key = 0L;
				infos.shuitems.AddOrUpdate(key, new List<ShuItemInfo> { eggitem }, delegate(long k, List<ShuItemInfo> v)
				{
					v.Add(eggitem);
					return v;
				});
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuItemInfo item3 = new ShuItemInfo
				{
					itemdescnum = Convert.ToInt32(mySqlDataReader["avatarItemNum"]),
					itemID = Convert.ToInt64(mySqlDataReader["itemID"]),
					gotDateTime = Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["gotDateTime"])),
					count = Convert.ToInt32(mySqlDataReader["count"]),
					state = Convert.ToInt32(mySqlDataReader["state"])
				};
				long key2 = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shuitems.AddOrUpdate(key2, new List<ShuItemInfo> { item3 }, delegate(long k, List<ShuItemInfo> v)
				{
					v.Add(item3);
					return v;
				});
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuCharInfo value = new ShuCharInfo
				{
					avatarItemNum = Convert.ToInt32(mySqlDataReader["avatarItemNum"]),
					Name = mySqlDataReader["name"].ToString(),
					state = Convert.ToInt32(mySqlDataReader["state"]),
					MotionList = Convert.ToInt64(mySqlDataReader["motionList"]),
					PurchaseMotionList = Convert.ToInt64(mySqlDataReader["purchaseMotionList"])
				};
				long num = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shuchars.TryAdd(num, value);
				infos.characterItemID.Add(num);
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuAvatarInfo item2 = new ShuAvatarInfo
				{
					Position = Convert.ToInt32(mySqlDataReader["position"]),
					itemID = Convert.ToInt64(mySqlDataReader["itemID"]),
					avatarItemNum = Convert.ToInt32(mySqlDataReader["avatarItemNum"])
				};
				long key3 = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shuavatars.AddOrUpdate(key3, new List<ShuAvatarInfo> { item2 }, delegate(long k, List<ShuAvatarInfo> v)
				{
					v.Add(item2);
					return v;
				});
			}
			foreach (KeyValuePair<long, List<ShuAvatarInfo>> shuavatar in infos.shuavatars)
			{
				int i;
				for (i = 3; i < 6; i++)
				{
					if (!shuavatar.Value.Exists((ShuAvatarInfo e) => e.Position == i))
					{
						ShuAvatarInfo item4 = new ShuAvatarInfo
						{
							Position = i,
							itemID = -1L,
							avatarItemNum = -1
						};
						shuavatar.Value.Add(item4);
					}
				}
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuStatusInfo item = new ShuStatusInfo
				{
					statustype = Convert.ToInt32(mySqlDataReader["statusType"]),
					value = Convert.ToInt32(mySqlDataReader["value"])
				};
				long key4 = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shustatus.AddOrUpdate(key4, new List<ShuStatusInfo> { item }, delegate(long k, List<ShuStatusInfo> v)
				{
					v.Add(item);
					return v;
				});
			}
		}

		private static void ChangeCurrentShu(Account User, long characterItemID, out long beforeCharacterItemID, out DBShuInfo infos)
		{
			infos = new DBShuInfo();
			beforeCharacterItemID = -1L;
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_shu_changeCurrentShu";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("characterItemID", MySqlDbType.Int64).Value = characterItemID;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
			if (!mySqlDataReader.HasRows)
			{
				return;
			}
			while (mySqlDataReader.Read())
			{
				beforeCharacterItemID = Convert.ToInt64(mySqlDataReader["beforeCharacterItemID"]);
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuCharInfo value = new ShuCharInfo
				{
					avatarItemNum = Convert.ToInt32(mySqlDataReader["avatarItemNum"]),
					Name = mySqlDataReader["name"].ToString(),
					state = Convert.ToInt32(mySqlDataReader["state"]),
					MotionList = Convert.ToInt64(mySqlDataReader["motionList"]),
					PurchaseMotionList = Convert.ToInt64(mySqlDataReader["purchaseMotionList"])
				};
				long num = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shuchars.TryAdd(num, value);
				infos.characterItemID.Add(num);
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuAvatarInfo item2 = new ShuAvatarInfo
				{
					Position = Convert.ToInt32(mySqlDataReader["position"]),
					itemID = Convert.ToInt64(mySqlDataReader["itemID"]),
					avatarItemNum = Convert.ToInt32(mySqlDataReader["avatarItemNum"])
				};
				long key = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shuavatars.AddOrUpdate(key, new List<ShuAvatarInfo> { item2 }, delegate(long k, List<ShuAvatarInfo> v)
				{
					v.Add(item2);
					return v;
				});
			}
			foreach (KeyValuePair<long, List<ShuAvatarInfo>> shuavatar in infos.shuavatars)
			{
				int i;
				for (i = 3; i < 6; i++)
				{
					if (!shuavatar.Value.Exists((ShuAvatarInfo e) => e.Position == i))
					{
						ShuAvatarInfo item3 = new ShuAvatarInfo
						{
							Position = i,
							itemID = -1L,
							avatarItemNum = -1
						};
						shuavatar.Value.Add(item3);
					}
				}
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				ShuStatusInfo item = new ShuStatusInfo
				{
					statustype = Convert.ToInt32(mySqlDataReader["statusType"]),
					value = Convert.ToInt32(mySqlDataReader["value"])
				};
				long key2 = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.shustatus.AddOrUpdate(key2, new List<ShuStatusInfo> { item }, delegate(long k, List<ShuStatusInfo> v)
				{
					v.Add(item);
					return v;
				});
			}
		}

		private static bool ManagerAction(Account User, long characterItemID, int actionType, out DBShuActionInfo infos)
		{
			infos = new DBShuActionInfo();
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_shu_managerAction";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("characterItemID", MySqlDbType.Int64).Value = characterItemID;
				mySqlCommand.Parameters.Add("actionType", MySqlDbType.Int32).Value = actionType;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				if (mySqlDataReader.HasRows)
				{
					while (mySqlDataReader.Read())
					{
						ShuActionResultInfo item2 = new ShuActionResultInfo
						{
							statusType = Convert.ToInt32(mySqlDataReader["statusType"]),
							giveValue = Convert.ToInt32(mySqlDataReader["giveValue"])
						};
						infos.remainMP = Convert.ToInt32(mySqlDataReader["remainMP"]);
						infos.ActionResult.Add(item2);
					}
					mySqlDataReader.NextResult();
					while (mySqlDataReader.Read())
					{
						ShuStatusInfo item = new ShuStatusInfo
						{
							statustype = Convert.ToInt32(mySqlDataReader["statusType"]),
							value = Convert.ToInt32(mySqlDataReader["value"])
						};
						long key = Convert.ToInt64(mySqlDataReader["characterItemID"]);
						infos.shustatus.AddOrUpdate(key, new List<ShuStatusInfo> { item }, delegate(long k, List<ShuStatusInfo> v)
						{
							v.Add(item);
							return v;
						});
					}
					mySqlDataReader.NextResult();
					while (mySqlDataReader.Read())
					{
						infos.beforeLevel = Convert.ToInt32(mySqlDataReader["beforeLevel"]);
						infos.afterLevel = Convert.ToInt32(mySqlDataReader["afterLevel"]);
					}
					return true;
				}
			}
			return false;
		}

		private static void ChangeAvatarInfo(Account User, long characterItemID, string strPosition, string strItemID, out DBShuChangeAVInfo infos)
		{
			infos = new DBShuChangeAVInfo();
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_shu_changeAvatarInfo";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("characterItemID", MySqlDbType.Int64).Value = characterItemID;
			mySqlCommand.Parameters.Add("strPosition", MySqlDbType.VarString).Value = strPosition;
			mySqlCommand.Parameters.Add("strItemID", MySqlDbType.VarString).Value = strItemID;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
			if (!mySqlDataReader.HasRows)
			{
				return;
			}
			while (mySqlDataReader.Read())
			{
				ShuAvatarState item2 = new ShuAvatarState
				{
					itemID = Convert.ToInt32(mySqlDataReader["itemID"]),
					state = Convert.ToInt32(mySqlDataReader["state"])
				};
				infos.AvatarState.Add(item2);
			}
			mySqlDataReader.NextResult();
			while (mySqlDataReader.Read())
			{
				long itemid = Convert.ToInt64(mySqlDataReader["itemID"]);
				ShuAvatarInfo item = new ShuAvatarInfo
				{
					Position = Convert.ToInt32(mySqlDataReader["position"]),
					itemID = itemid,
					avatarItemNum = Convert.ToInt32(mySqlDataReader["avatarItemNum"])
				};
				long key = Convert.ToInt64(mySqlDataReader["characterItemID"]);
				infos.AvatarState.Find((ShuAvatarState f) => f.itemID == itemid);
				infos.shuavatars.AddOrUpdate(key, new List<ShuAvatarInfo> { item }, delegate(long k, List<ShuAvatarInfo> v)
				{
					v.Add(item);
					return v;
				});
			}
			foreach (KeyValuePair<long, List<ShuAvatarInfo>> shuavatar in infos.shuavatars)
			{
				int i;
				for (i = 3; i < 6; i++)
				{
					if (!shuavatar.Value.Exists((ShuAvatarInfo e) => e.Position == i))
					{
						ShuAvatarInfo item3 = new ShuAvatarInfo
						{
							Position = i,
							itemID = -1L,
							avatarItemNum = -1
						};
						shuavatar.Value.Add(item3);
					}
				}
			}
		}

		private static bool ChangeName(Account User, long characterItemID, string name, out string outname)
		{
			outname = string.Empty;
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_shu_changeName";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("characterItemID", MySqlDbType.Int64).Value = characterItemID;
				mySqlCommand.Parameters.Add("changeName", MySqlDbType.VarString).Value = name;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				if (mySqlDataReader.HasRows)
				{
					mySqlDataReader.Read();
					outname = mySqlDataReader["changeName"].ToString();
					return true;
				}
			}
			return false;
		}

		private static bool ExploreStart(Account User, byte zoneid, long characterID, out ExploreInfo info)
		{
			info = new ExploreInfo();
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_shu_exploreStart";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("zoneNum", MySqlDbType.Int16).Value = zoneid;
				mySqlCommand.Parameters.Add("characterItemID", MySqlDbType.Int64).Value = characterID;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				if (mySqlDataReader.HasRows)
				{
					mySqlDataReader.Read();
					info.zoneNum = Convert.ToByte(mySqlDataReader["zoneNum"]);
					info.characterItemID = Convert.ToInt64(mySqlDataReader["characterItemID"]);
					info.endDateTime = Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["endDateTime"]));
					return true;
				}
			}
			return false;
		}

		private static bool ExploreStop(Account User, byte zoneid, out long characterItemID)
		{
			characterItemID = 0L;
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_shu_exploreStop";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("zoneNum", MySqlDbType.Int16).Value = zoneid;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				if (mySqlDataReader.HasRows)
				{
					mySqlDataReader.Read();
					characterItemID = Convert.ToInt64(mySqlDataReader["characterItemID"]);
					return true;
				}
			}
			return false;
		}

		private static void ExploreCheck(Account User, byte zonenum, out List<ExploreInfo> infos)
		{
			infos = new List<ExploreInfo>();
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_shu_exploreCheck";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("zoneNum", MySqlDbType.Int16).Value = zonenum;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
			if (mySqlDataReader.HasRows)
			{
				while (mySqlDataReader.Read())
				{
					ExploreInfo item = new ExploreInfo
					{
						zoneNum = Convert.ToByte(mySqlDataReader["zoneNum"]),
						characterItemID = Convert.ToInt64(mySqlDataReader["characterItemID"]),
						endDateTime = Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["endDateTime"]))
					};
					infos.Add(item);
				}
			}
		}

		private static bool ExploreReward(Account User, byte zonenum, out long characterItemID, out List<ShuRewardInfo> infos)
		{
			characterItemID = -1L;
			infos = new List<ShuRewardInfo>();
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_shu_exploreReward";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("zoneNum", MySqlDbType.Int16).Value = zonenum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				if (mySqlDataReader.HasRows)
				{
					while (mySqlDataReader.Read())
					{
						ShuRewardInfo item = new ShuRewardInfo
						{
							rewardType = Convert.ToInt32(mySqlDataReader["rewardType"]),
							rewardItem = Convert.ToInt32(mySqlDataReader["rewardItem"]),
							rewardCount = Convert.ToInt32(mySqlDataReader["rewardCount"])
						};
						characterItemID = Convert.ToInt64(mySqlDataReader["characterItemID"]);
						infos.Add(item);
					}
					return true;
				}
			}
			return false;
		}

		private static bool GetGift(Account User, long characterItemID, out int exp, out List<ShuRewardInfo> infos)
		{
			exp = -1;
			infos = new List<ShuRewardInfo>();
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_shu_gift";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("characterItemID", MySqlDbType.Int64).Value = characterItemID;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				if (mySqlDataReader.HasRows)
				{
					while (mySqlDataReader.Read())
					{
						ShuRewardInfo item = new ShuRewardInfo
						{
							rewardType = Convert.ToInt32(mySqlDataReader["rewardType"]),
							rewardItem = Convert.ToInt32(mySqlDataReader["rewardItem"]),
							rewardCount = Convert.ToInt32(mySqlDataReader["rewardCount"])
						};
						exp = Convert.ToInt32(mySqlDataReader["exp"]);
						infos.Add(item);
					}
					return true;
				}
			}
			return false;
		}

		private static bool UseItem(Account User, int position, long characterItemID, long itemID, int itemNum, int useCount, out DBShuUseItemInfo infos)
		{
			infos = new DBShuUseItemInfo();
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_shu_useItem";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("characterItemID", MySqlDbType.Int64).Value = characterItemID;
				mySqlCommand.Parameters.Add("itemID", MySqlDbType.Int64).Value = itemID;
				mySqlCommand.Parameters.Add("itemNum", MySqlDbType.Int32).Value = itemNum;
				mySqlCommand.Parameters.Add("useCount", MySqlDbType.Int32).Value = useCount;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					ShuItemInfo item4 = new ShuItemInfo
					{
						itemdescnum = Convert.ToInt32(mySqlDataReader["avatarItemNum"]),
						itemID = Convert.ToInt64(mySqlDataReader["itemID"]),
						gotDateTime = Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["gotDateTime"])),
						count = Convert.ToInt32(mySqlDataReader["count"]),
						state = Convert.ToInt32(mySqlDataReader["state"])
					};
					infos.ItemInfos.Add(item4);
				}
				mySqlDataReader.NextResult();
				if (mySqlDataReader.HasRows)
				{
					if (position == 2001)
					{
						while (mySqlDataReader.Read())
						{
							infos.beforeLevel = Convert.ToInt32(mySqlDataReader["beforeLevel"]);
							infos.afterLevel = Convert.ToInt32(mySqlDataReader["afterLevel"]);
						}
						mySqlDataReader.NextResult();
						while (mySqlDataReader.Read())
						{
							ShuStatusInfo item3 = new ShuStatusInfo
							{
								statustype = Convert.ToInt32(mySqlDataReader["statusType"]),
								value = Convert.ToInt32(mySqlDataReader["value"])
							};
							long key = Convert.ToInt64(mySqlDataReader["characterItemID"]);
							infos.shustatus.AddOrUpdate(key, new List<ShuStatusInfo> { item3 }, delegate(long k, List<ShuStatusInfo> v)
							{
								v.Add(item3);
								return v;
							});
						}
					}
					else if (position == 2002)
					{
						while (mySqlDataReader.Read())
						{
							infos.remainMP = Convert.ToInt32(mySqlDataReader["remainMP"]);
						}
					}
					else if (position == 2003)
					{
						while (mySqlDataReader.Read())
						{
							ShuCharInfo value = new ShuCharInfo
							{
								avatarItemNum = Convert.ToInt32(mySqlDataReader["avatarItemNum"]),
								Name = mySqlDataReader["name"].ToString(),
								state = Convert.ToInt32(mySqlDataReader["state"]),
								MotionList = Convert.ToInt64(mySqlDataReader["motionList"]),
								PurchaseMotionList = Convert.ToInt64(mySqlDataReader["purchaseMotionList"])
							};
							long num = Convert.ToInt64(mySqlDataReader["characterItemID"]);
							infos.shuchars.TryAdd(num, value);
							infos.characterItemID.Add(num);
						}
						mySqlDataReader.NextResult();
						while (mySqlDataReader.Read())
						{
							ShuAvatarInfo item2 = new ShuAvatarInfo
							{
								Position = Convert.ToInt32(mySqlDataReader["position"]),
								itemID = Convert.ToInt64(mySqlDataReader["itemID"]),
								avatarItemNum = Convert.ToInt32(mySqlDataReader["avatarItemNum"])
							};
							long key2 = Convert.ToInt64(mySqlDataReader["characterItemID"]);
							infos.shuavatars.AddOrUpdate(key2, new List<ShuAvatarInfo> { item2 }, delegate(long k, List<ShuAvatarInfo> v)
							{
								v.Add(item2);
								return v;
							});
						}
						foreach (KeyValuePair<long, List<ShuAvatarInfo>> shuavatar in infos.shuavatars)
						{
							int i;
							for (i = 3; i < 6; i++)
							{
								if (!shuavatar.Value.Exists((ShuAvatarInfo e) => e.Position == i))
								{
									ShuAvatarInfo item5 = new ShuAvatarInfo
									{
										Position = i,
										itemID = -1L,
										avatarItemNum = -1
									};
									shuavatar.Value.Add(item5);
								}
							}
						}
						mySqlDataReader.NextResult();
						while (mySqlDataReader.Read())
						{
							ShuStatusInfo item = new ShuStatusInfo
							{
								statustype = Convert.ToInt32(mySqlDataReader["statusType"]),
								value = Convert.ToInt32(mySqlDataReader["value"])
							};
							long key3 = Convert.ToInt64(mySqlDataReader["characterItemID"]);
							infos.shustatus.AddOrUpdate(key3, new List<ShuStatusInfo> { item }, delegate(long k, List<ShuStatusInfo> v)
							{
								v.Add(item);
								return v;
							});
						}
					}
					return true;
				}
			}
			return false;
		}
	}
}
