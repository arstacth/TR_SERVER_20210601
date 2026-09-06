using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AgentServer.Structuring;
using AgentServer.Structuring.Opcode;
using AgentServer.Structuring.User;
using LocalCommons.Network;
using LocalCommons.Utilities;
using MySql.Data.MySqlClient;

namespace AgentServer.Packet.Send
{
	public sealed class LoginUserInfo : NetPacket
	{
		public LoginUserInfo(Account User, UserLoginInfo logininfo, byte last)
		{
			ns.WriteOP(Opcodes.eServer_LOGIN_OK_ACK);
			long value = 0L;
			byte value2 = 20;
			byte value3 = 20;
			ns.Write(User.Session);
			ns.Write((short)(-1));
			ns.Write(25687);
			ns.Write(User.TR);
			ns.Write(User.Exp);
			ns.Write(User.Attribute);
			ns.Write(logininfo.playingTime);
			ns.Write(User.CoupleInfo.CoupleNum);
			ns.Write(User.CoupleInfo.CoupleType);
			ns.WriteBIG5Fixed_intSize(User.CoupleInfo.MateName);
			ns.Write(User.CoupleInfo.CreateTime);
			ns.Write(User.CoupleInfo.MarriedTime);
			ns.Write(User.CoupleInfo.RingChangedTime);
			ns.Write(User.CoupleInfo.CoupleRingNum);
			ns.Write(User.CoupleInfo.MaxRingDays);
			ns.Write(User.CoupleInfo.CoupleLevel);
			ns.Write((short)0);
			ns.Write(User.CoupleInfo.CondDays);
			ns.Write(User.CoupleInfo.AccumulateExp);
			ns.Write(0);
			ns.Write(User.CoupleInfo.CouplePoint);
			ns.Write(0);
			ns.Write(value: false);
			ns.Write((byte)0);
			ns.Write((byte)0);
			ns.Write(Utility.CurrentTimeMilliseconds());
			ns.Write((short)2);
			byte[] first = BitConverter.GetBytes((short)Conf.RelayPort).Reverse().ToArray();
			byte[] second = BitConverter.GetBytes(Utility.IPToInt(Conf.ServerIP)).Reverse().ToArray();
			IEnumerable<byte> source = first.Concat(second);
			ns.Write(source.ToArray(), 0, source.ToArray().Length);
			ns.Write(2018008620);
			ns.Write(0);
			ns.Write(value: true);
			ns.Write(0);
			ns.Write((byte)1);
			ns.Write(0);
			ns.Write((int)User.PartyType);
			ns.Write(0L);
			ns.Write(User.GameOption);
			ns.Write(601);
			ns.Write(0L);
			ns.Write(logininfo.shuMP);
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_freepass_getUserDesc";
				mySqlCommand.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader(CommandBehavior.SingleRow);
				mySqlDataReader.Read();
				User.FreePassType = Convert.ToInt32(mySqlDataReader["type"]);
				value = (Convert.IsDBNull(mySqlDataReader["expireTime"]) ? 0 : Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader["expireTime"])));
			}
			ns.Write(User.FreePassType);
			ns.Write(value);
			using (MySqlConnection mySqlConnection2 = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection2.Open();
				using MySqlCommand mySqlCommand2 = new MySqlCommand(string.Empty, mySqlConnection2);
				mySqlCommand2.Parameters.Clear();
				mySqlCommand2.CommandType = CommandType.StoredProcedure;
				mySqlCommand2.CommandText = "usp_storage_getUserDesc";
				mySqlCommand2.Parameters.Add("userNum", MySqlDbType.Int32).Value = User.UserNum;
				using MySqlDataReader mySqlDataReader2 = mySqlCommand2.ExecuteReader(CommandBehavior.SingleRow);
				mySqlDataReader2.Read();
				User.FreePassType = Convert.ToInt32(mySqlDataReader2["type"]);
				value = (Convert.IsDBNull(mySqlDataReader2["expireTime"]) ? 0 : Utility.ConvertToTimestamp(Convert.ToDateTime(mySqlDataReader2["expireTime"])));
				value2 = Convert.ToByte(mySqlDataReader2["maxSavableCount"]);
				value3 = Convert.ToByte(mySqlDataReader2["maxReceivableCount"]);
			}
			ns.Write(value2);
			ns.Write(value3);
			ns.Write(User.FreePassType);
			ns.Write(value);
			ns.Write(0);
			ns.Write(0);
			ns.Write(0);
			ns.Write(0);
			ns.Write((byte)0);
			ns.Write(User.TopRank);
			ns.Write(value: false);
			ns.Write(last);
		}
	}
}
