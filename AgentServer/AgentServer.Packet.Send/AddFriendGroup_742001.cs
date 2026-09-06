using System;
using System.Data;
using System.IO;
using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using MySql.Data.MySqlClient;

namespace AgentServer.Packet.Send
{
	public sealed class AddFriendGroup_742001 : NetPacket
	{
		public AddFriendGroup_742001(Account User, string groupname, byte last)
		{
			ns.WriteOP(Opcodes.eServer_COMMUNITY_SERVER_PROTOCOL);
			ns.WriteOP(32);
			ns.Write((byte)1);
			int num = (int)ns.Position;
			int num2 = 0;
			ns.Write(num2);
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_cm_groupOperate";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("operationType", MySqlDbType.Int16).Value = 1;
				mySqlCommand.Parameters.Add("groupNum", MySqlDbType.Int16).Value = 0;
				mySqlCommand.Parameters.Add("isFolding", MySqlDbType.Byte).Value = 0;
				mySqlCommand.Parameters.Add("groupName", MySqlDbType.VarString).Value = groupname;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				if (mySqlDataReader.HasRows)
				{
					while (mySqlDataReader.Read())
					{
						ns.Write(Convert.ToInt16(mySqlDataReader["fdGroupNum"]));
						ns.Write(Convert.ToBoolean(mySqlDataReader["fdIsFolding"]));
						ns.WriteBIG5Fixed_intSize(mySqlDataReader["fdGroupName"].ToString());
						num2++;
					}
				}
			}
			ns.Write(last);
			ns.Seek(num, SeekOrigin.Begin);
			ns.Write(num2);
		}
	}
}
