using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AgentServer.Database;
using AgentServer.Holders;
using AgentServer.Network.Connections;
using AgentServer.Packet.RoomServer;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Item;
using Akka.Actor;
using LocalCommons.Network;
using LocalCommons.Utilities;
using MySql.Data.MySqlClient;
using NetMsg.LBS;
using Serilog;
using TRCommon;

namespace AgentServer.Packet
{
	public class ParkHandle
	{
		public static void Handle_GetMachineInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			int machineID = reader.ReadLEInt32();
			int key = num;
			if (num == 43845)
			{
				key = CapsuleMachineHolder.CurrentRotateNum;
			}
			if (CapsuleMachineHolder.CapsuleMachineContainer.TryGetValue(key, out var value))
			{
				if (value.LastResetTime < DateTime.Now)
				{
					Client.SendAsync(new GetMachineInfo(currentAccount, num, value, last));
				}
				else
				{
					Client.SendAsync(new GetMachineInfoResetting(currentAccount, num, value, last));
				}
			}
			else
			{
				Log.Warning("Unknown CapsuleMachineNum:{0}, NickName:{1}", num, currentAccount.NickName);
				Client.SendAsync(new GetMachineInfoFail(currentAccount, num, machineID, last));
			}
		}

		public static void Handle_Alchemist_MachineSelect(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			switch (num)
			{
			case 2:
			{
				int fixedLength4 = reader.ReadLEInt16();
				int num3 = Convert.ToInt32(reader.ReadBig5StringSafe(fixedLength4));
				fixedLength4 = reader.ReadLEInt16();
				int num4 = Convert.ToInt32(reader.ReadBig5StringSafe(fixedLength4));
				if (num3 >= 1 && num3 <= 3 && num4 >= 1 && num4 <= 6)
				{
					Log.Debug("Divination  pDivination:{0}, pReqDivination:{1}", num3, num4);
					if (useDivination(currentAccount, num3, num4, 0, last))
					{
						currentAccount.CashNeedUpdateFromDB = true;
					}
				}
				break;
			}
			case 3:
			{
				int fixedLength7 = reader.ReadLEInt16();
				int num5 = Convert.ToInt32(reader.ReadBig5StringSafe(fixedLength7));
				int fixedLength8 = reader.ReadLEInt16();
				Convert.ToInt32(reader.ReadBig5StringSafe(fixedLength8));
				if ((DateTime.Now - currentAccount.LastSelectMachineTime).TotalSeconds < 2.0)
				{
					Client.SendAsync(new GetMachineSelectItemFail(num5, 4, last));
					break;
				}
				currentAccount.LastSelectMachineTime = DateTime.Now;
				if (CapsuleMachineHolder.CapsuleMachineContainer.TryGetValue(num5, out var value2))
				{
					if (value2.RealMachineNumKind > 1000 && !value2.isRotate)
					{
						Client.SendAsync(new GetMachineSelectItemFail(value2.RealMachineNum, 1, last));
					}
					else if (value2.LastResetTime < DateTime.Now)
					{
						int ret2;
						int resultItemNum = value2.DrawItem(currentAccount, out ret2);
						if (ret2 == 0)
						{
							Client.SendAsync(new GetMachineSelectItem(value2.RealMachineNum, resultItemNum, last));
							break;
						}
						byte err = (byte)(2 + ret2);
						Client.SendAsync(new GetMachineSelectItemFail(value2.RealMachineNum, err, last));
					}
					else
					{
						Client.SendAsync(new GetMachineSelectItemFail(value2.RealMachineNum, 9, last));
					}
				}
				else
				{
					Client.SendAsync(new GetMachineSelectItemFail(value2.RealMachineNum, 2, last));
				}
				break;
			}
			case 4:
			{
				int fixedLength5 = reader.ReadLEInt16();
				int itemnum = Convert.ToInt32(reader.ReadBig5StringSafe(fixedLength5));
				if (GiveUserFarmSlot(currentAccount, itemnum, out var SlotNum))
				{
					Client.SendAsync(new BuyFarmSlotOK_Ack(SlotNum, last));
				}
				break;
			}
			case 5:
			{
				int fixedLength6 = reader.ReadLEInt16();
				int itemnum2 = Convert.ToInt32(reader.ReadBig5StringSafe(fixedLength6));
				if (MyRoomGiveUserMyRoomSlot(currentAccount, itemnum2, out var SlotNum2))
				{
					Client.SendAsync(new Myroom_BuyMyRoomSlotOK(SlotNum2, last));
				}
				break;
			}
			case 8:
			{
				int fixedLength = reader.ReadLEInt16();
				int key = Convert.ToInt32(reader.ReadBig5StringSafe(fixedLength));
				int fixedLength2 = reader.ReadLEInt16();
				short num2 = Convert.ToInt16(reader.ReadBig5StringSafe(fixedLength2));
				int fixedLength3 = reader.ReadLEInt16();
				Convert.ToInt32(reader.ReadBig5StringSafe(fixedLength3));
				if (EventPickBoardHolder.HuMongPickBoardContainer.TryGetValue(key, out var value) && num2 <= value.PickInfo.Length)
				{
					if (value.LastResetTime < DateTime.Now && !value.PickInfo[num2 - 1])
					{
						int AdditionRewardItemNum;
						int ret;
						byte rank = value.PickItem(currentAccount, num2, out AdditionRewardItemNum, out ret);
						if (ret == 0)
						{
							Client.SendAsync(new HuMongPickBoard_PickItem_OK(num2, rank, AdditionRewardItemNum, last));
						}
						else
						{
							Client.SendAsync(new HuMongPickBoard_PickItem_Fail(eServerResult.eServerResult_DB_FAILED_ACK, last));
						}
					}
					else
					{
						Client.SendAsync(new HuMongPickBoard_PickItem_Fail(eServerResult.eServerResult_HUMONGPICKBOARD_ALREADY_PICKED, last));
					}
				}
				else
				{
					Client.SendAsync(new HuMongPickBoard_PickItem_Fail(eServerResult.eServerResult_HUMONGPICKBOARD_INVALID_BOARD, last));
				}
				break;
			}
			default:
				Log.Warning("Unknown Alchemist_MachineSelect type: {0}!", num);
				break;
			}
		}

