using System;
using System.Collections.Generic;
using System.Data;
using AgentServer.Network.Connections;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.User;
using LocalCommons.Network;
using MySql.Data.MySqlClient;
using Serilog;

namespace AgentServer.Packet
{
	public class RankHandle
	{
		public static void Handle_GetRankInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			byte b = reader.ReadByte();
			int startRank = reader.ReadLEInt32();
			int showcount = reader.ReadLEInt32();
			byte b2 = reader.ReadByte();
			eRequestRankKind eRequestRankKind = (eRequestRankKind)b;
			switch (eRequestRankKind)
			{
			case eRequestRankKind.eRequestRankKind_NORMAL:
			case eRequestRankKind.eRequestRankKind_FARM:
			case eRequestRankKind.eRequestRankKind_ITEM_COLLECTION:
			{
				if (eRequestRankKind == eRequestRankKind.eRequestRankKind_NORMAL)
				{
					b2 = 0;
				}
				DB_getRankRange(eRequestRankKind, bSearchByNickname: false, startRank, showcount, b2, string.Empty, out var ranklist2);
				Client.SendAsync(new GetRankInfo_ALL(eRequestRankKind, ranklist2, last));
				break;
			}
			case eRequestRankKind.eRequestRankKind_COUPLE:
			{
				coupleGetRankRange(startRank, showcount, b2, out var ranklist);
				Client.SendAsync(new GetCoupleRankRange_ACK(b, ranklist, last));
				break;
			}
			default:
				Log.Warning("incorrect rankKind:{0}", b);
				break;
			}
		}

