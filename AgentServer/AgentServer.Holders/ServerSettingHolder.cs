using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AgentServer.Database;
using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using MySql.Data.MySqlClient;
using Serilog;

namespace AgentServer.Holders
{
	public static class ServerSettingHolder
	{
		public static List<SettingInfo> ServerSettingList { get; } = new List<SettingInfo>();


		public static ServerSetting ServerSettings { get; private set; }

		public static HashSet<string> HashList { get; set; } = new HashSet<string>();


		public static void LoadServerSettingInfo()
		{
			ServerSettingList.Clear();
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand("SELECT * FROM tblserversettinginfo", mySqlConnection);
				mySqlCommand.Parameters.Clear();
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					SettingInfo item = new SettingInfo
					{
						Key = mySqlDataReader["fdKey"].ToString().Replace(" ", ""),
						Value = mySqlDataReader["fdValue"].ToString(),
						OnlyServerSetting = Convert.ToBoolean(mySqlDataReader["fdOnlyServerSetting"])
					};
					ServerSettingList.Add(item);
				}
			}
			PacketWriter packetWriter = PacketWriter.CreateInstance(32, LittleEndian: true);
			packetWriter.WriteOP(Opcodes.eServer_GET_SERVERSETTING_INFO_ACK);
			packetWriter.Write(ServerSettingList.Count((SettingInfo c) => !c.OnlyServerSetting));
			foreach (SettingInfo item2 in ServerSettingList.Where((SettingInfo w) => !w.OnlyServerSetting))
			{
				packetWriter.WriteBIG5Fixed_shortSize(item2.Key);
				packetWriter.WriteBIG5Fixed_shortSize(item2.Value);
			}
			packetWriter.Write((byte)1);
			DBInit.GameServerSetting = packetWriter.ToArray();
			PacketWriter.ReleaseInstance(packetWriter);
			packetWriter = null;
			LoadDBInfo();
			Log.Information("Load ServerSetting Count: {0}", ServerSettingList.Count());
		}

		public static void LoadHashList()
		{
			HashList.Clear();
			string[] array = File.ReadAllLines("hash.ini");
			foreach (string item in array)
			{
				HashList.Add(item);
			}
			Log.Information("Load Hash Count: {0}", HashList.Count);
		}

		private static void LoadDBInfo()
		{
			ServerSettings = null;
			ServerSettings = new ServerSetting();
			foreach (SettingInfo serverSetting in ServerSettingList)
			{
				switch (serverSetting.Key)
				{
				case "charStatReset":
					ServerSettings.charStatReset = Convert.ToInt32(serverSetting.Value);
					break;
				case "MultiplyTR":
					ServerSettings.MultiplyTR = Convert.ToSingle(serverSetting.Value);
					break;
				case "MultiplyEXP":
					ServerSettings.MultiplyEXP = Convert.ToSingle(serverSetting.Value);
					break;
				case "SurvivalMaxUserNum":
					ServerSettings.SurvivalMaxUserNum = Convert.ToByte(serverSetting.Value);
					break;
				case "SurvivalMinUserNum":
					ServerSettings.SurvivalMinUserNum = Convert.ToByte(serverSetting.Value);
					break;
				case "GateNoticeURL":
					ServerSettings.GateNoticeURL = serverSetting.Value;
					break;
				case "QuitConfirmDialogURL":
					ServerSettings.QuitConfirmDialogURL = serverSetting.Value;
					break;
				case "cashFillUpURL":
					ServerSettings.cashFillUpURL = serverSetting.Value;
					break;
				case "EveryDayEventURL":
					ServerSettings.EveryDayEventURL = serverSetting.Value;
					break;
				case "LoadingTimeOutMilliSeconds":
					ServerSettings.LoadingTimeOutMilliSeconds = Convert.ToInt32(serverSetting.Value);
					break;
				case "RABBIT_TURTLE_FATIGUE_DEC":
					ServerSettings.RABBIT_TURTLE_FATIGUE_DEC = Convert.ToSingle(serverSetting.Value);
					break;
				case "RABBIT_TURTLE_FATIGUE_INC":
					ServerSettings.RABBIT_TURTLE_FATIGUE_INC = Convert.ToSingle(serverSetting.Value);
					break;
				case "RABBIT_TURTLE_ITEM_FATIGUE_DEC":
					ServerSettings.RABBIT_TURTLE_ITEM_FATIGUE_DEC = Convert.ToSingle(serverSetting.Value);
					break;
				case "RABBIT_TURTLE_ITEM_FATIGUE_INC":
					ServerSettings.RABBIT_TURTLE_ITEM_FATIGUE_INC = Convert.ToSingle(serverSetting.Value);
					break;
				case "corunModeMinPlayerNum":
					ServerSettings.corunModeMinPlayerNum = Convert.ToByte(serverSetting.Value);
					break;
				case "corunModeDecreaseEnergyRatio":
					ServerSettings.corunModeDecreaseEnergyRatio = Convert.ToInt32(serverSetting.Value);
					break;
				case "useMapNumByRoomKindIDCheck":
					ServerSettings.useMapNumByRoomKindIDCheck = Convert.ToBoolean(serverSetting.Value);
					break;
				case "dungeonRaidMaxUserNum":
					ServerSettings.dungeonRaidMaxUserNum = Convert.ToByte(serverSetting.Value);
					break;
				case "dungeonRaidMinUserNum":
					ServerSettings.dungeonRaidMinUserNum = Convert.ToByte(serverSetting.Value);
					break;
				case "dungeonRaidGotPointConst":
					ServerSettings.dungeonRaidGotPointConst = Convert.ToSingle(serverSetting.Value);
					break;
				case "dungeonRaidBalanceConst":
					ServerSettings.dungeonRaidBalanceConst = Convert.ToInt32(serverSetting.Value);
					break;
				case "competitionMaxUserNum":
					ServerSettings.competitionMaxUserNum = Convert.ToByte(serverSetting.Value);
					break;
				case "competitionMinUserNum":
					ServerSettings.competitionMinUserNum = Convert.ToByte(serverSetting.Value);
					break;
				case "countrycode":
					ServerSettings.countrycode = serverSetting.Value;
					break;
				case "GiftItemLimitLevel":
					ServerSettings.GiftItemLimitLevel = Convert.ToInt32(serverSetting.Value);
					break;
				case "GMRoomMaxUserNum":
					ServerSettings.GMRoomMaxUserNum = Convert.ToByte(serverSetting.Value);
					break;
				case "competitionEventOn":
					ServerSettings.competitionEventOn = Convert.ToBoolean(serverSetting.Value);
					break;
				case "useCompetitionEvent":
					ServerSettings.useCompetitionEvent = Convert.ToInt32(serverSetting.Value);
					break;
				case "competitionEventMaxUserNum":
					ServerSettings.competitionEventMaxUserNum = Convert.ToByte(serverSetting.Value);
					break;
				case "competitionEventMinUserNum":
					ServerSettings.competitionEventMinUserNum = Convert.ToByte(serverSetting.Value);
					break;
				case "GuildMatchPartyMemberCount":
					ServerSettings.GuildMatchPartyMemberCount = Convert.ToInt32(serverSetting.Value);
					break;
				case "GuildMarkRegisterURL":
					ServerSettings.GuildMarkRegisterURL = serverSetting.Value;
					break;
				case "TowerEventTime":
					ServerSettings.TowerEventTime = Convert.ToInt32(serverSetting.Value);
					break;
				case "useThankOfferingSystem":
					ServerSettings.useThankOfferingSystem = Convert.ToBoolean(serverSetting.Value);
					break;
				case "ThankOfferingSchedule_CurNum":
					ServerSettings.ThankOfferingSchedule_CurNum = Convert.ToInt32(serverSetting.Value);
					break;
				case "useEasyAntiCheat":
					ServerSettings.useEasyAntiCheat = Convert.ToBoolean(serverSetting.Value);
					break;
				case "competitionEventUsingRoomKind":
					ServerSettings.competitionEventUsingRoomKind = serverSetting.Value.ToString();
					break;
				}
			}
		}
	}
}