		public static void Handle_MachineReceiveORGiftItem(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int resultItemNum = 0;
			bool isGift = false;
			int num = reader.ReadLEInt16();
			string text = string.Empty;
			if (num > 0)
			{
				text = reader.ReadBig5StringSafe(num);
			}
			byte ret;
			if (text == string.Empty)
			{
				ret = 1;
			}
			else
			{
				int num2 = reader.ReadLEInt16();
				string memo = string.Empty;
				if (num2 > 0)
				{
					memo = reader.ReadBig5StringSafe(num2);
				}
				resultItemNum = GiveItem(currentAccount, text, memo, out isGift, out ret);
			}
			if (ret == 0)
			{
				Client.SendAsync(new MachineGiveItem(currentAccount, resultItemNum, isGift, text, last));
				return;
			}
			byte err = (byte)(2 + ret);
			Client.SendAsync(new MachineGiveItemFail(err, last));
		}

		public static void Handle_MachineKeepItem(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int type = reader.ReadLEInt32();
			int itemnum = reader.ReadLEInt32();
			long uniqueNum;
			int itemNum;
			long dateTime;
			bool isSuccess = KeepItem(currentAccount, type, itemnum, out uniqueNum, out itemNum, out dateTime);
			Client.SendAsync(new MachineKeepItem(currentAccount, isSuccess, uniqueNum, itemNum, dateTime, last));
		}

		public static void Handle_CheckDivinationFree(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			if (num >= 1 && num <= 3)
			{
				checkDivinationFree(currentAccount, num, out var isFree);
				Client.SendAsync(new CheckDivinationFree_ACK(num, isFree, last));
			}
		}

		public static void Handle_ParkUseDivinationFree(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			int num2 = reader.ReadLEInt32();
			if (num >= 1 && num <= 3 && num2 >= 1 && num2 <= 6 && useDivination(currentAccount, num, num2, 1, last))
			{
				currentAccount.CashNeedUpdateFromDB = true;
			}
		}

