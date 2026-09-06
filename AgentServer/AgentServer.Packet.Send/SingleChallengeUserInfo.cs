using System;
using System.Data;
using System.IO;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using MySql.Data.MySqlClient;

namespace AgentServer.Packet.Send
{
	public sealed class SingleChallengeUserInfo : NetPacket
	{
		public SingleChallengeUserInfo(string NickName, byte last)
		{
			ns.WriteOP(Opcodes.eServer_GET_USER_INFO_ACK__CHALLENGE_FULLRECORD);
			ns.Write(1);
			ns.WriteBIG5Fixed_intSize(NickName);
			int num = 0;
			int num2 = (int)ns.Position;
			ns.Write(num);
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_SingleChallenge_GetUserInfo";
				mySqlCommand.Parameters.Add("NickName", MySqlDbType.VarString).Value = NickName;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					ns.Write(Convert.ToInt32(mySqlDataReader["fdMapNum"]));
					ns.Write(Convert.ToInt32(mySqlDataReader["fdMedalType"]));
					ns.Write(Convert.ToInt32(mySqlDataReader["fdGoalSec"]));
					num++;
				}
			}
			ns.Write(last);
			ns.Seek(num2, SeekOrigin.Begin);
			ns.Write(num);
		}
	}
}