		public static void Handle_GetMyRankInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			byte b = reader.ReadByte();
			byte b2 = reader.ReadByte();
			eRequestRankKind eRequestRankKind = (eRequestRankKind)b;
			switch (eRequestRankKind)
			{
			case eRequestRankKind.eRequestRankKind_NORMAL:
			case eRequestRankKind.eRequestRankKind_FARM:
			case eRequestRankKind.eRequestRankKind_ITEM_COLLECTION:
			{
				if (eRequestRankKind == eRequestRankKind.eRequestRankKind_NORMAL)
				{
					b2 = 0;
				}
				DB_getMyRank(eRequestRankKind, currentAccount.NickName, b2, out var myrank);
				Client.SendAsync(new GetMyRankInfo_ALL(eRequestRankKind, myrank, last));
				break;
			}
			case eRequestRankKind.eRequestRankKind_COUPLE:
			{
				coupleGetRankNickName(currentAccount.NickName, b2, out var info);
				Client.SendAsync(new GetMyCoupleRankInfo_ACK(b, info, last));
				break;
			}
			default:
				Log.Warning("incorrect rankKind:{0}", b);
				break;
			}
		}

		public static void Handle_SearchRank(ClientConnection Client, PacketReader reader, byte last)
		{
			byte b = reader.ReadByte();
			int fixedLength = reader.ReadLEInt16();
			string text = reader.ReadBig5StringSafe(fixedLength);
			int showcount = reader.ReadLEInt32();
			byte b2 = reader.ReadByte();
			eRequestRankKind eRequestRankKind = (eRequestRankKind)b;
			switch (eRequestRankKind)
			{
			case eRequestRankKind.eRequestRankKind_NORMAL:
			case eRequestRankKind.eRequestRankKind_FARM:
			case eRequestRankKind.eRequestRankKind_ITEM_COLLECTION:
			{
				if (eRequestRankKind == eRequestRankKind.eRequestRankKind_NORMAL)
				{
					b2 = 0;
				}
				DB_getRankRange(eRequestRankKind, bSearchByNickname: true, 0, showcount, b2, text, out var ranklist2);
				Client.SendAsync(new GetRankInfo_ALL(eRequestRankKind, ranklist2, last));
				break;
			}
			case eRequestRankKind.eRequestRankKind_COUPLE:
			{
				coupleGetRankSearch(text, showcount, b2, out var ranklist);
				Client.SendAsync(new GetCoupleRankRange_ACK(b, ranklist, last));
				break;
			}
			default:
				Log.Warning("incorrect rankKind:{0}", b);
				break;
			}
		}

		private static void DB_getRankRange(eRequestRankKind rankKind, bool bSearchByNickname, int startRank, int showcount, byte rankDetailKind, string strSearchNickname, out List<CRankListData> ranklist)
		{
			ranklist = new List<CRankListData>();
			int num = startRank + showcount - 1;
			string text = string.Empty;
			if (bSearchByNickname)
			{
				switch (rankKind)
				{
				case eRequestRankKind.eRequestRankKind_NORMAL:
					text += "usp_Rank_Search";
					break;
				case eRequestRankKind.eRequestRankKind_SPORTS:
					text += "usp_sportsGetRankSearch";
					break;
				case eRequestRankKind.eRequestRankKind_FARM:
					text += "usp_Farm_getRankSearch";
					break;
				case eRequestRankKind.eRequestRankKind_COUPLE:
					text += "usp_coupleGetRankSearch";
					break;
				case eRequestRankKind.eRequestRankKind_ITEM_COLLECTION:
					text += "usp_itemCollection_getRankSearch";
					break;
				}
			}
			else
			{
				switch (rankKind)
				{
				case eRequestRankKind.eRequestRankKind_NORMAL:
					text += "usp_Rank_getRange";
					break;
				case eRequestRankKind.eRequestRankKind_SPORTS:
					text += "usp_sportsGetRankRange";
					break;
				case eRequestRankKind.eRequestRankKind_FARM:
					text += "usp_Farm_getRankRange";
					break;
				case eRequestRankKind.eRequestRankKind_COUPLE:
					text += "usp_coupleGetRankRange";
					break;
				case eRequestRankKind.eRequestRankKind_ITEM_COLLECTION:
					text += "usp_itemCollection_getRankRange";
					break;
				}
			}
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = text;
				if (bSearchByNickname)
				{
					switch (rankKind)
					{
					case eRequestRankKind.eRequestRankKind_NORMAL:
					case eRequestRankKind.eRequestRankKind_COUPLE:
						mySqlCommand.Parameters.Add("usernickname", MySqlDbType.VarString).Value = strSearchNickname;
						mySqlCommand.Parameters.Add("showcount", MySqlDbType.Int32).Value = showcount;
						mySqlCommand.Parameters.Add("detailRank", MySqlDbType.Int32).Value = rankDetailKind;
						break;
					case eRequestRankKind.eRequestRankKind_FARM:
						mySqlCommand.Parameters.Add("usernickname", MySqlDbType.VarString).Value = strSearchNickname;
						mySqlCommand.Parameters.Add("showcount", MySqlDbType.Int32).Value = showcount;
						break;
					default:
						mySqlCommand.Parameters.Add("nickName", MySqlDbType.VarString).Value = strSearchNickname;
						mySqlCommand.Parameters.Add("showCount", MySqlDbType.Int32).Value = showcount;
						break;
					}
				}
				else
				{
					switch (rankKind)
					{
					case eRequestRankKind.eRequestRankKind_NORMAL:
					case eRequestRankKind.eRequestRankKind_COUPLE:
						mySqlCommand.Parameters.Add("startRank", MySqlDbType.Int32).Value = startRank;
						mySqlCommand.Parameters.Add("showCount", MySqlDbType.Int32).Value = num;
						mySqlCommand.Parameters.Add("detailRank", MySqlDbType.Int32).Value = rankDetailKind;
						break;
					case eRequestRankKind.eRequestRankKind_ITEM_COLLECTION:
						mySqlCommand.Parameters.Add("startRank", MySqlDbType.Int32).Value = startRank;
						mySqlCommand.Parameters.Add("showCount", MySqlDbType.Int32).Value = showcount;
						break;
					default:
						mySqlCommand.Parameters.Add("startRank", MySqlDbType.Int32).Value = startRank;
						mySqlCommand.Parameters.Add("endRank", MySqlDbType.Int32).Value = num;
						break;
					}
				}
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				if (eRequestRankKind.eRequestRankKind_ITEM_COLLECTION == rankKind)
				{
					while (mySqlDataReader.Read())
					{
						CRankListData item = new CRankListData
						{
							m_ranking = mySqlDataReader.GetInt32("rank"),
							m_nickname = mySqlDataReader.GetString("nickname"),
							m_experienceValue = mySqlDataReader.GetInt64("point"),
							m_numbers = 0
						};
						ranklist.Add(item);
					}
				}
				else if (rankKind == eRequestRankKind.eRequestRankKind_NORMAL)
				{
					while (mySqlDataReader.Read())
					{
						CRankListData item2 = new CRankListData
						{
							m_ranking = mySqlDataReader.GetInt32("Rank"),
							m_nickname = mySqlDataReader.GetString("nickname"),
							m_experienceValue = mySqlDataReader.GetInt64("EXP"),
							m_experienceValue2 = mySqlDataReader.GetInt64("EXP"),
							m_numbers = 0
						};
						ranklist.Add(item2);
					}
				}
				else
				{
					while (mySqlDataReader.Read())
					{
						CRankListData item3 = new CRankListData
						{
							m_ranking = mySqlDataReader.GetInt32("RANK"),
							m_nickname = mySqlDataReader.GetString("nickname"),
							m_experienceValue = mySqlDataReader.GetInt64("EXP"),
							m_numbers = 0
						};
						ranklist.Add(item3);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("{0} Error:{1}", text, ex.Message);
			}
		}

		private static void DB_getMyRank(eRequestRankKind rankKind, string strSearchNickname, byte rankDetailKind, out CRankListData myrank)
		{
			myrank = null;
			string text = string.Empty;
			switch (rankKind)
			{
			case eRequestRankKind.eRequestRankKind_NORMAL:
				text += "usp_Rank_getNickname";
				break;
			case eRequestRankKind.eRequestRankKind_SPORTS:
				text += "usp_sportsGetRankNickname";
				break;
			case eRequestRankKind.eRequestRankKind_FARM:
				text += "usp_Farm_getRankNickName";
				break;
			case eRequestRankKind.eRequestRankKind_COUPLE:
				text += "usp_coupleGetRankNickName";
				break;
			case eRequestRankKind.eRequestRankKind_ITEM_COLLECTION:
				text += "usp_itemCollection_getRankSearch";
				break;
			}
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = text;
				switch (rankKind)
				{
				case eRequestRankKind.eRequestRankKind_NORMAL:
				case eRequestRankKind.eRequestRankKind_COUPLE:
					mySqlCommand.Parameters.Add("nickname", MySqlDbType.VarString).Value = strSearchNickname;
					mySqlCommand.Parameters.Add("detailRank", MySqlDbType.Int32).Value = rankDetailKind;
					break;
				case eRequestRankKind.eRequestRankKind_ITEM_COLLECTION:
					mySqlCommand.Parameters.Add("nickname", MySqlDbType.VarString).Value = strSearchNickname;
					mySqlCommand.Parameters.Add("showCount", MySqlDbType.Int32).Value = 1;
					break;
				default:
					mySqlCommand.Parameters.Add("nickname", MySqlDbType.VarString).Value = strSearchNickname;
					break;
				}
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				if (eRequestRankKind.eRequestRankKind_ITEM_COLLECTION == rankKind)
				{
					if (mySqlDataReader.HasRows && mySqlDataReader.Read())
					{
						myrank = new CRankListData();
						myrank.m_nickname = strSearchNickname;
						myrank.m_ranking = mySqlDataReader.GetInt32("rank");
						myrank.m_experienceValue = mySqlDataReader.GetInt64("point");
					}
				}
				else if (rankKind == eRequestRankKind.eRequestRankKind_NORMAL)
				{
					if (mySqlDataReader.HasRows && mySqlDataReader.Read())
					{
						myrank = new CRankListData();
						myrank.m_nickname = strSearchNickname;
						myrank.m_ranking = mySqlDataReader.GetInt32("Rank");
						myrank.m_experienceValue = mySqlDataReader.GetInt64("EXP");
						myrank.m_experienceValue2 = mySqlDataReader.GetInt64("EXP");
					}
				}
				else if (mySqlDataReader.HasRows && mySqlDataReader.Read())
				{
					myrank = new CRankListData();
					myrank.m_nickname = strSearchNickname;
					myrank.m_ranking = mySqlDataReader.GetInt32("RANK");
					myrank.m_experienceValue = mySqlDataReader.GetInt64("EXP");
				}
			}
			catch (Exception ex)
			{
				Log.Error("{0} Error:{1}", text, ex.Message);
			}
		}

		private static void coupleGetRankRange(int startRank, int showcount, short detailRank, out List<CoupleRankInfo> ranklist)
		{
			ranklist = new List<CoupleRankInfo>();
			int num = startRank + showcount - 1;
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_coupleGetRankRange";
				mySqlCommand.Parameters.Add("startRank", MySqlDbType.Int32).Value = startRank;
				mySqlCommand.Parameters.Add("endRank", MySqlDbType.Int32).Value = num;
				mySqlCommand.Parameters.Add("detailRank", MySqlDbType.Int16).Value = detailRank;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					CoupleRankInfo item = new CoupleRankInfo
					{
						rank = mySqlDataReader.GetInt32("rank"),
						coupleNum = mySqlDataReader.GetInt32("coupleNum"),
						point = mySqlDataReader.GetInt32("point"),
						femaleNickName = mySqlDataReader.GetString("femaleNickName"),
						maleNickName = mySqlDataReader.GetString("maleNickName"),
						level = mySqlDataReader.GetInt32("level")
					};
					ranklist.Add(item);
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_coupleGetRankRange Error:{0}", ex.Message);
			}
		}

		private static void coupleGetRankNickName(string nickname, short detailRank, out CoupleRankInfo info)
		{
			info = null;
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_coupleGetRankNickName";
				mySqlCommand.Parameters.Add("nickname", MySqlDbType.VarString).Value = nickname;
				mySqlCommand.Parameters.Add("detailRank", MySqlDbType.Int16).Value = detailRank;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				if (mySqlDataReader.HasRows)
				{
					mySqlDataReader.Read();
					info = new CoupleRankInfo
					{
						rank = mySqlDataReader.GetInt32("rank"),
						coupleNum = mySqlDataReader.GetInt32("coupleNum"),
						point = mySqlDataReader.GetInt32("point"),
						femaleNickName = mySqlDataReader.GetString("femaleNickName"),
						maleNickName = mySqlDataReader.GetString("maleNickName"),
						level = mySqlDataReader.GetInt32("level")
					};
				}
			}
			catch (Exception ex)
			{
				info = null;
				Log.Error("usp_coupleGetRankNickName Error:{0}", ex.Message);
			}
		}

		private static void coupleGetRankSearch(string nickname, int showcount, short detailRank, out List<CoupleRankInfo> ranklist)
		{
			ranklist = new List<CoupleRankInfo>();
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_coupleGetRankSearch";
				mySqlCommand.Parameters.Add("usernickname", MySqlDbType.VarString).Value = nickname;
				mySqlCommand.Parameters.Add("showcount", MySqlDbType.Int32).Value = showcount;
				mySqlCommand.Parameters.Add("detailRank", MySqlDbType.Int16).Value = detailRank;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					CoupleRankInfo item = new CoupleRankInfo
					{
						rank = mySqlDataReader.GetInt32("rank"),
						coupleNum = mySqlDataReader.GetInt32("coupleNum"),
						point = mySqlDataReader.GetInt32("point"),
						femaleNickName = mySqlDataReader.GetString("femaleNickName"),
						maleNickName = mySqlDataReader.GetString("maleNickName"),
						level = mySqlDataReader.GetInt32("level")
					};
					ranklist.Add(item);
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_coupleGetRankSearch Error:{0}", ex.Message);
			}
		}
	}
}
