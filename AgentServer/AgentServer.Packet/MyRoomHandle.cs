using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AgentServer.Database;
using AgentServer.Holders;
using AgentServer.Network.Connections;
using AgentServer.Packet.RoomServer;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Item;
using LocalCommons.Network;
using LocalCommons.Utilities;
using MySql.Data.MySqlClient;
using Serilog;
using TRCommon;

namespace AgentServer.Packet
{
	public class MyRoomHandle
	{
		public static void Handle_MyRoomGetCharacterList(ClientConnection Client, byte last)
		{
			Client.SendAsync(new MyRoom_GetCharacterList(Client.CurrentAccount, last));
		}

		public static void Handle_MyRoomGetMyCards(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			short num = reader.ReadLEInt16();
			string text = string.Empty;
			if (num > 0)
			{
				text = reader.ReadBig5StringSafe(num);
			}
			bool flag = !string.IsNullOrEmpty(text);
			AlchemistHandle.GetMyAlchemistCards(currentAccount.UserNum, text, out var cards);
			Client.SendAsync(new MyRoom_GetMyAlchemistCards(flag, cards, last));
		}

		public static void Handle_MyRoomGetCharacterSetting(ClientConnection Client, PacketReader reader, byte last)
		{
			int characterKind = reader.ReadLEUInt16();
			if (myRoomGetCharacterSetting(Client.CurrentAccount, characterKind, out var m_realAvatarInfo))
			{
				Client.SendAsync(new MyRoom_GetCharacterSetting_ACK(m_realAvatarInfo, last));
			}
			else
			{
				Client.SendAsync(new MyRoom_GetCharacterSetting_Fail_ACK(last));
			}
		}

		public static void Handle_SaveCharSetting(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			bool bWrongData = false;
			long num = reader.ReadLEInt64();
			long num2 = Utility.CurrentTimeMilliseconds();
			long num3 = 0L;
			num3 = ((num2 <= num) ? (num - num2) : (num2 - num));
			if (300000 < num3)
			{
				Client.SendAsync(new MyroomSetCharSettingFail(last));
				bWrongData = true;
			}
			AdvancedAvatarInfo advancedAvatarInfo = new AdvancedAvatarInfo();
			AdvancedAvatarInfo advancedAvatarInfo2 = new AdvancedAvatarInfo();
			for (int i = 0; i < 15; i++)
			{
				advancedAvatarInfo2.m_realAvatarInfo.m_nItemPartArry[i] = reader.ReadLEUInt16();
			}
			for (int j = 0; j < 7; j++)
			{
				advancedAvatarInfo2.m_realAvatarInfo.m_nGameAccArry[j] = reader.ReadLEUInt16();
			}
			for (int k = 0; k < 1; k++)
			{
				advancedAvatarInfo2.m_realAvatarInfo.m_nEFItemArry[k] = reader.ReadLEUInt16();
			}
			reader.Offset += 120;
			for (int l = 0; l < 15; l++)
			{
				advancedAvatarInfo2.m_costumeAvatarInfo.m_nItemPartArry[l] = reader.ReadLEUInt16();
			}
			for (int m = 0; m < 7; m++)
			{
				advancedAvatarInfo2.m_costumeAvatarInfo.m_nGameAccArry[m] = reader.ReadLEUInt16();
			}
			for (int n = 0; n < 1; n++)
			{
				advancedAvatarInfo2.m_costumeAvatarInfo.m_nEFItemArry[n] = reader.ReadLEUInt16();
			}
			reader.Offset += 120;
			advancedAvatarInfo2.m_bIsUseCostume = reader.ReadBoolean();
			reader.Offset++;
			bool bChangeRealAvatar = reader.ReadBoolean();
			bool bChangeCostumeAvatar = reader.ReadBoolean();
			bool bChangeAvatarMode = reader.ReadBoolean();
			int num4 = reader.ReadLEInt32();
			Dictionary<cpk_type, cpk_type> dictionary = new Dictionary<cpk_type, cpk_type>();
			for (int num5 = 0; num5 < num4; num5++)
			{
				cpk_type cpk_type = reader.ReadUInt16();
				cpk_type value = reader.ReadUInt16();
				if (advancedAvatarInfo2.m_realAvatarInfo.m_nItemPartArry[cpk_type] != 0)
				{
					dictionary.Add(cpk_type, value);
				}
			}
			num4 = 0;
			Dictionary<cpk_type, cpk_type> dictionary2 = new Dictionary<cpk_type, cpk_type>();
			num4 = reader.ReadLEInt32();
			for (int num6 = 0; num6 < num4; num6++)
			{
				cpk_type cpk_type2 = reader.ReadUInt16();
				cpk_type value2 = reader.ReadUInt16();
				if (advancedAvatarInfo2.m_costumeAvatarInfo.m_nItemPartArry[cpk_type2] != 0)
				{
					dictionary2.Add(cpk_type2, value2);
				}
			}
			if (!advancedAvatarInfo2.isValidCharacter)
			{
				Log.Warning("{0} - Not Invalid AdvancedAvatar Character Setting!!.", currentAccount.UserID);
				Client.SendAsync(new MyroomSetCharSettingFail(last));
				bWrongData = true;
			}
			advancedAvatarInfo = advancedAvatarInfo2.ShallowCopy();
			if (ItemHandle.checkInvalidItemDescNum(currentAccount, currentAccount.advancedAvatarInfo.m_realAvatarInfo, ref advancedAvatarInfo.m_realAvatarInfo, ref advancedAvatarInfo2.m_realAvatarInfo, dictionary, ref bWrongData) && ItemHandle.checkInvalidItemDescNum(currentAccount, currentAccount.advancedAvatarInfo.m_costumeAvatarInfo, ref advancedAvatarInfo.m_costumeAvatarInfo, ref advancedAvatarInfo2.m_costumeAvatarInfo, dictionary2, ref bWrongData))
			{
				if (bWrongData)
				{
					AdvancedAvatarInfo advancedAvatarInfo3 = new AdvancedAvatarInfo();
					advancedAvatarInfo3.setCharacter(advancedAvatarInfo2.getRealCharacter);
					advancedAvatarInfo = advancedAvatarInfo3;
					advancedAvatarInfo2 = advancedAvatarInfo3;
				}
				if (MyRoomSetCharacterSetting(currentAccount.UserNum, advancedAvatarInfo, advancedAvatarInfo2, bChangeRealAvatar, bChangeCostumeAvatar, bChangeAvatarMode))
				{
					Client.SendAsync(new MyroomSetCharSettingOK(last));
				}
				else
				{
					Client.SendAsync(new MyroomSetCharSettingFail(last));
				}
			}
			else
			{
				Log.Warning("{0} - save character setting {1} character failed!!.", currentAccount.UserID, (ushort)advancedAvatarInfo2.m_realAvatarInfo.m_character);
				Client.SendAsync(new MyroomSetCharSettingFail(last));
			}
		}

