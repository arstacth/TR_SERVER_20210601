using System;
using System.Collections.Generic;
using System.Linq;
using AgentServer.Database;
using AgentServer.Function;
using AgentServer.Network.Connections;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Item;
using LocalCommons.Network;
using Serilog;
using TRCommon;

namespace AgentServer.Packet
{
	public class ItemHandle
	{
		public static void Handle_GetCurrentAvatarInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			bool bRequestNickName = reader.ReadBoolean();
			currentAccount.AvatarItemDyeing.AddRange(Enumerable.Repeat(new UserItemDyeing(), 24));
			getCurrentAvatarInfo(currentAccount, bRequestNickName, last);
		}

		public static void Handle_GetAvatarItems(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			ushort charid = reader.ReadLEUInt16();
			int position = reader.ReadLEInt32();
			getCharacterAvatarItem(currentAccount, charid, position, last);
		}

		public static void Handle_GetActiveFuncItem(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			bool bExpiredCheck = reader.ReadBoolean();
			bool bLevelLimit = reader.ReadBoolean();
			getActiveFuncItem(currentAccount, -1, bExpiredCheck, bLevelLimit);
		}

		public static void Handle_GetActiveFuncItem_Position(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int position = reader.ReadLEInt32();
			getActiveFuncItem(currentAccount, position);
		}

