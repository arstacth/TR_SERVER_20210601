using System;
using System.Data;
using System.Linq;
using AgentServer.Holders;
using AgentServer.Network.Connections;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using AgentServer.Structuring.Map;
using LocalCommons.Network;
using LocalCommons.Utilities;
using MySql.Data.MySqlClient;

namespace AgentServer.Packet
{
	public class SingleChallengeHandle
	{
		public static void Handle_GetUserInfo(ClientConnection Client, PacketReader reader, byte last)
		{
			_ = Client.CurrentAccount;
			int fixedLength = reader.ReadLEInt16();
			string nickName = reader.ReadBig5StringSafe(fixedLength);
			Client.SendAsync(new SingleChallengeUserInfo(nickName, last));
		}

		public static void Handle_StartChallenge(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			if (SingleChallengeHolder.MapInfos.ContainsKey(num))
			{
				ChallengeStart(currentAccount, num);
				currentAccount.ChallengeMapNum = num;
				currentAccount.ChallengeStartTime = Utility.CurrentTimeMilliseconds();
				Client.SendAsync(new StartChallengeOK(last));
			}
		}

		public static void Handle_ChallengeAction(ClientConnection Client, PacketReader reader, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			int num = reader.ReadLEInt32();
			int finishTime = (int)(Utility.CurrentTimeMilliseconds() - currentAccount.ChallengeStartTime);
			if (num == 0)
			{
				if (SingleChallengeHolder.MapInfos.TryGetValue(currentAccount.ChallengeMapNum, out var value))
				{
					byte medalType = value.OrderBy((ChallengeMapInfo o) => o.GoalSec).FirstOrDefault((ChallengeMapInfo f) => f.GoalSec >= finishTime)?.MedalType ?? 0;
					Client.SendAsync(new SingleChallengeGoalInOK(currentAccount.UserNum, currentAccount.ChallengeMapNum, medalType, finishTime, last));
				}
			}
			else
			{
				Client.SendAsync(new SingleChallengeAction(num, last));
			}
		}

		private static void ChallengeStart(Account User, int MapNum)
		{
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_SingleChallenge_Start";
			mySqlCommand.Parameters.Add("UserNum", MySqlDbType.Int32).Value = User.UserNum;
			mySqlCommand.Parameters.Add("MapNum", MySqlDbType.Int32).Value = MapNum;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
			mySqlDataReader.Read();
			User.TR -= Convert.ToInt32(mySqlDataReader["TryCost"]);
		}
	}
}