		public static void Handle_SaveDefaultCharacter(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			ushort charid = reader.ReadLEUInt16();
			if (myRoomSetDefaultCharacter(currentAccount, charid))
			{
				Client.SendAsync(new Myroom_SaveDefaultChar(last));
				currentAccount.getAttrs();
			}
			else
			{
				Client.SendAsync(new Myroom_SaveDefaultCharFail(last));
			}
		}

		public static void Handle_ItemMsgPop(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			if (num == 0)
			{
				Client.SendAsync(new eServer_GET_ITEMMSG_ACK(currentAccount, num, last));
			}
		}

		public static void Handle_RepairItem(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int itemnum = reader.ReadLEInt32();
			int repairitemnum = reader.ReadLEInt32();
			if (RepairItem(currentAccount, itemnum, repairitemnum) == 0)
			{
				Client.SendAsync(new Myroom_RepairItemOK(itemnum, repairitemnum, last));
			}
		}

		public static void Handle_PetRebirth(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int petitemnum = reader.ReadLEInt32();
			int rebirthitemnum = reader.ReadLEInt32();
			myRoomPetRebirth(currentAccount.UserNum, petitemnum, rebirthitemnum, out var PetItemlist);
			Client.SendAsync(new Myroom_PetRebirth_New(PetItemlist, rebirthitemnum, last));
		}

		public static void Handle_FeedPet(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			int petitemnum = reader.ReadLEInt32();
			int num2 = reader.ReadLEInt32();
			if (num2 < 1)
			{
				return;
			}
			if (!ShopItemTable.getItemQueryFromItemDescNum(num, out var query) || query.Item1 != 0 || query.Item2 != 107)
			{
				Log.Error("Not pet feed item");
				return;
			}
			int num3 = 0;
			int num4 = 0;
			num3 = (int)ItemAttrTable.getItemAttrFromItemDescNum(num, eItemAttr.eItemAttr_usePetExp);
			num4 = (int)ItemAttrTable.getItemAttrFromItemDescNum(num, eItemAttr.eItemAttr_usePetDays);
			if ((int)ItemAttrTable.getItemAttrFromItemDescNum(num, eItemAttr.eItemAttr_PetFeedType) == 1)
			{
				if (FeedPet(currentAccount, petitemnum, num, num4, num3, num2) == 0)
				{
					Client.SendAsync(new Myroom_FeedPetOK(petitemnum, num, last));
				}
			}
			else if (FeedAllPet(currentAccount, num, num4, num3, num2) == 0)
			{
				Client.SendAsync(new Myroom_FeedPetOK(petitemnum, num, last));
			}
		}

		public static void Handle_PetUpgrade(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			if (!ShopItemTable.getItemQueryFromItemDescNum(num, out var query))
			{
				Log.Error("Requested bad Pet upgrade. cannot find item. itemdescnum : {0}", num);
				return;
			}
			if (query.Item2 != 10 && query.Item2 != 117)
			{
				Log.Error("Requested bad Pet upgrade. position is not 10. itemdescnum : {0}", num);
				return;
			}
			int num2 = query.Item3 % 10;
			if (num2 <= 0 || num2 >= 3)
			{
				return;
			}
			if (!ItemHolder.PetMaxEXP.TryGetValue(query.Item3, out var value))
			{
				switch (num2)
				{
				case 1:
					value = 50000;
					break;
				case 2:
					value = 100000;
					break;
				case 3:
					value = 1000000;
					break;
				}
			}
			if (PetUpgrade(currentAccount, num, value) == 0)
			{
				Client.SendAsync(new Myroom_PetUpgradeOK(currentAccount, last));
			}
		}

		public static void Handle_FFCF0100(ClientConnection Client, PacketReader reader, byte last)
		{
			bool flag = false;
			flag = reader.ReadBoolean();
			Client.SendAsync(new Myroom_FFCF0100(flag, last));
		}

		public static void Handle_GetGiftList(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			short startindex = reader.ReadLEInt16();
			short lastindex = reader.ReadLEInt16();
			ShopHandle.shopGetGiftAcceptWaitList(currentAccount.UserNum, startindex, lastindex, out var GiftList, out var Itemcount);
			Client.SendAsync(new Myroom_GetGiftList_New(startindex, lastindex, GiftList, Itemcount, last));
		}

		public static void Handle_AcceptGift(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int fixedLength = reader.ReadLEInt16();
			string empty = string.Empty;
			empty = $"{reader.ReadBig5StringSafe(fixedLength)},";
			if (!new Regex("^[\\d,]+$").IsMatch(empty))
			{
				return;
			}
			int level = currentAccount.Level;
			AcceptGiftEx(currentAccount, empty, out var itemList);
			if (itemList.Count > 0)
			{
				Client.SendAsync(new Myroom_AcceptGiftOK(itemList, last));
				if (LobbyHandle.LevelUPCheck(currentAccount, level))
				{
					Client.SendAsync(new UserLevelUPEXPInfo(1, currentAccount.Level, currentAccount.Exp, last));
				}
			}
			else
			{
				Client.SendAsync(new Myroom_AcceptGiftFail(last));
			}
		}

