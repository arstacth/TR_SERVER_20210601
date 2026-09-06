using System;
using System.Data;
using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using LocalCommons.Utilities;
using MySql.Data.MySqlClient;

namespace AgentServer.Packet.Send
{
	public sealed class ShopBuyFreePassUpdate2 : NetPacket
	{
		public ShopBuyFreePassUpdate2(Account User, byte last)
		{
			ns.WriteOP(Opcodes.eServer_STORAGE_INSPIRE_NOTIFY);
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_storage_getUserDesc";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				mySqlDataReader.Read();
				ns.Write(Convert.ToInt32(mySqlDataReader["type"]));
				ns.Write(Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["expireTime"])));
				ns.Write(Convert.ToByte(mySqlDataReader["maxSavableCount"]));
				ns.Write(Convert.ToByte(mySqlDataReader["maxReceivableCount"]));
			}
			ns.Write(last);
		}
	}
}
