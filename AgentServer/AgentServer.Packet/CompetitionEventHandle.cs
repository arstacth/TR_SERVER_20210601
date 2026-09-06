using System;
using System.Collections.Generic;
using System.Data;
using AgentServer.Holders;
using AgentServer.Network.Connections;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Item;
using LocalCommons.Network;
using MySql.Data.MySqlClient;
using Serilog;

namespace AgentServer.Packet
{
	public class CompetitionEventHandle
	{
		public enum eCompetitionEventResult
		{
			eCompetitionEventResult_OK,
			eCompetitionEventResult_DBERROR,
			eCompetitionEventResult_ALREADY_RECV_REWARD,
			eCompetitionEventResult_NOT_WINNER_PARTY,
			eCompetitionEventResult_ALREADY_RECV_POINT_REWARD,
			eCompetitionEventResult_NOT_ENOUGH_POINT,
			eCompetitionEventResult_NOT_NEXT_POINT_REWARD,
			eCompetitionEventResult_REWARD_LIMIT_TIME_OVER,
			eCompetitionEventResult_JOIN_PARTY_FAILED_INVALID_GENDER,
			eCompetitionEventResult_CANT_JOIN_SELECT_PARTY,
			eCompetitionEventResult_NOT_FOUND_TERRITORY_INFO,
			eCompetitionEventResult_NO_SEASONPASS_FOR_REWARD
		}

		private static readonly object eventJoinLock = new object();

		public static void Handle_GetPartyPointInfo(ClientConnection Client, byte last)
		{
			_ = Client.CurrentAccount;
			competitionEvent_partyInfo(out var CompetitionPartyInfo);
			Client.SendAsync(new CompetitionEvent_PartyInfo(CompetitionPartyInfo, last));
		}

		public static void Handle_PartyJoin(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			if (!ServerSettingHolder.ServerSettings.competitionEventOn)
			{
				return;
			}
			lock (eventJoinLock)
			{
				competitionEvent_partyJoin(currentAccount.UserNum, out var exinfo, out var joinPartyType, out var SubPartyType, out var result);
				Client.SendAsync(new CompetitionEvent_PartyJoinOK(result, joinPartyType, SubPartyType, exinfo, last));
				if (result == eCompetitionEventResult.eCompetitionEventResult_OK)
				{
					currentAccount.PartyType = (short)joinPartyType;
					currentAccount.SubPartyType = SubPartyType;
				}
			}
		}

		public static void Handle_PartyUserInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			competitionEvent_partyUserInfo(currentAccount.UserNum, out var SubPartyType, out var CompetitionPartyInfo);
			Client.SendAsync(new CompetitionEvent_PartyUserInfo(currentAccount.PartyType, SubPartyType, CompetitionPartyInfo, last));
		}

		public static void Handle_TodayGameInfo(ClientConnection Client, byte last)
		{
			Client.SendAsync(new CompetitionEvent_TodayGameInfo(last));
		}

		public static void Handle_FronTier_SchduleInfo(ClientConnection Client, byte last)
		{
			Client.SendAsync(new FronTier_SchduleInfo(last));
		}

		public static void Handle_SEASON_CHANNEL_SCHEDULE_REQ(ClientConnection Client, byte last)
		{
			Client.SendAsync(new SEASON_CHANNEL_SCHEDULE_ACK(last));
		}

		public static void Handle_PointReward(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			short rewardLevel = reader.ReadLEInt16();
			bool flag = reader.ReadBoolean();
			if (flag && !currentAccount.activeItem.HasPosition(820))
			{
				Client.SendAsync(new CompetitionEvent_PointReward(eCompetitionEventResult.eCompetitionEventResult_NO_SEASONPASS_FOR_REWARD, rewardLevel, flag, null, last));
			}
			else if (ServerSettingHolder.ServerSettings.competitionEventOn)
			{
				competitionEvent_pointReward(currentAccount.UserNum, rewardLevel, flag, out var exinfo, out var result);
				Client.SendAsync(new CompetitionEvent_PointReward(result, rewardLevel, flag, exinfo, last));
			}
		}

		public static void Handle_GetRoomKindPlayerNum(ClientConnection Client, byte last)
		{
			Client.SendAsync(new CompetitionEvent_RoomKindPlayerNum(last));
		}

