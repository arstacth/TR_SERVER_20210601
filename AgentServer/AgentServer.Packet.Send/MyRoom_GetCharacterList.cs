using System;
using System.Collections.Generic;
using System.Data;
using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using LocalCommons.Network;
using MySql.Data.MySqlClient;

namespace AgentServer.Packet.Send
{
	public sealed class MyRoom_GetCharacterList : NetPacket
	{
		public MyRoom_GetCharacterList(Account User, byte last)
		{
			List<ushort> list = new List<ushort>();
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_myRoomGetCharacterList";
				mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
				while (mySqlDataReader.Read())
				{
					list.Add(Convert.ToUInt16(mySqlDataReader["character"]));
				}
			}
			ns.WriteOP(Opcodes.eServer_MYROOM_ACK);
			ns.WriteOP(eMyRoomProtocol.eServer_MYROOM_GET_MY_CHARACTER_LIST_ACK);
			ns.Write((byte)list.Count);
			foreach (ushort item in list)
			{
				ns.Write(item);
			}
			ns.Write(last);
		}
	}
}