		public static void Handle_MyroomGetUserItemAttr(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			bool bStrengthenGetMyItemLoad = reader.ReadBoolean();
			LoginTrafficManager.getUserItemAttr(currentAccount, bStrengthenGetMyItemLoad);
		}

		public static void Handle_GetStorageItemList(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			switch (reader.ReadLEInt32())
			{
			case 0:
			{
				ShopHandle.storage_getKeepingItemList(currentAccount.UserNum, out var KeepList);
				Client.SendAsync(new Myroom_GetStorageKeepingList(KeepList, last));
				break;
			}
			case 1:
			{
				ShopHandle.storage_getGiftList(currentAccount.UserNum, out var GiftList);
				Client.SendAsync(new Myroom_GetStorageGiftList(GiftList, last));
				break;
			}
			}
		}

		public static void Handle_StorageItemGift(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int fixedLength = reader.ReadLEInt16();
			string text = reader.ReadBig5StringSafe(fixedLength);
			int num = reader.ReadLEInt16();
			string msg = string.Empty;
			if (num > 0)
			{
				msg = reader.ReadBig5StringSafe(num);
			}
			long uniqueNum = reader.ReadLEInt64();
			if (currentAccount.NickName == text)
			{
				Client.SendAsync(new Myroom_StorageGiftACK(currentAccount, 4, uniqueNum, text, last));
				return;
			}
			byte error = StorageGift(currentAccount, text, msg, uniqueNum);
			Client.SendAsync(new Myroom_StorageGiftACK(currentAccount, error, uniqueNum, text, last));
		}

		public static void Handle_StorageItemReceive(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int type = reader.ReadLEInt32();
			int fixedLength = reader.ReadLEInt16();
			string empty = string.Empty;
			empty = $"{reader.ReadBig5StringSafe(fixedLength)},";
			if (new Regex("^[\\d,]+$").IsMatch(empty))
			{
				StorageReceiveEx(currentAccount, type, empty, out var itemList);
				if (itemList.Count > 0)
				{
					Client.SendAsync(new Myroom_StorageReceiveOK(type, itemList, last));
				}
			}
		}

		public static void Handle_ItemOnOff(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			bool flag = reader.ReadBoolean();
			if (flag && num == 58018 && !currentAccount.ChangedTalesBook)
			{
				currentAccount.ChangedTalesBook = true;
			}
			else
			{
				ItemOnOff(currentAccount, num, flag, last);
			}
		}

		public static void Handle_UseLuckyBag(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int itemnum = reader.ReadLEInt32();
			byte b = reader.ReadByte();
			int level = currentAccount.Level;
			if (b > 10)
			{
				b = 10;
			}
			ConcurrentDictionary<int, byte> itemlist;
			if (!ServerStatus.enableUseLuckyBag)
			{
				Client.SendAsync(new Myroom_UseLuckyBagFail(itemnum, last));
			}
			else if (UseLuckyBag(currentAccount, itemnum, b, out itemlist) && itemlist.Count > 0)
			{
				Client.SendAsync(new Myroom_UseLuckyBagOK(itemnum, b, itemlist, last));
				ItemHolder.ShuRewardCheck(currentAccount, itemlist.Keys.ToList());
				if (LobbyHandle.LevelUPCheck(currentAccount, level))
				{
					Client.SendAsync(new UserLevelUPEXPInfo(1, currentAccount.Level, currentAccount.Exp, last));
				}
			}
			else
			{
				Client.SendAsync(new Myroom_UseLuckyBagFail(itemnum, last));
			}
		}

		public static void Handle_SetSlotItemSetting(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			AvatarInfo avatarInfo = default(AvatarInfo);
			for (int i = 0; i < 15; i++)
			{
				avatarInfo.m_nItemPartArry[i] = reader.ReadLEUInt16();
			}
			reader.Offset += 136;
			int slotNum = reader.ReadLEInt32();
			int num = reader.ReadLEInt16();
			string text = string.Empty;
			if (num > 0)
			{
				text = reader.ReadBig5StringSafe(num);
			}
			Regex regex = new Regex("[~`!@#$%^&*()+=|\\\\{}':;.,<>/?[\\]\"_-]");
			int byteCount = Encoding.Default.GetByteCount(text);
			bool flag = false;
			int err = 0;
			if (byteCount > 18 || regex.IsMatch(text))
			{
				err = 3;
			}
			else
			{
				flag = MyRoomSetSlotItemSetting(currentAccount.UserNum, slotNum, text, avatarInfo, out err);
			}
			if (flag)
			{
				Client.SendAsync(new Myroom_SetSlotItemSettingOK(slotNum, text, avatarInfo, last));
			}
			else
			{
				Client.SendAsync(new Myroom_SetSlotItemSettingFail(err, last));
			}
		}

		public static void Handle_GetUserSlotInfo(ClientConnection Client, byte last)
		{
			MyRoomGetUserSlotInfo(Client.CurrentAccount.UserNum, out var m_vSlotInfo);
			Client.SendAsync(new Myroom_GetSlotInfoOK(m_vSlotInfo, last));
		}

		public static void Handle_GetFavoriteList(ClientConnection Client, byte last)
		{
			if (GetFavorite(Client.CurrentAccount.UserNum, out var itemlist))
			{
				Client.SendAsync(new Myroom_FavoriteList(itemlist, last));
			}
		}

		public static void Handle_AddFavorite(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			if (currentAccount.activeItem.HasItem(num) && ModifyFavorite(currentAccount.UserNum, num, 1))
			{
				Client.SendAsync(new Myroom_AddFavorite(num, last));
			}
		}

		public static void Handle_RemoveFavorite(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			if (currentAccount.activeItem.HasItem(num) && ModifyFavorite(currentAccount.UserNum, num, 2))
			{
				Client.SendAsync(new Myroom_RemoveFavorite(num, last));
			}
		}