		private static int GiveItem(Account User, string NickName, string memo, out bool isGift, out byte ret)
		{
			ret = 0;
			isGift = false;
			int result = 0;
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_capsuleMachineGiveItem";
				mySqlCommand.Parameters.Add("sendUserNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("sendNickname", MySqlDbType.VarString).Value = User.NickName;
				mySqlCommand.Parameters.Add("receiveNickname", MySqlDbType.VarString).Value = NickName;
				mySqlCommand.Parameters.Add("memo", MySqlDbType.VarString).Value = memo;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				mySqlDataReader.Read();
				ret = (byte)mySqlDataReader.GetInt32("retval");
				if (ret == 0)
				{
					result = mySqlDataReader.GetInt32("itemNum");
					isGift = mySqlDataReader.GetBoolean("isGift");
					Convert.ToInt64(mySqlDataReader["reamainGameMoney"]);
					return result;
				}
				return result;
			}
			catch (Exception ex)
			{
				Log.Error("usp_capsuleMachineGiveItem Error: {0}", ex.Message);
				ret = 2;
				return result;
			}
		}

		private static bool KeepItem(Account User, int type, int itemnum, out long uniqueNum, out int itemNum, out long dateTime)
		{
			uniqueNum = 0L;
			itemNum = 0;
			dateTime = 0L;
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_storage_save";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("type", MySqlDbType.Int32).Value = type;
				mySqlCommand.Parameters.Add("itemNum", MySqlDbType.Int32).Value = itemnum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				if (mySqlDataReader.HasRows)
				{
					mySqlDataReader.Read();
					uniqueNum = Convert.ToInt64(mySqlDataReader["uniqueNum"]);
					itemNum = Convert.ToInt32(mySqlDataReader["itemNum"]);
					dateTime = Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["dateTime"]));
					return true;
				}
			}
			catch (Exception ex)
			{
				Log.Error("Error on keep item: {0}", ex.Message);
				return false;
			}
			return false;
		}

		public static int GetAlchemistMixGrade(float luck)
		{
			int num = 0;
			int num2 = 0;
			Random random = new Random(Guid.NewGuid().GetHashCode());
			if (luck <= 0f)
			{
				int num3 = 81;
				num2 = random.Next() % num3;
			}
			else
			{
				int num4 = 70;
				int num5 = (int)(luck / (float)num4 + 1f);
				num5 = ((num5 > 10) ? 10 : num5);
				for (int i = 0; i < num5; i++)
				{
					int num6 = 30;
					int num7 = num6 + random.Next() % (101 - num6);
					if (num2 < num7)
					{
						num2 = num7;
					}
				}
			}
			int num8 = 98;
			int num9 = 75;
			int num10 = 40;
			if (num2 < num8)
			{
				if (num2 < num9)
				{
					if (num2 < num10)
					{
						return 400;
					}
					return 300;
				}
				return 200;
			}
			return 100;
		}

		private static bool MyRoomGiveUserMyRoomSlot(Account User, int itemnum, out int SlotNum)
		{
			SlotNum = 0;
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_myRoom_GiveUserMyRoomSlot";
				mySqlCommand.Parameters.Add("pUserNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("pMyRoomSlotItemNum", MySqlDbType.Int32).Value = itemnum;
				mySqlCommand.Parameters.Add("pMyRoomSlotMethod", MySqlDbType.Int32).Value = 2;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				if (mySqlDataReader.HasRows)
				{
					mySqlDataReader.Read();
					SlotNum = Convert.ToInt32(mySqlDataReader["fdSlotNum"]);
					User.TR -= Convert.ToInt32(mySqlDataReader["TRPrice"]);
					User.Cash -= Convert.ToInt32(mySqlDataReader["CashPrice"]);
					return true;
				}
			}
			return false;
		}

		private static bool GiveUserFarmSlot(Account User, int itemnum, out int SlotNum)
		{
			SlotNum = 0;
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_Farm_GiveUserFarmSlot";
				mySqlCommand.Parameters.Add("pUserNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("pFarmSlotItemNum", MySqlDbType.Int32).Value = itemnum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				if (mySqlDataReader.HasRows)
				{
					mySqlDataReader.Read();
					SlotNum = Convert.ToInt32(mySqlDataReader["fdSlotNum"]);
					User.TR -= Convert.ToInt32(mySqlDataReader["TRPrice"]);
					User.Cash -= Convert.ToInt32(mySqlDataReader["CashPrice"]);
					return true;
				}
			}
			return false;
		}

		private static void checkDivinationFree(Account User, int pDivination, out bool isFree)
		{
			isFree = false;
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_checkDivinationFree";
				mySqlCommand.Parameters.Add("pUsernum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("pDivination", MySqlDbType.Int32).Value = pDivination;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				if (mySqlDataReader.HasRows)
				{
					mySqlDataReader.Read();
					if (mySqlDataReader.GetInt32("retVal") != 0)
					{
						isFree = true;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_checkDivinationFree Error:{0}", ex.Message);
			}
		}

		private static bool useDivination2(Account User, int pDivination, int pReqDivination, int pFree, out ExtraAbilityItemAttrInfo exItemAttrInfo, out ExtraAbilityItemInfo exItemInfo)
		{
			exItemAttrInfo = new ExtraAbilityItemAttrInfo();
			exItemInfo = new ExtraAbilityItemInfo();
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_useDivination";
				mySqlCommand.Parameters.Add("pUsernum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("pDivination", MySqlDbType.Int32).Value = pDivination;
				mySqlCommand.Parameters.Add("pReqDivination", MySqlDbType.Int32).Value = pReqDivination;
				mySqlCommand.Parameters.Add("pCoupleNum", MySqlDbType.Int32).Value = User.CoupleInfo.CoupleNum;
				mySqlCommand.Parameters.Add("pFree", MySqlDbType.Int32).Value = pFree;
				mySqlCommand.Parameters.Add("ignoreFreeCheck", MySqlDbType.Int32).Value = 0;
				mySqlCommand.Parameters.Add("iMinute", MySqlDbType.Int32).Value = 15;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				if (mySqlDataReader.HasRows)
				{
					while (mySqlDataReader.Read())
					{
						ushort attr = Convert.ToUInt16(mySqlDataReader["attrtype"]);
						float attrValue = Convert.ToSingle(mySqlDataReader["attrvalue"]);
						ItemAttr item = new ItemAttr
						{
							Attr = attr,
							AttrValue = attrValue
						};
						exItemAttrInfo.attrlist.Add(item);
						int @int = mySqlDataReader.GetInt32("itemnum");
						exItemAttrInfo.itemnum = @int;
						exItemAttrInfo.limit = Convert.ToInt32(mySqlDataReader["limit"]);
						long num = Utility.ConvertToTimestamp(mySqlDataReader.GetDateTime("gottime"));
						exItemAttrInfo.gottime = num;
						NetItemInfo info = new NetItemInfo(@int, Convert.ToInt16(mySqlDataReader["character"]), Convert.ToUInt16(mySqlDataReader["position"]), Convert.ToUInt16(mySqlDataReader["kind"]), Convert.ToBoolean(mySqlDataReader["using"]), Convert.ToInt32(mySqlDataReader["count"]), Utility.ConvertToTimestamp(mySqlDataReader.GetDateTime("expiretime")), num);
						User.activeItem.replaceItemInfoByItem(info);
					}
					exItemInfo.divinationType = pDivination;
					exItemInfo.remainItemCount = 0;
					return true;
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_useDivination Error:{0}", ex.Message);
			}
			return false;
		}

		private static bool useDivination(Account User, int pDivination, int pReqDivination, int pFree, byte last)
		{
			CActiveItems cActiveItems = new CActiveItems();
			Dictionary<int, ExtraAbilityInfo> dictionary = new Dictionary<int, ExtraAbilityInfo>();
			bool flag = false;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_useDivination");
				mySqlCommandHelper.AddParamInt("pUsernum", User.UserNum);
				mySqlCommandHelper.AddParamInt("pDivination", pDivination);
				mySqlCommandHelper.AddParamInt("pReqDivination", pReqDivination);
				mySqlCommandHelper.AddParamInt("pCoupleNum", User.CoupleInfo.CoupleNum);
				mySqlCommandHelper.AddParamInt("pFree", pFree);
				mySqlCommandHelper.AddParamInt("ignoreFreeCheck", 0);
				mySqlCommandHelper.AddParamInt("iMinute", 15);
				mySqlCommandHelper.Execute();
				while (mySqlCommandHelper.HasResult())
				{
					int @int = mySqlCommandHelper.GetInt("itemNum");
					if (!dictionary.TryGetValue(@int, out var value))
					{
						value = new ExtraAbilityInfo();
					}
					short key = (short)mySqlCommandHelper.GetInt("attr");
					float @float = mySqlCommandHelper.GetFloat("value");
					int int2 = mySqlCommandHelper.GetInt("limit");
					long dateTime = mySqlCommandHelper.GetDateTime("gotTime", 0L);
					value.iItemDescNum = @int;
					value.iLimitTime = int2;
					value.tGotTime = dateTime;
					value.mapAttributes[key] = @float;
					if (!dictionary.ContainsKey(@int))
					{
						dictionary.Add(@int, value);
					}
					else
					{
						dictionary[@int] = value;
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
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_useDivination Error:{0}", ex.Message);
			}
			if (flag)
			{
				User.Connection.SendAsync(new ParkUseDivination_ACK(User, pDivination, dictionary, last));
				foreach (NetItemInfo item in cActiveItems.getVector())
				{
					User.activeItem.replaceItemInfoByItem(item);
				}
				if (User.isInRoom(out var room))
				{
					ServerStatus.ToRoomServer(new eRoom_CHANGE_USER_ACTIVE_ITEM_ONE(User, cActiveItems, dictionary, last), room.RoomServerID);
				}
				if (pDivination == 3)
				{
					int iItemDescNum = dictionary.FirstOrDefault().Value.iItemDescNum;
					ParkDivinationCouple message = new ParkDivinationCouple
					{
						ItemNum = iItemDescNum,
						UserNum = User.UserNum,
						CoupleNum = User.CoupleInfo.CoupleNum,
						Attrs = new Dictionary<ushort, float>(),
						limit = 0,
						gottime = 0L
					};
					ServerStatus.LBServerActor.Tell(message);
				}
			}
			return flag;
		}

		public static bool divinationUpdateCoupleAbility(Account User, int pExItemNum)
		{
			CActiveItems cActiveItems = new CActiveItems();
			Dictionary<int, ExtraAbilityInfo> dictionary = new Dictionary<int, ExtraAbilityInfo>();
			bool flag = false;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_divinationUpdateCoupleAbility");
				mySqlCommandHelper.AddParamInt("pUsernum", User.UserNum);
				mySqlCommandHelper.AddParamInt("pExItemNum", pExItemNum);
				mySqlCommandHelper.Execute();
				while (mySqlCommandHelper.HasResult())
				{
					ExtraAbilityInfo extraAbilityInfo = new ExtraAbilityInfo();
					short key = (short)mySqlCommandHelper.GetInt("attr");
					float @float = mySqlCommandHelper.GetFloat("value");
					int @int = mySqlCommandHelper.GetInt("itemNum");
					int int2 = mySqlCommandHelper.GetInt("limit");
					long dateTime = mySqlCommandHelper.GetDateTime("gotTime", 0L);
					extraAbilityInfo.iItemDescNum = @int;
					extraAbilityInfo.iLimitTime = int2;
					extraAbilityInfo.tGotTime = dateTime;
					extraAbilityInfo.mapAttributes[key] = @float;
					if (!dictionary.ContainsKey(@int))
					{
						dictionary.Add(@int, extraAbilityInfo);
					}
					else
					{
						dictionary[@int] = extraAbilityInfo;
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
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_divinationUpdateCoupleAbility Error:{0}", ex.Message);
			}
			if (flag)
			{
				User.Connection.SendAsync(new ParkUseDivination_Couple_ACK(User, dictionary, 1));
				foreach (NetItemInfo item in cActiveItems.getVector())
				{
					User.activeItem.replaceItemInfoByItem(item);
				}
				if (User.isInRoom(out var room))
				{
					ServerStatus.ToRoomServer(new eRoom_CHANGE_USER_ACTIVE_ITEM_ONE(User, cActiveItems, dictionary, 1), room.RoomServerID);
				}
			}
			return flag;
		}
	}
}