		public static void Handle_GetActiveFuncItem_List(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			string text = string.Empty;
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadLEInt32();
				text += $"{num2},";
			}
			getActiveFuncItemOne(currentAccount, text);
		}

		public static void Handle_GetAvatarItemOne(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			string itemnums = $"{num},";
			if (getAvatarItemOne(currentAccount, itemnums, out var aItemInfo))
			{
				if (aItemInfo.m_count <= 0 && ShopItemTable.getItemDataFromItemDescNum(aItemInfo.m_iItemDescNum, out var itemData) && itemData.m_iType != 1)
				{
					currentAccount.activeItem.deleteItem(aItemInfo.m_iItemDescNum);
				}
				Client.SendAsync(new GET_AVATAR_ITEM_ONE_ACK(aItemInfo, last));
			}
		}

		public static void Handle_GetAvatarItemList(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			string text = string.Empty;
			List<int> list = new List<int>();
			for (int i = 0; i < num; i++)
			{
				int num2 = reader.ReadLEInt32();
				list.Add(num2);
				text += $"{num2},";
			}
			getAvatarItemAll(currentAccount, list, text, last);
		}

		public static bool checkInvalidItemDescNum(Account User, AvatarInfo userAvatarInfo, ref AvatarInfo originAvatarInfo, ref AvatarInfo transAvatarInfo, Dictionary<cpk_type, cpk_type> transKindNumMap, ref bool bWrongData)
		{
			try
			{
				if (ShopItemTable.getRealItemDataFromCPK(0, (ushort)10, originAvatarInfo.m_pet, out var itemData))
				{
					cpk_type cpk_type = 0;
					if (itemData.m_mapAttr.ContainsKey(147))
					{
						cpk_type = (int)itemData.m_mapAttr[147];
					}
					if ((int)cpk_type != 0 && (int)originAvatarInfo.m_character != (int)cpk_type)
					{
						bWrongData = true;
					}
				}
				if ((0 < (int)transAvatarInfo.m_character && 28 > (int)transAvatarInfo.m_character) || (200 < (int)transAvatarInfo.m_character && 223 > (int)transAvatarInfo.m_character))
				{
					cpk_type cpk_type2 = transAvatarInfo.m_nItemPartArry[0];
					CItemTransformInfo itemTransformInfo = new CItemTransformInfo();
					foreach (KeyValuePair<cpk_type, cpk_type> item in transKindNumMap)
					{
						if (!TransformItemManager.getItemTransformInfo((cpk_type2, item.Key, item.Value), ref itemTransformInfo))
						{
							bWrongData = true;
						}
						cpk_type cpk_type3 = NetCommonFunc.getAvatarPartsByItemPosition(item.Key);
						if (NetCommonFunc.isWearItemPosition(cpk_type3))
						{
							originAvatarInfo.setItemPart(cpk_type3, itemTransformInfo.m_iOriginKind);
						}
						int itemDescNumFromCPK = ShopItemTable.getItemDescNumFromCPK(cpk_type2, item.Key, itemTransformInfo.m_iOriginKind);
						if (User.userItemAttr.getItemAttr(itemDescNumFromCPK, out CItemAttr rAttr))
						{
							float num = rAttr.m_attr[136];
							if ((int)item.Value != (short)num)
							{
								bWrongData = true;
							}
						}
						else
						{
							bWrongData = true;
						}
					}
					User.checkAvatar(ref transAvatarInfo, ref originAvatarInfo);
					if (((2123 <= (int)userAvatarInfo.m_gTopBody && 2138 >= (int)userAvatarInfo.m_gTopBody) || (2123 <= (int)originAvatarInfo.m_gTopBody && 2138 >= (int)originAvatarInfo.m_gTopBody) || 20028 == (int)transAvatarInfo.m_gTopBody) && ((int)userAvatarInfo.m_head != 0 || (int)userAvatarInfo.m_downBody != 0 || (int)userAvatarInfo.m_foot != 0 || (int)originAvatarInfo.m_head != 0 || (int)originAvatarInfo.m_downBody != 0 || (int)originAvatarInfo.m_foot != 0))
					{
						bWrongData = true;
					}
					return true;
				}
			}
			catch (Exception ex)
			{
				Log.Error("checkInvalidItemDescNum: {0}", ex.ToString());
			}
			return false;
		}

		public static void getCurrentAvatarInfo(Account User, bool bRequestNickName, byte last)
		{
			bool flag = false;
			AdvancedAvatarInfo advancedAvatarInfo = new AdvancedAvatarInfo();
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_getCurrentAvatarInfo");
				mySqlCommandHelper.AddParamInt("usernum", User.UserNum);
				mySqlCommandHelper.ExecuteSingle();
				if (mySqlCommandHelper.HasResult())
				{
					if (mySqlCommandHelper.IsDBNull("character"))
					{
						advancedAvatarInfo.m_realAvatarInfo.m_character = 0;
					}
					else
					{
						mySqlCommandHelper.getResultAvatarInfo(ref advancedAvatarInfo.m_realAvatarInfo);
						mySqlCommandHelper.getResultAvatarInfo(ref advancedAvatarInfo.m_costumeAvatarInfo, bCostume: true);
						advancedAvatarInfo.m_bIsUseCostume = mySqlCommandHelper.GetBoolean("costumeMode");
					}
				}
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_getCurrentAvatarInfo Error: {0}", ex.ToString());
			}
			if (!flag)
			{
				User.Connection.SendAsync(new GET_AVATAR_FAIL_ACK(eServerResult.eServerResult_GET_AVATAR_FAILED_ACK, last));
				return;
			}
			if ((int)advancedAvatarInfo.getRealCharacter == 0)
			{
				User.Connection.SendAsync(new GET_AVATAR_FAIL_ACK(eServerResult.eServerResult_SELECT_START_CHARACTER_ACK, last));
				return;
			}
			if (User.Attribute == 1)
			{
				short num = advancedAvatarInfo.m_realAvatarInfo.m_topBody;
				advancedAvatarInfo.clear();
				advancedAvatarInfo.setCharacter(101);
				advancedAvatarInfo.m_realAvatarInfo.m_topBody = num;
			}
			User.checkEquipmentItem(bAvatarChanged: true, bDefaultAvatar: false, ref advancedAvatarInfo);
			User.setAvatarInfoAndSendTCP(advancedAvatarInfo, bRequestNickName);
		}

		public static void getActiveFuncItem(Account User, int position, bool bExpiredCheck = false, bool bLevelLimit = false, bool bEquipment = false)
		{
			List<NetItemInfo> list = new List<NetItemInfo>();
			bool flag = false;
			try
			{
				using (MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_getActiveFuncItem"))
				{
					mySqlCommandHelper.AddParamInt("usernum", User.UserNum);
					mySqlCommandHelper.AddParamInt("position", position);
					mySqlCommandHelper.AddParamInt("expiredcheck", bExpiredCheck ? 1 : 0);
					mySqlCommandHelper.Execute();
					while (mySqlCommandHelper.HasResult())
					{
						NetItemInfo netItemInfo = new NetItemInfo();
						netItemInfo.clear();
						netItemInfo.m_iItemDescNum = mySqlCommandHelper.GetInt("itemdescnum");
						netItemInfo.m_character = mySqlCommandHelper.GetInt("character");
						netItemInfo.m_position = mySqlCommandHelper.GetInt("position");
						netItemInfo.m_kind = mySqlCommandHelper.GetInt("kind");
						netItemInfo.m_tGot = mySqlCommandHelper.GetDateTime("gotDateTime", 0L);
						if (mySqlCommandHelper.IsDBNull("expireTime"))
						{
							netItemInfo.m_bHasExpireTime = false;
							netItemInfo.m_count = mySqlCommandHelper.GetInt("count");
						}
						else
						{
							netItemInfo.m_bHasExpireTime = true;
							netItemInfo.m_expireTime = mySqlCommandHelper.GetDateTime("expiretime", 0L);
							netItemInfo.m_count = mySqlCommandHelper.GetInt("count");
						}
						netItemInfo.m_bUsing = mySqlCommandHelper.GetBoolean("using");
						list.Add(netItemInfo);
					}
				}
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_getActiveFuncItem Error: {0}", ex.ToString());
			}
			if (flag)
			{
				if (position < 0)
				{
					User.onRecvActiveFuncItem(flag, list, bEquipment);
				}
				else
				{
					User.onRecvActiveFuncItem(flag, list, position);
				}
				User.modifyActiveItemForRoom();
			}
		}

		private static void getActiveFuncItemOne(Account User, string itemnums)
		{
			Dictionary<int, NetItemInfo> dictionary = new Dictionary<int, NetItemInfo>();
			bool flag = false;
			try
			{
				using (MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_getActiveFuncItemOne"))
				{
					mySqlCommandHelper.AddParamInt("usernum", User.UserNum);
					mySqlCommandHelper.AddParamVarString("itemnums", itemnums);
					mySqlCommandHelper.Execute();
					while (mySqlCommandHelper.HasResult())
					{
						NetItemInfo netItemInfo = new NetItemInfo();
						netItemInfo.clear();
						netItemInfo.m_iItemDescNum = mySqlCommandHelper.GetInt("itemdescnum");
						netItemInfo.m_character = mySqlCommandHelper.GetInt("character");
						netItemInfo.m_position = mySqlCommandHelper.GetInt("position");
						netItemInfo.m_kind = mySqlCommandHelper.GetInt("kind");
						netItemInfo.m_tGot = mySqlCommandHelper.GetDateTime("gotDateTime", 0L);
						if (mySqlCommandHelper.IsDBNull("expireTime"))
						{
							netItemInfo.m_bHasExpireTime = false;
							netItemInfo.m_count = mySqlCommandHelper.GetInt("count");
						}
						else
						{
							netItemInfo.m_bHasExpireTime = true;
							netItemInfo.m_expireTime = mySqlCommandHelper.GetDateTime("expiretime", 0L);
							netItemInfo.m_count = mySqlCommandHelper.GetInt("count");
						}
						netItemInfo.m_bUsing = mySqlCommandHelper.GetBoolean("using");
						dictionary[netItemInfo.m_iItemDescNum] = netItemInfo;
					}
				}
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_getActiveFuncItemOne Error: {0}", ex.ToString());
			}
			if (flag)
			{
				User.onRecvActiveFuncItem(flag, dictionary);
				User.modifyActiveItemForRoom();
			}
		}

		private static bool getAvatarItemOne(Account User, string itemnums, out NetItemInfo aItemInfo)
		{
			aItemInfo = new NetItemInfo();
			bool flag = false;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_getAvatarItem");
				mySqlCommandHelper.AddParamInt("usernum", User.UserNum);
				mySqlCommandHelper.AddParamVarString("itemnums", itemnums);
				mySqlCommandHelper.Execute();
				if (mySqlCommandHelper.HasResult())
				{
					aItemInfo.clear();
					aItemInfo.m_iItemDescNum = mySqlCommandHelper.GetInt("itemdescnum");
					aItemInfo.m_character = mySqlCommandHelper.GetInt("character");
					aItemInfo.m_position = mySqlCommandHelper.GetInt("position");
					aItemInfo.m_kind = mySqlCommandHelper.GetInt("kind");
					aItemInfo.m_count = mySqlCommandHelper.GetInt("count");
					aItemInfo.m_exp = mySqlCommandHelper.GetInt("exp");
					aItemInfo.m_bUsing = mySqlCommandHelper.GetBoolean("using");
					aItemInfo.m_tGot = mySqlCommandHelper.GetDateTime("gotDateTime", 0L);
					if (mySqlCommandHelper.IsDBNull("expireTime"))
					{
						aItemInfo.m_bHasExpireTime = false;
					}
					else
					{
						aItemInfo.m_bHasExpireTime = true;
						aItemInfo.m_expireTime = mySqlCommandHelper.GetDateTime("expiretime", 0L);
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_getAvatarItem Error: {0}", ex.ToString());
				return flag;
			}
		}

		private static void getAvatarItemAll(Account User, List<int> itemnum, string itemnums, byte last)
		{
			Dictionary<int, NetItemInfo> dictionary = new Dictionary<int, NetItemInfo>();
			List<NetItemInfo> list = new List<NetItemInfo>();
			bool flag = false;
			try
			{
				if (itemnum.Count > 500)
				{
					using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_getAvatarItem_all");
					mySqlCommandHelper.AddParamInt("usernum", User.UserNum);
					mySqlCommandHelper.Execute();
					while (mySqlCommandHelper.HasResult())
					{
						NetItemInfo netItemInfo = new NetItemInfo();
						netItemInfo.clear();
						netItemInfo.m_iItemDescNum = mySqlCommandHelper.GetInt("itemdescnum");
						if (itemnum.Contains(netItemInfo.m_iItemDescNum))
						{
							netItemInfo.m_character = mySqlCommandHelper.GetInt("character");
							netItemInfo.m_position = mySqlCommandHelper.GetInt("position");
							netItemInfo.m_kind = mySqlCommandHelper.GetInt("kind");
							netItemInfo.m_count = mySqlCommandHelper.GetInt("count");
							netItemInfo.m_exp = mySqlCommandHelper.GetInt("exp");
							netItemInfo.m_bUsing = mySqlCommandHelper.GetBoolean("using");
							netItemInfo.m_tGot = mySqlCommandHelper.GetDateTime("gotDateTime", 0L);
							if (mySqlCommandHelper.IsDBNull("expireTime"))
							{
								netItemInfo.m_bHasExpireTime = false;
							}
							else
							{
								netItemInfo.m_bHasExpireTime = true;
								netItemInfo.m_expireTime = mySqlCommandHelper.GetDateTime("expiretime", 0L);
							}
							list.Add(netItemInfo);
							if (mySqlCommandHelper.GetIntConvert("isActiveItem") > 0)
							{
								dictionary[netItemInfo.m_iItemDescNum] = netItemInfo;
							}
						}
					}
					flag = true;
				}
				else
				{
					using MySqlCommandHelper mySqlCommandHelper2 = new MySqlCommandHelper("usp_getAvatarItem");
					mySqlCommandHelper2.AddParamInt("usernum", User.UserNum);
					mySqlCommandHelper2.AddParamVarString("itemnums", itemnums);
					mySqlCommandHelper2.Execute();
					while (mySqlCommandHelper2.HasResult())
					{
						NetItemInfo netItemInfo2 = new NetItemInfo();
						netItemInfo2.m_iItemDescNum = mySqlCommandHelper2.GetInt("itemdescnum");
						netItemInfo2.m_character = mySqlCommandHelper2.GetInt("character");
						netItemInfo2.m_position = mySqlCommandHelper2.GetInt("position");
						netItemInfo2.m_kind = mySqlCommandHelper2.GetInt("kind");
						netItemInfo2.m_count = mySqlCommandHelper2.GetInt("count");
						netItemInfo2.m_exp = mySqlCommandHelper2.GetInt("exp");
						netItemInfo2.m_bUsing = mySqlCommandHelper2.GetBoolean("using");
						netItemInfo2.m_tGot = mySqlCommandHelper2.GetDateTime("gotDateTime", 0L);
						if (mySqlCommandHelper2.IsDBNull("expireTime"))
						{
							netItemInfo2.m_bHasExpireTime = false;
						}
						else
						{
							netItemInfo2.m_bHasExpireTime = true;
							netItemInfo2.m_expireTime = mySqlCommandHelper2.GetDateTime("expiretime", 0L);
						}
						list.Add(netItemInfo2);
						if (mySqlCommandHelper2.GetIntConvert("isActiveItem") > 0)
						{
							dictionary[netItemInfo2.m_iItemDescNum] = netItemInfo2;
						}
					}
					flag = true;
				}
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_getAvatarItem Error: {0}", ex.ToString());
			}
			if (!flag)
			{
				return;
			}
			User.activeItem.replaceItemInfoByItemNum(dictionary);
			List<List<NetItemInfo>> list2 = list.Split();
			byte b = 0;
			foreach (List<NetItemInfo> item in list2)
			{
				short startindex = (short)(1630 * b);
				b = (byte)(b + 1);
				byte remainpage = (byte)(list2.Count - b);
				User.Connection.SendAsync(new GET_AVATAR_ITEM_LIST_ACK(User, startindex, remainpage, item, last));
			}
		}

		private static void getCharacterAvatarItem(Account User, int charid, int position, byte last)
		{
			Dictionary<int, List<NetItemInfo>> dictionary = new Dictionary<int, List<NetItemInfo>>();
			if (!User.isRecvAllofCharItem)
			{
				charid = -1;
				position = -1;
			}
			bool flag = false;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_getCharacterAvatarItem");
				mySqlCommandHelper.AddParamInt("usernum", User.UserNum);
				mySqlCommandHelper.AddParamInt("pcharacter", charid);
				mySqlCommandHelper.AddParamInt("position", position);
				mySqlCommandHelper.Execute();
				while (mySqlCommandHelper.HasResult())
				{
					NetItemInfo netItemInfo = new NetItemInfo();
					netItemInfo.m_iItemDescNum = mySqlCommandHelper.GetInt("itemdescnum");
					netItemInfo.m_character = mySqlCommandHelper.GetInt("character");
					netItemInfo.m_position = mySqlCommandHelper.GetInt("position");
					netItemInfo.m_kind = mySqlCommandHelper.GetInt("kind");
					netItemInfo.m_count = mySqlCommandHelper.GetInt("count");
					netItemInfo.m_exp = mySqlCommandHelper.GetInt("exp");
					netItemInfo.m_bUsing = mySqlCommandHelper.GetBoolean("using");
					netItemInfo.m_tGot = mySqlCommandHelper.GetDateTime("gotDateTime", 0L);
					if (mySqlCommandHelper.IsDBNull("expireTime"))
					{
						netItemInfo.m_bHasExpireTime = false;
					}
					else
					{
						netItemInfo.m_bHasExpireTime = true;
						netItemInfo.m_expireTime = mySqlCommandHelper.GetDateTime("expiretime", 0L);
					}
					if (dictionary.ContainsKey(netItemInfo.m_character))
					{
						dictionary[netItemInfo.m_character].Add(netItemInfo);
						continue;
					}
					dictionary.Add(netItemInfo.m_character, new List<NetItemInfo> { netItemInfo });
				}
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_getCharacterAvatarItem Error: {0}", ex.ToString());
			}
			if (!flag)
			{
				return;
			}
			if (!User.isRecvAllofCharItem)
			{
				User.isRecvAllofCharItem = true;
			}
			if (dictionary.Count == 0)
			{
				User.Connection.SendAsync(new GET_AVATAR_ITEMS_ACK(charid, position, 0, 0, new List<NetItemInfo>(), last));
				return;
			}
			foreach (KeyValuePair<int, List<NetItemInfo>> item in dictionary)
			{
				List<List<NetItemInfo>> list = item.Value.Split();
				byte b = 0;
				if (list.Count == 0)
				{
					User.Connection.SendAsync(new GET_AVATAR_ITEMS_ACK(item.Key, position, 0, 0, new List<NetItemInfo>(), last));
					continue;
				}
				foreach (List<NetItemInfo> item2 in list)
				{
					short startindex = (short)(1630 * b);
					b = (byte)(b + 1);
					byte remainpage = (byte)(list.Count - b);
					User.Connection.SendAsync(new GET_AVATAR_ITEMS_ACK(item.Key, position, remainpage, startindex, item2, last));
				}
			}
		}
	}
}
