using System.Data;
using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using MySql.Data.MySqlClient;

namespace AgentServer.Packet.Send
{
	public sealed class GetRequestedToMe_741E : NetPacket
	{
		public GetRequestedToMe_741E(Account User, byte last)
		{
			using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
			mySqlConnection.Open();
			using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
			mySqlCommand.Parameters.Clear();
			mySqlCommand.CommandType = CommandType.StoredProcedure;
			mySqlCommand.CommandText = "usp_cm_getRequestedToMe";
			mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
			using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
			if (mySqlDataReader.HasRows)
			{
				ns.WriteOP(Opcodes.eServer_COMMUNITY_SERVER_PROTOCOL);
				ns.WriteOP(30);
				mySqlDataReader.Read();
				ns.WriteBIG5Fixed_intSize(mySqlDataReader["nickname"].ToString());
				ns.WriteBIG5Fixed_intSize(mySqlDataReader["invitationmessage"].ToString());
				ns.Write(last);
			}
		}
	}
}