		public static void Handle_CharacterStatReset(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			short character = reader.ReadLEInt16();
			int num = reader.ReadLEInt32();
			bool flag = true;
			if (num == 1)
			{
				List<ShopBuyItemInfo> list = new List<ShopBuyItemInfo>();
				for (int i = 0; i < 1; i++)
				{
					int charStatReset = ServerSettingHolder.ServerSettings.charStatReset;
					int unk = 0;
					int unk2 = 133234944;
					int sellItemNum = 0;
					ShopBuyItemInfo item = new ShopBuyItemInfo
					{
						ItemNum = charStatReset,
						unk3 = unk,
						unk4 = unk2,
						SellItemNum = sellItemNum
					};
					list.Add(item);
				}
				eShopFailed_REASON m_failReason = eShopFailed_REASON.eShopFailed_REASON_UNKNOWN;
				foreach (ShopBuyItemInfo item2 in list.Where((ShopBuyItemInfo w) => !w.NotForSale))
				{
					if (ShopHandle.BuyItemCheck(currentAccount, item2.ItemNum, item2.SellItemNum, 4000, out var _, out var _, out m_failReason))
					{
						item2.BuySuccess = true;
					}
				}
				flag = list.All((ShopBuyItemInfo a) => a.BuySuccess);
				if (flag)
				{
					Client.SendAsync(new ShopBuyItem_ACK(list, isFarmOrShuItem: false, last));
				}
				else
				{
					Client.SendAsync(new ShopBuyItemFail_New(m_failReason, list, last));
				}
				ItemHandle.getActiveFuncItem(currentAccount, -1);
			}
			if (!flag || !UserCharacterStatReset(currentAccount, character, num, last))
			{
				Client.SendAsync(new Myroom_CharacterStatResetFail(last));
			}
		}

		public static void Handle_CharacterStatConfirm(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			short character = reader.ReadLEInt16();
			int statNum = reader.ReadLEInt32();
			UserCharacterStatConfirm(currentAccount, character, statNum, last);
		}

		public static void Handle_UseExtraAbilityItem(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int itemNum = reader.ReadLEInt32();
			UseExtraAbilityItem(currentAccount, itemNum, last);
		}

		public static bool MyRoomSetCharacterSetting(int UserNum, AdvancedAvatarInfo m_originAdvancedAvatarInfo, AdvancedAvatarInfo m_transAdvancedAvatarInfo, bool bChangeRealAvatar, bool bChangeCostumeAvatar, bool bChangeAvatarMode)
		{
			bool flag = true;
			if (bChangeRealAvatar && flag)
			{
				try
				{
					using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_myRoomSetCharacterSetting");
					mySqlCommandHelper.AddParamInt("usernum", UserNum);
					mySqlCommandHelper.AddParamInt("pcharacter", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_character);
					mySqlCommandHelper.AddParamInt("head", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_head);
					mySqlCommandHelper.AddParamInt("topbody", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_topBody);
					mySqlCommandHelper.AddParamInt("downbody", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_downBody);
					mySqlCommandHelper.AddParamInt("foot", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_foot);
					mySqlCommandHelper.AddParamInt("acHead", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_acHead);
					mySqlCommandHelper.AddParamInt("acFace", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_acFace);
					mySqlCommandHelper.AddParamInt("acHand", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_acHand);
					mySqlCommandHelper.AddParamInt("acBack", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_acBack);
					mySqlCommandHelper.AddParamInt("acNeck", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_acNeck);
					mySqlCommandHelper.AddParamInt("pet", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_pet);
					mySqlCommandHelper.AddParamInt("expansion", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_expansion);
					mySqlCommandHelper.AddParamInt("acWrist", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_acWrist);
					mySqlCommandHelper.AddParamInt("acBooster", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_acBooster);
					mySqlCommandHelper.AddParamInt("acTail", m_originAdvancedAvatarInfo.m_realAvatarInfo.m_accTail);
					mySqlCommandHelper.AddParamInt("transCharacter", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_character);
					mySqlCommandHelper.AddParamInt("transHead", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_head);
					mySqlCommandHelper.AddParamInt("transTopBody", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_topBody);
					mySqlCommandHelper.AddParamInt("transDownBody", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_downBody);
					mySqlCommandHelper.AddParamInt("transFoot", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_foot);
					mySqlCommandHelper.AddParamInt("transAcHead", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_acHead);
					mySqlCommandHelper.AddParamInt("transAcFace", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_acFace);
					mySqlCommandHelper.AddParamInt("transAcHand", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_acHand);
					mySqlCommandHelper.AddParamInt("transAcBack", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_acBack);
					mySqlCommandHelper.AddParamInt("transAcNeck", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_acNeck);
					mySqlCommandHelper.AddParamInt("transPet", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_pet);
					mySqlCommandHelper.AddParamInt("transExpansion", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_expansion);
					mySqlCommandHelper.AddParamInt("transAcWrist", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_acWrist);
					mySqlCommandHelper.AddParamInt("transAcBooster", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_acBooster);
					mySqlCommandHelper.AddParamInt("transAcTail", m_transAdvancedAvatarInfo.m_realAvatarInfo.m_accTail);
					mySqlCommandHelper.ExecuteNonQuery();
					flag = true;
				}
				catch (Exception ex)
				{
					flag = false;
					Log.Error("usp_myRoomSetCharacterSetting Error: {0}", ex.Message);
				}
			}
			if (bChangeCostumeAvatar && flag)
			{
				try
				{
					using MySqlCommandHelper mySqlCommandHelper2 = new MySqlCommandHelper("usp_myRoomSetCostumeCharacterSetting");
					mySqlCommandHelper2.AddParamInt("usernum", UserNum);
					mySqlCommandHelper2.AddParamInt("pcharacter", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_character);
					mySqlCommandHelper2.AddParamInt("head", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_head);
					mySqlCommandHelper2.AddParamInt("topbody", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_topBody);
					mySqlCommandHelper2.AddParamInt("downbody", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_downBody);
					mySqlCommandHelper2.AddParamInt("foot", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_foot);
					mySqlCommandHelper2.AddParamInt("acHead", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_acHead);
					mySqlCommandHelper2.AddParamInt("acFace", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_acFace);
					mySqlCommandHelper2.AddParamInt("acHand", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_acHand);
					mySqlCommandHelper2.AddParamInt("acBack", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_acBack);
					mySqlCommandHelper2.AddParamInt("acNeck", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_acNeck);
					mySqlCommandHelper2.AddParamInt("pet", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_pet);
					mySqlCommandHelper2.AddParamInt("expansion", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_expansion);
					mySqlCommandHelper2.AddParamInt("acWrist", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_acWrist);
					mySqlCommandHelper2.AddParamInt("acBooster", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_acBooster);
					mySqlCommandHelper2.AddParamInt("acTail", m_originAdvancedAvatarInfo.m_costumeAvatarInfo.m_accTail);
					mySqlCommandHelper2.AddParamInt("transCharacter", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_character);
					mySqlCommandHelper2.AddParamInt("transHead", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_head);
					mySqlCommandHelper2.AddParamInt("transTopBody", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_topBody);
					mySqlCommandHelper2.AddParamInt("transDownBody", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_downBody);
					mySqlCommandHelper2.AddParamInt("transFoot", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_foot);
					mySqlCommandHelper2.AddParamInt("transAcHead", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_acHead);
					mySqlCommandHelper2.AddParamInt("transAcFace", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_acFace);
					mySqlCommandHelper2.AddParamInt("transAcHand", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_acHand);
					mySqlCommandHelper2.AddParamInt("transAcBack", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_acBack);
					mySqlCommandHelper2.AddParamInt("transAcNeck", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_acNeck);
					mySqlCommandHelper2.AddParamInt("transPet", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_pet);
					mySqlCommandHelper2.AddParamInt("transExpansion", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_expansion);
					mySqlCommandHelper2.AddParamInt("transAcWrist", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_acWrist);
					mySqlCommandHelper2.AddParamInt("transAcBooster", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_acBooster);
					mySqlCommandHelper2.AddParamInt("transAcTail", m_transAdvancedAvatarInfo.m_costumeAvatarInfo.m_accTail);
					mySqlCommandHelper2.ExecuteNonQuery();
					flag = true;
				}
				catch (Exception ex2)
				{
					flag = false;
					Log.Error("usp_myRoomSetCostumeCharacterSetting Error: {0}", ex2.Message);
				}
			}
			if (bChangeAvatarMode && flag)
			{
				try
				{
					using MySqlCommandHelper mySqlCommandHelper3 = new MySqlCommandHelper("usp_myRoomSetCostumeModeSetting");
					mySqlCommandHelper3.AddParamInt("usernum", UserNum);
					mySqlCommandHelper3.AddParamShort("costumeMode", (short)(m_transAdvancedAvatarInfo.m_bIsUseCostume ? 1 : 0));
					mySqlCommandHelper3.ExecuteNonQuery();
					return true;
				}
				catch (Exception ex3)
				{
					flag = false;
					Log.Error("usp_myRoomSetCostumeModeSetting Error: {0}", ex3.Message);
					return flag;
				}
			}
			return flag;
		}