		private static void competitionEvent_partyJoin(int UserNum, out List<ExchangeItemInfo> exinfo, out int joinPartyType, out int SubPartyType, out eCompetitionEventResult result)
		{
			exinfo = new List<ExchangeItemInfo>();
			joinPartyType = 0;
			SubPartyType = 0;
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_competitionEvent_partyJoin";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = UserNum;
				mySqlCommand.Parameters.Add("eventType", MySqlDbType.Int16).Value = ServerSettingHolder.ServerSettings.useCompetitionEvent;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					ExchangeItemInfo item = new ExchangeItemInfo
					{
						type = Convert.ToInt32(mySqlDataReader["rewardType"]),
						id = Convert.ToInt32(mySqlDataReader["rewardItem"]),
						count = Convert.ToInt32(mySqlDataReader["rewardCount"])
					};
					exinfo.Add(item);
				}
				mySqlDataReader.NextResult();
				mySqlDataReader.Read();
				joinPartyType = mySqlDataReader.GetInt16("joinPartyType");
				SubPartyType = mySqlDataReader.GetInt16("SubPartyType");
				result = eCompetitionEventResult.eCompetitionEventResult_OK;
			}
			catch (MySqlException ex)
			{
				result = eCompetitionEventResult.eCompetitionEventResult_DBERROR;
				Log.Error("usp_competitionEvent_partyJoin Error:{0}", ex.Message);
			}
			catch (Exception ex2)
			{
				result = eCompetitionEventResult.eCompetitionEventResult_DBERROR;
				Log.Error("usp_competitionEvent_partyJoin Error:{0}", ex2.Message);
			}
		}

		private static void competitionEvent_partyUserInfo(int UserNum, out int SubPartyType, out List<CompetitionPartyUserInfo> CompetitionPartyInfo)
		{
			SubPartyType = 0;
			CompetitionPartyInfo = new List<CompetitionPartyUserInfo>();
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_competitionEvent_partyUserInfo";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = UserNum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					CompetitionPartyUserInfo item = new CompetitionPartyUserInfo
					{
						point = Convert.ToInt32(mySqlDataReader["point"]),
						accPoint = Convert.ToInt32(mySqlDataReader["accPoint"]),
						rewardLevel = Convert.ToByte(mySqlDataReader["rewardLevel"]),
						eventType = Convert.ToByte(mySqlDataReader["eventType"]),
						ReceivedSeasonPassRewardLevel = Convert.ToByte(mySqlDataReader["ReceivedSeasonPassRewardLevel"])
					};
					SubPartyType = Convert.ToInt32(mySqlDataReader["SubPartyType"]);
					CompetitionPartyInfo.Add(item);
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_competitionEvent_partyUserInfo Error:{0}", ex.Message);
			}
		}

		private static void competitionEvent_partyInfo(out List<CompetitionPartyInfo> CompetitionPartyInfo)
		{
			CompetitionPartyInfo = new List<CompetitionPartyInfo>();
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_competitionEvent_partyInfo";
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					CompetitionPartyInfo item = new CompetitionPartyInfo
					{
						partyType = Convert.ToInt32(mySqlDataReader["partyType"]),
						point = mySqlDataReader.GetInt64("point"),
						userDiffPercent = mySqlDataReader.GetInt32("userDiffPercent")
					};
					CompetitionPartyInfo.Add(item);
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_competitionEvent_partyInfo Error:{0}", ex.Message);
			}
		}

		private static void competitionEvent_pointReward(int UserNum, short rewardLevel, bool bUseSeasonPass, out List<ExchangeItemInfo> exinfo, out eCompetitionEventResult result)
		{
			exinfo = new List<ExchangeItemInfo>();
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_competitionEvent_pointReward";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = UserNum;
				mySqlCommand.Parameters.Add("eventType", MySqlDbType.Int16).Value = ServerSettingHolder.ServerSettings.useCompetitionEvent;
				mySqlCommand.Parameters.Add("rewardLevel", MySqlDbType.Int16).Value = rewardLevel;
				mySqlCommand.Parameters.Add("useSeasonPass", MySqlDbType.Int32).Value = bUseSeasonPass;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					ExchangeItemInfo item = new ExchangeItemInfo
					{
						type = Convert.ToInt32(mySqlDataReader["rewardType"]),
						id = Convert.ToInt32(mySqlDataReader["rewardItem"]),
						count = Convert.ToInt32(mySqlDataReader["rewardCount"])
					};
					exinfo.Add(item);
				}
				result = eCompetitionEventResult.eCompetitionEventResult_OK;
			}
			catch (MySqlException ex)
			{
				result = eCompetitionEventResult.eCompetitionEventResult_DBERROR;
				if (ex.Message.Contains("already recv point reward"))
				{
					result = eCompetitionEventResult.eCompetitionEventResult_ALREADY_RECV_POINT_REWARD;
				}
				else if (ex.Message.Contains("not next point reward"))
				{
					result = eCompetitionEventResult.eCompetitionEventResult_NOT_NEXT_POINT_REWARD;
				}
				else if (ex.Message.Contains("not enough point"))
				{
					result = eCompetitionEventResult.eCompetitionEventResult_NOT_ENOUGH_POINT;
				}
				Log.Error("usp_competitionEvent_pointReward Error:{0}", ex.Message);
			}
			catch (Exception ex2)
			{
				result = eCompetitionEventResult.eCompetitionEventResult_DBERROR;
				Log.Error("usp_competitionEvent_pointReward Error:{0}", ex2.Message);
			}
		}
	}
}
