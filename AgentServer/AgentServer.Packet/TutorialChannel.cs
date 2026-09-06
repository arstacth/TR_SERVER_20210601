using System;
using System.Collections.Generic;
using System.Data;
using AgentServer.Network.Connections;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Item;
using AgentServer.Structuring.User;
using LocalCommons.Network;
using MySql.Data.MySqlClient;
using Serilog;

namespace AgentServer.Packet
{
	public class TutorialChannel
	{
		public static void Handle_GetUserInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			TutorialChannel_GerUserInfo(Client.CurrentAccount.UserNum, out var infos);
			Client.SendAsync(new TutorialChannel_UserInfo(infos, last));
		}

		public static void Handle_RequestReward(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int level = reader.ReadLEInt32();
			int type = reader.ReadLEInt32();
			if (TutorialChannel_GiveReward(currentAccount.UserNum, type, level, out var exinfo))
			{
				currentAccount.TRNeedUpdateFromDB = true;
				currentAccount.EXPNeedUpdateFromDB = true;
				Client.SendAsync(new TutorialChannel_GiveRewardInfo(exinfo, last));
			}
		}

		private static void TutorialChannel_GerUserInfo(int UserNum, out List<TutorialChannelInfo> infos)
		{
			infos = new List<TutorialChannelInfo>();
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_TutorialChannel_GerUserInfo";
				mySqlCommand.Parameters.Add("pUserNum", MySqlDbType.Int32).Value = UserNum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					TutorialChannelInfo item = new TutorialChannelInfo
					{
						Type = mySqlDataReader.GetInt32("fdType"),
						Level = mySqlDataReader.GetInt32("fdLevel")
					};
					infos.Add(item);
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_TutorialChannel_GerUserInfo Error: {0}", ex.Message);
			}
		}

		private static bool TutorialChannel_GiveReward(int UserNum, int Type, int Level, out List<ExchangeItemInfo> exinfo)
		{
			exinfo = new List<ExchangeItemInfo>();
			try
			{
				using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
				{
					mySqlConnection.Open();
					using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
					mySqlCommand.Parameters.Clear();
					mySqlCommand.CommandType = CommandType.StoredProcedure;
					mySqlCommand.CommandText = "usp_TutorialChannel_GiveReward";
					mySqlCommand.Parameters.Add("pUserNum", MySqlDbType.Int32).Value = UserNum;
					mySqlCommand.Parameters.Add("pType", MySqlDbType.Int32).Value = Type;
					mySqlCommand.Parameters.Add("pLevel", MySqlDbType.Int32).Value = Level;
					using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
					if (mySqlDataReader.HasRows)
					{
						while (mySqlDataReader.Read())
						{
							ExchangeItemInfo item = new ExchangeItemInfo
							{
								type = mySqlDataReader.GetInt32("fdRewardType"),
								id = mySqlDataReader.GetInt32("fdRewardItem"),
								count = mySqlDataReader.GetInt32("fdRewardCount")
							};
							exinfo.Add(item);
						}
						return true;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				Log.Error("usp_TutorialChannel_GiveReward Error: {0}", ex.Message);
				return false;
			}
		}
	}
}