		private static bool MyRoomSetSlotItemSetting(int UserNum, int slotNum, string SlotName, AvatarInfo m_AvatarInfo, out int err)
		{
			err = 0;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_myRoomSetSlotItemSetting");
				mySqlCommandHelper.AddParamInt("usernum", UserNum);
				mySqlCommandHelper.AddParamInt("slotNum", slotNum);
				mySqlCommandHelper.AddParamVarString("pSlotName", SlotName);
				mySqlCommandHelper.AddParamInt("charact", m_AvatarInfo.m_character);
				mySqlCommandHelper.AddParamInt("head", m_AvatarInfo.m_head);
				mySqlCommandHelper.AddParamInt("topbody", m_AvatarInfo.m_topBody);
				mySqlCommandHelper.AddParamInt("downbody", m_AvatarInfo.m_downBody);
				mySqlCommandHelper.AddParamInt("foot", m_AvatarInfo.m_foot);
				mySqlCommandHelper.AddParamInt("acHead", m_AvatarInfo.m_acHead);
				mySqlCommandHelper.AddParamInt("acFace", m_AvatarInfo.m_acFace);
				mySqlCommandHelper.AddParamInt("acHand", m_AvatarInfo.m_acHand);
				mySqlCommandHelper.AddParamInt("acBack", m_AvatarInfo.m_acBack);
				mySqlCommandHelper.AddParamInt("acNeck", m_AvatarInfo.m_acNeck);
				mySqlCommandHelper.AddParamInt("pet", m_AvatarInfo.m_pet);
				mySqlCommandHelper.AddParamInt("expansion", m_AvatarInfo.m_expansion);
				mySqlCommandHelper.AddParamInt("acWrist", m_AvatarInfo.m_acWrist);
				mySqlCommandHelper.AddParamInt("acBooster", m_AvatarInfo.m_acBooster);
				mySqlCommandHelper.AddParamInt("acTail", m_AvatarInfo.m_accTail);
				mySqlCommandHelper.ExecuteSingle();
				if (mySqlCommandHelper.HasResult())
				{
					err = mySqlCommandHelper.GetInt("ret");
					if (err == 0)
					{
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_myRoomGetUserSlotInfo Error: {0}", ex.ToString());
			}
			return false;
		}

		private static void MyRoomGetUserSlotInfo(int UserNum, out List<MyRoomSlotInfo> m_vSlotInfo)
		{
			m_vSlotInfo = new List<MyRoomSlotInfo>();
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_myRoomGetUserSlotInfo");
				mySqlCommandHelper.AddParamInt("pUserNum", UserNum);
				mySqlCommandHelper.Execute();
				while (mySqlCommandHelper.HasResult())
				{
					MyRoomSlotInfo item = default(MyRoomSlotInfo);
					item.m_iSlotNum = mySqlCommandHelper.GetInt("fdSlotNum");
					item.m_strSlotName = mySqlCommandHelper.GetString("fdSlotName");
					mySqlCommandHelper.getResultAvatarInfo(ref item.m_AvatarInfo);
					m_vSlotInfo.Add(item);
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_myRoomGetUserSlotInfo Error: {0}", ex.ToString());
			}
		}

		private static byte AcceptGiftEx(Account User, string strGiftNum, out List<int> itemList)
		{
			itemList = new List<int>();
			byte result = 0;
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_shopAcceptGiftEx";
				mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("strGiftNum", MySqlDbType.VarString).Value = strGiftNum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				long tR = User.TR;
				long exp = User.Exp;
				while (mySqlDataReader.Read())
				{
					result = Convert.ToByte(mySqlDataReader["ret"]);
					itemList.Add(mySqlDataReader.GetInt32("fdItemNum"));
					exp = Convert.ToInt64(mySqlDataReader["exp"]);
					tR = Convert.ToInt64(mySqlDataReader["gamemoney"]);
				}
				User.TR = tR;
				User.Exp = exp;
				return result;
			}
			catch (Exception ex)
			{
				Log.Error("usp_shopAcceptGiftEx Error:{0}", ex.Message);
				return 1;
			}
		}

