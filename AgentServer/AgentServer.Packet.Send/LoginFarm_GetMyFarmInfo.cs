using System;
using System.Data;
using AgentServer.Structuring;
using AgentServer.Structuring.Farm;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using LocalCommons.Utilities;
using MySql.Data.MySqlClient;

namespace AgentServer.Packet.Send
{
	public sealed class LoginFarm_GetMyFarmInfo : NetPacket
	{
		public LoginFarm_GetMyFarmInfo(Account User, byte last)
		{
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_Farm_GetMyFarmInfo";
				mySqlCommand.Parameters.Add("pUserNum", MySqlDbType.Int32).Value = User.UserNum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				if (mySqlDataReader.HasRows)
				{
					mySqlDataReader.Read();
					User.MyFarmUniqueNum = Convert.ToInt32(mySqlDataReader["FarmIndex"]);
					MyFarmInfo myFarmInfo2 = (User.MyFarmInfo = new MyFarmInfo
					{
						FarmTypeNum = Convert.ToInt32(mySqlDataReader["FarmTypeNum"]),
						FarmSkyTypeNum = 0,
						FarmWeatherTypeNum = 0,
						FarmName = mySqlDataReader["FarmName"].ToString(),
						MasterName = mySqlDataReader["MasterName"].ToString(),
						ExpireTime = (Convert.IsDBNull(mySqlDataReader["ExpireDateTime"]) ? 1842465389770955L : Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["ExpireDateTime"]))),
						CreateTime = (Convert.IsDBNull(mySqlDataReader["CreateDateTime"]) ? 1842465389770955L : Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["CreateDateTime"]))),
						isPublic = false,
						TotalCount = Convert.ToInt32(mySqlDataReader["TotalVisitedCount"]),
						TodaysVisitorCount = Convert.ToInt32(mySqlDataReader["TodaysVisitorCount"]),
						PremiumFarmUsing = false,
						PremiumFarmExpireDateTime = 0L,
						farmExp = Convert.ToInt32(mySqlDataReader["farmExp"])
					});
				}
			}
			ns.WriteOP(Opcodes.eServer_FARM_ACK);
			ns.WriteOP(FarmProtocol.GetMyFarmInfo_ACK);
			ns.Write(0);
			ns.Write(User.MyFarmUniqueNum);
			ns.Write(User.MyFarmInfo.FarmWeatherTypeNum);
			ns.Write(User.MyFarmInfo.FarmSkyTypeNum);
			ns.Write(User.MyFarmInfo.FarmTypeNum);
			ns.WriteBIG5Fixed_intSize(User.MyFarmInfo.FarmName);
			ns.WriteBIG5Fixed_intSize(User.MyFarmInfo.MasterName);
			ns.Write(User.MyFarmInfo.ExpireTime);
			ns.Write(User.MyFarmInfo.CreateTime);
			ns.Write((byte)1);
			ns.Write(User.MyFarmInfo.isPublic);
			ns.Write((byte)0);
			ns.Write(User.MyFarmInfo.TotalCount);
			ns.Write(User.MyFarmInfo.TodaysVisitorCount);
			ns.Fill(3);
			ns.Write(User.MyFarmInfo.PremiumFarmUsing);
			ns.Write(User.MyFarmInfo.PremiumFarmExpireDateTime);
			ns.Write(User.MyFarmInfo.farmExp);
			ns.Write(0);
			ns.Write(last);
		}
	}
}