		private static byte StorageReceiveEx(Account User, int type, string strUniqueNum, out List<long> itemList)
		{
			byte result = 0;
			itemList = new List<long>();
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_storage_receiveEx";
				mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("type", MySqlDbType.Int32).Value = type;
				mySqlCommand.Parameters.Add("strUniqueNum", MySqlDbType.VarString).Value = strUniqueNum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					result = Convert.ToByte(mySqlDataReader["retval"]);
					itemList.Add(mySqlDataReader.GetInt64("fdUniqueNum"));
				}
				return result;
			}
			catch (Exception ex)
			{
				Log.Error("usp_storage_receiveEx Error:{0}", ex.Message);
				return 1;
			}
		}

		private static byte StorageGift(Account User, string nickname, string msg, long UniqueNum)
		{
			byte b = 0;
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_storage_gift";
			mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("sendNickname", MySqlDbType.VarString).Value = User.NickName;
			mySqlCommand.Parameters.Add("targetNickname", MySqlDbType.VarString).Value = nickname;
			mySqlCommand.Parameters.Add("uniqueNum", MySqlDbType.Int64).Value = UniqueNum;
			mySqlCommand.Parameters.Add("memo", MySqlDbType.VarString).Value = msg;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
			mySqlDataReader.Read();
			return Convert.ToByte(mySqlDataReader["retval"]);
		}

		private static byte RepairItem(Account User, int itemnum, int repairitemnum)
		{
			byte b = 0;
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_alchemist_repairItem";
			mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("repairTargetItemdescnum", MySqlDbType.Int32).Value = itemnum;
			mySqlCommand.Parameters.Add("repairItemDescNum", MySqlDbType.Int32).Value = repairitemnum;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
			mySqlDataReader.Read();
			return Convert.ToByte(mySqlDataReader["retval"]);
		}

		private static byte FeedPet(Account User, int petitemnum, int feeditemnum, int addDays, int addExp, int feedcount)
		{
			byte b = 0;
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_myRoomPetFeed";
			mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("petItemDescNum", MySqlDbType.Int32).Value = petitemnum;
			mySqlCommand.Parameters.Add("petFeedItemDescNum", MySqlDbType.Int32).Value = feeditemnum;
			mySqlCommand.Parameters.Add("addDays", MySqlDbType.Int32).Value = addDays;
			mySqlCommand.Parameters.Add("addExp", MySqlDbType.Int32).Value = addExp;
			mySqlCommand.Parameters.Add("feedcount", MySqlDbType.Int32).Value = feedcount;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
			mySqlDataReader.Read();
			return Convert.ToByte(mySqlDataReader["retval"]);
		}

		private static byte FeedAllPet(Account User, int feeditemnum, int addDays, int addExp, int feedcount)
		{
			byte b = 0;
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_myRoomPetFeedToAll";
			mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("petFeedItemDescNum", MySqlDbType.Int32).Value = feeditemnum;
			mySqlCommand.Parameters.Add("addDays", MySqlDbType.Int32).Value = addDays;
			mySqlCommand.Parameters.Add("addExp", MySqlDbType.Int32).Value = addExp;
			mySqlCommand.Parameters.Add("feedcount", MySqlDbType.Int32).Value = feedcount;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
			mySqlDataReader.Read();
			return Convert.ToByte(mySqlDataReader["retval"]);
		}

		private static byte PetUpgrade(Account User, int petitemnum, int needexp)
		{
			byte b = 0;
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_myRoomPetUpgrade";
			mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("petitemdescnum", MySqlDbType.Int32).Value = petitemnum;
			mySqlCommand.Parameters.Add("needExp", MySqlDbType.Int32).Value = needexp;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
			mySqlDataReader.Read();
			return Convert.ToByte(mySqlDataReader["retval"]);
		}

		private static bool ItemOnOff(Account User, int itemnum, bool bOnOff, byte last)
		{
			bool flag = false;
			string text = string.Empty;
			try
			{
				text = ((!bOnOff) ? "usp_item_Off" : "usp_item_On");
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper(text);
				mySqlCommandHelper.AddParamInt("UserNum", User.UserNum);
				mySqlCommandHelper.AddParamInt("ItemDescNum", itemnum);
				mySqlCommandHelper.ExecuteNonQuery();
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("{0} Error: {1}", text, ex.ToString());
			}
			if (flag)
			{
				ShopItemTable.getItemDataFromItemDescNum(itemnum, out var itemData);
				User.updateItemOnOff(itemnum, bOnOff, out var offItemInfo, out var onItemInfo);
				if (User.isInRoom(out var room))
				{
					ServerStatus.ToRoomServer(new eRoom_UPDATE_ITEM_ONOFF_INFO(User, itemnum, itemData.m_iOnOffType, itemData.m_iPosition, bOnOff, offItemInfo, onItemInfo, last), room.RoomServerID);
				}
				else
				{
					User.Connection.SendAsync(new ITEM_ONOFF_ACK(itemnum, itemData.m_iOnOffType, itemData.m_iPosition, bOnOff, last));
				}
			}
			return flag;
		}

		private static bool UseLuckyBag(Account User, int itemnum, byte OpenNum, out ConcurrentDictionary<int, byte> itemlist)
		{
			itemlist = new ConcurrentDictionary<int, byte>();
			try
			{
				using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
				{
					mySqlConnection.Open();
					using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
					mySqlCommand.Parameters.Clear();
					mySqlCommand.CommandType = CommandType.StoredProcedure;
					mySqlCommand.CommandText = "usp_myRoomUseLuckyBag_New";
					mySqlCommand.Parameters.Add("pUserNum", MySqlDbType.Int32).Value = User.UserNum;
					mySqlCommand.Parameters.Add("pItemDescNum", MySqlDbType.Int32).Value = itemnum;
					mySqlCommand.Parameters.Add("pOpenNum", MySqlDbType.Int32).Value = OpenNum;
					using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
					if (mySqlDataReader.HasRows)
					{
						long tR = User.TR;
						long exp = User.Exp;
						while (mySqlDataReader.Read())
						{
							int key = Convert.ToInt32(mySqlDataReader["resultItem"]);
							if (!itemlist.ContainsKey(key))
							{
								itemlist.TryAdd(key, 1);
							}
							else
							{
								itemlist[key]++;
							}
							exp = Convert.ToInt64(mySqlDataReader["totalExp"]);
							tR = Convert.ToInt64(mySqlDataReader["totalGameMoney"]);
						}
						User.TR = tR;
						User.Exp = exp;
						return true;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				Log.Error("Error on using lucky bag:{0}, itemnum:{1}", ex.Message, itemnum);
				return false;
			}
		}

		private static bool GetFavorite(int UserNum, out List<int> itemlist)
		{
			itemlist = new List<int>();
			try
			{
				using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
				{
					mySqlConnection.Open();
					using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
					mySqlCommand.Parameters.Clear();
					mySqlCommand.CommandType = CommandType.StoredProcedure;
					mySqlCommand.CommandText = "usp_myRoom_GetFavorite";
					mySqlCommand.Parameters.Add("UserNum", MySqlDbType.Int32).Value = UserNum;
					using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
					if (mySqlDataReader.HasRows)
					{
						while (mySqlDataReader.Read())
						{
							itemlist.Add(Convert.ToInt32(mySqlDataReader["fdItemNum"]));
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				Log.Error("Error on usp_myRoom_GetFavorite:\r\n{0}", ex.Message);
				return false;
			}
		}

		private static bool ModifyFavorite(int UserNum, int itemnum, int type)
		{
			try
			{
				using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
				{
					mySqlConnection.Open();
					using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
					mySqlCommand.Parameters.Clear();
					mySqlCommand.CommandType = CommandType.StoredProcedure;
					mySqlCommand.CommandText = "usp_myRoom_ModifyFavorite";
					mySqlCommand.Parameters.Add("UserNum", MySqlDbType.Int32).Value = UserNum;
					mySqlCommand.Parameters.Add("ItemNum", MySqlDbType.Int32).Value = itemnum;
					mySqlCommand.Parameters.Add("Type", MySqlDbType.Int32).Value = type;
					mySqlCommand.ExecuteNonQuery();
				}
				return true;
			}
			catch (Exception ex)
			{
				Log.Error("Error on usp_myRoom_ModifyFavorite:\r\n{0}", ex.Message);
				return false;
			}
		}

		private static void myRoomPetRebirth(int UserNum, int petitemnum, int rebirthitemnum, out List<int> PetItemlist)
		{
			PetItemlist = new List<int>();
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_myRoomPetRebirth";
				mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = UserNum;
				mySqlCommand.Parameters.Add("petItemNum", MySqlDbType.Int32).Value = petitemnum;
				mySqlCommand.Parameters.Add("petRebirthItemNum", MySqlDbType.Int32).Value = rebirthitemnum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					PetItemlist.Add(mySqlDataReader.GetInt32("fdPetItemNum"));
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_myRoomPetRebirth Error:{0}", ex.Message);
			}
		}

		private static bool UseExtraAbilityItem(Account User, int ItemNum, byte last)
		{
			CActiveItems cActiveItems = new CActiveItems();
			Dictionary<int, ExtraAbilityInfo> dictionary = new Dictionary<int, ExtraAbilityInfo>();
			bool flag = false;
			int num = 0;
			int divinationType = 0;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_useExtraAbilityItem");
				mySqlCommandHelper.AddParamInt("userNum", User.UserNum);
				mySqlCommandHelper.AddParamInt("itemNum", ItemNum);
				mySqlCommandHelper.AddParamInt("coupleNum", User.CoupleInfo.CoupleNum);
				mySqlCommandHelper.Execute();
				while (mySqlCommandHelper.HasResult())
				{
					short key = (short)mySqlCommandHelper.GetInt("attr");
					float @float = mySqlCommandHelper.GetFloat("value");
					int @int = mySqlCommandHelper.GetInt("itemNum");
					int int2 = mySqlCommandHelper.GetInt("limit");
					long dateTime = mySqlCommandHelper.GetDateTime("gotTime", 0L);
					if (!dictionary.TryGetValue(@int, out var value))
					{
						value = new ExtraAbilityInfo();
					}
					value.iItemDescNum = @int;
					value.iLimitTime = int2;
					value.tGotTime = dateTime;
					if (!value.mapAttributes.ContainsKey(key))
					{
						value.mapAttributes.Add(key, @float);
					}
					if (!dictionary.ContainsKey(@int))
					{
						dictionary.Add(@int, value);
					}
				}
				mySqlCommandHelper.NextResult();
				while (mySqlCommandHelper.HasResult())
				{
					int int3 = mySqlCommandHelper.GetInt("itemNum");
					cpk_type character = mySqlCommandHelper.GetInt("itemCharacter");
					cpk_type position = mySqlCommandHelper.GetInt("itemPosition");
					cpk_type kind = mySqlCommandHelper.GetInt("itemKind");
					bool boolean = mySqlCommandHelper.GetBoolean("using");
					int int4 = mySqlCommandHelper.GetInt("itemCount");
					long expiretime = 0L;
					if (!mySqlCommandHelper.IsDBNull("expireTime"))
					{
						expiretime = mySqlCommandHelper.GetDateTime("expireTime", 0L);
					}
					cActiveItems.insertItem(new NetItemInfo(int3, character, position, kind, boolean, int4, expiretime, 0L));
				}
				mySqlCommandHelper.NextResult();
				if (mySqlCommandHelper.HasResult())
				{
					num = mySqlCommandHelper.GetInt("remainItemCount");
					divinationType = mySqlCommandHelper.GetInt("divinationType");
				}
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_useExtraAbilityItem Error:{0}", ex.Message);
			}
			if (flag)
			{
				User.activeItem.updateItemCount(ItemNum, num);
				User.Connection.SendAsync(new Myroom_UseExtraAbilityItem_ACK(User, divinationType, ItemNum, num, dictionary, last));
				foreach (NetItemInfo item in cActiveItems.getVector())
				{
					User.activeItem.replaceItemInfoByItem(item);
				}
				if (User.isInRoom(out var room))
				{
					ServerStatus.ToRoomServer(new eRoom_CHANGE_USER_ACTIVE_ITEM_ONE(User, cActiveItems, dictionary, last), room.RoomServerID);
				}
			}
			return flag;
		}

		private static bool UserCharacterStatReset(Account User, int character, int statType, byte last)
		{
			UserItemAttrInfo userItemAttrInfo = new UserItemAttrInfo();
			bool flag = false;
			int statNum = 0;
			long tR = 0L;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_UserCharacterStatReset");
				mySqlCommandHelper.AddParamInt("pUserNum", User.UserNum);
				mySqlCommandHelper.AddParamInt("pcharacter", character);
				mySqlCommandHelper.AddParamInt("statType", statType);
				mySqlCommandHelper.Execute();
				while (mySqlCommandHelper.HasResult())
				{
					tR = mySqlCommandHelper.GetLongConvert("remain");
					statNum = mySqlCommandHelper.GetInt("statNum");
					short @short = mySqlCommandHelper.GetShort("AttrType");
					float @float = mySqlCommandHelper.GetFloat("AttrValue");
					userItemAttrInfo.m_iItemDescNum = mySqlCommandHelper.GetInt("itemDescNum");
					userItemAttrInfo.m_ItemAttr[@short] = @float;
				}
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_UserCharacterStatReset Error:{0}", ex.Message);
			}
			if (flag)
			{
				User.TR = tR;
				User.Connection.SendAsync(new Myroom_CharacterStatReset_ACK(statNum, userItemAttrInfo, last));
			}
			return flag;
		}

		private static bool UserCharacterStatConfirm(Account User, int character, int statNum, byte last)
		{
			UserItemAttrInfo userItemAttrInfo = new UserItemAttrInfo();
			bool flag = false;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_UserCharacterStatConfirm");
				mySqlCommandHelper.AddParamInt("pUserNum", User.UserNum);
				mySqlCommandHelper.AddParamInt("pcharacter", character);
				mySqlCommandHelper.AddParamInt("statNum", statNum);
				mySqlCommandHelper.Execute();
				while (mySqlCommandHelper.HasResult())
				{
					short @short = mySqlCommandHelper.GetShort("AttrType");
					float @float = mySqlCommandHelper.GetFloat("AttrValue");
					userItemAttrInfo.m_iItemDescNum = mySqlCommandHelper.GetInt("itemDescNum");
					userItemAttrInfo.m_ItemAttr[@short] = @float;
				}
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_UserCharacterStatConfirm Error:{0}", ex.Message);
			}
			if (flag)
			{
				User.userItemAttr.insertCharAttr(userItemAttrInfo);
				User.Connection.SendAsync(new Myroom_CharacterStatConfirm_ACK(userItemAttrInfo, last));
			}
			else
			{
				User.Connection.SendAsync(new Myroom_CharacterStatConfirmFail(last));
			}
			return flag;
		}

		private static bool myRoomGetCharacterSetting(Account User, int characterKind, out AvatarInfo m_realAvatarInfo)
		{
			m_realAvatarInfo = default(AvatarInfo);
			bool flag = false;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_myRoomGetCharacterSetting");
				mySqlCommandHelper.AddParamInt("usernum", User.UserNum);
				mySqlCommandHelper.AddParamInt("characterKind", characterKind);
				mySqlCommandHelper.Execute();
				if (mySqlCommandHelper.HasResult())
				{
					mySqlCommandHelper.getResultAvatarInfo(ref m_realAvatarInfo);
					flag = true;
				}
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_myRoomGetCharacterSetting Error:{0}", ex.Message);
			}
			if (flag)
			{
				AdvancedAvatarInfo advancedAvatarInfo = new AdvancedAvatarInfo();
				advancedAvatarInfo.setAvatarInfo(1, m_realAvatarInfo);
				User.checkEquipmentItem(bAvatarChanged: true, bDefaultAvatar: false, ref advancedAvatarInfo);
			}
			return flag;
		}

		private static bool myRoomSetDefaultCharacter(Account User, int charid)
		{
			AdvancedAvatarInfo advancedAvatarInfo = new AdvancedAvatarInfo();
			bool flag = false;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_myRoomSetDefaultCharacter");
				mySqlCommandHelper.AddParamInt("usernum", User.UserNum);
				mySqlCommandHelper.AddParamInt("pcharacter", charid);
				mySqlCommandHelper.Execute();
				if (mySqlCommandHelper.HasResult())
				{
					mySqlCommandHelper.getResultAvatarInfo(ref advancedAvatarInfo.m_realAvatarInfo);
					mySqlCommandHelper.getResultAvatarInfo(ref advancedAvatarInfo.m_costumeAvatarInfo, bCostume: true);
					advancedAvatarInfo.m_bIsUseCostume = mySqlCommandHelper.GetBoolean("costumeMode");
					flag = true;
				}
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_myRoomSetDefaultCharacter Error: {0}", ex.ToString());
			}
			if (flag)
			{
				User.setAvatarInfoAndSendTCP(advancedAvatarInfo);
			}
			return flag;
		}
	}
}
