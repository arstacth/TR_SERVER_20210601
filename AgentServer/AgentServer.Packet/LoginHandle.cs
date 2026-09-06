using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AgentServer.Cryptography;
using AgentServer.Database;
using AgentServer.Holders;
using AgentServer.Network.Connections;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using Akka.Actor;
using LocalCommons.Network;
using LocalCommons.Utilities;
using NetMsg.LBS;
using Serilog;
using TRCommon;

namespace AgentServer.Packet
{
	public class LoginHandle
	{
		public static void Handle_LoginCheck(ClientConnection Client, PacketReader reader)
		{
			try
			{
				int fixedLength = reader.ReadLEInt16();
				string text = reader.ReadBig5StringSafe(fixedLength);
				int length = reader.ReadLEInt16();
				string password = pwdecode(reader.ReadByteArray(length));
				bool flag = false;
				if (!new Regex("='\"").IsMatch(text) && text.Length != 0)
				{
					flag = checkUserAccount(text, password);
				}
				if (flag)
				{
					byte[] array = new byte[16];
					new RNGCryptoServiceProvider().GetBytes(array);
					byte[] xorKey = Encrypt.EncryptKey(array);
					long num = Utility.CurrentTimeMilliseconds();
					Account account2 = (Client.CurrentAccount = new Account
					{
						UserID = text,
						Connection = Client,
						LastIp = Client.IP,
						Port = (short)((IPEndPoint)Client.EP).Port,
						EncryptKey = array,
						XorKey = xorKey,
						Session = Client.session,
						LoginDateTime = DateTime.Now,
						bLogin = false,
						LastCheckTime = num,
						LastPingTime = num
					});
					ClientConnection.CurrentAccounts.TryAdd(Client.session, Client.CurrentAccount);
				}
				else
				{
					Log.Warning("{0} Login Fail! ip:{1}", text, Client.IP);
				}
				Client.SendAsync(new LOGIN_AUTH_ACK_THAI(text, flag));
				ClientConnection.DDOS_IP.TryRemove(Client.IP, out var _);
			}
			catch (Exception ex)
			{
				Log.Error("LoginCheck Error:{0}", ex.Message);
			}
		}

		public static void Handle_GetClientKey(ClientConnection Client, PacketReader reader)
		{
			try
			{
				Client.CurrentAccount.GotClientKey = true;
				byte[] collection = reader.ReadByteArray(255);
				List<byte> list = new List<byte>();
				list.AddRange(collection);
				list.Add(0);
				byte[] key = Encrypt.TFUNC_1_W(list.ToArray(), Client.CurrentAccount.EncryptKey, Conf.ServerIP);
				Client.SendAsync(new LoginGenKey(key));
			}
			catch (Exception ex)
			{
				Log.Error("GetClientKey Error:{0}", ex.Message);
			}
		}

		public static void Handle_LoginSuccess(ClientConnection Client, PacketReader reader, byte last)
		{
			reader.ReadByte();
			short num = reader.ReadLEInt16();
			int fixedLength = reader.ReadLEInt16();
			reader.ReadBig5StringSafe(fixedLength);
			int fixedLength2 = reader.ReadLEInt16();
			string text = reader.ReadBig5StringSafe(fixedLength2);
			int fixedLength3 = reader.ReadLEInt16();
			reader.ReadBig5StringSafe(fixedLength3);
			Account currentAccount = Client.CurrentAccount;
			if (num != 1922)
			{
				currentAccount.isBlocked = true;
				Log.Error("Incorrect Protocol version userid : {0}, server(4,{1}max), client(4,{2}max)", currentAccount.UserID, (short)1922, num);
				Client.SendAsync(new LoginError(5, last, 0));
				return;
			}
			if (Conf.HashCheck && !ServerSettingHolder.HashList.Contains(text))
			{
				currentAccount.isBlocked = true;
				Log.Error("InCorrect hash. {0} : {1}", currentAccount.UserID, text);
				Client.SendAsync(new LoginError(8, last, 0));
				return;
			}
			ServerStatus.LBServerActor.Tell(new AccountCheckRequest
			{
				Session = currentAccount.Session,
				UID = currentAccount.UserID,
				ServerID = ServerStatus.MyAgentID
			});
			if (ClientConnection.CurrentAccounts.Count((KeyValuePair<int, Account> c) => c.Value.isLogin) > Conf.MaxUserCount)
			{
				currentAccount.isBlocked = true;
				Log.Error("User [{0}] can't login because server full!", currentAccount.UserID);
				Client.SendAsync(new LoginError(7, last, 0));
			}
			else
			{
				LoginTrafficManager.LoginProcess(Client, last);
			}
		}

		public static void Handle_NOTIFY_MY_UDP(ClientConnection Client, PacketReader reader, byte last)
		{
			Client.CurrentAccount.UDPInfo = reader.ReadByteArray(48);
			Client.SendAsync(new Login_NOTIFY_MY_UDP(last));
		}

		public static void Handle_GetNickName(ClientConnection Client, PacketReader reader, byte last)
		{
			Client.SendAsync(new LoginGetNickName_0X1C(Client.CurrentAccount, last));
		}

		public static void Handle_GetUserCash(ClientConnection Client, byte last)
		{
			Account currentAccount = Client.CurrentAccount;
			if (currentAccount.CashNeedUpdateFromDB)
			{
				getUserCash(currentAccount);
			}
			Client.SendAsync(new LoginGetUserCash(currentAccount, last));
		}

		public static void Handle_FF7F01(ClientConnection Client, byte last)
		{
			Client.SendAsync(new Login_FF7F01_0x180(last));
		}

		public static void Handle_82(ClientConnection Client, byte last)
		{
			Client.SendAsync(new Login_82_0x83(Client.CurrentAccount, last));
		}

		public static void Handle_GetCommunityAgentServer(ClientConnection Client, byte last)
		{
			Client.SendAsync(new GetCommunityAgentServer(last));
		}

		public static void Handle_GetExtraAbilities(ClientConnection Client, byte last)
		{
			getExtraAbilities(Client.CurrentAccount, last);
		}

		private static bool checkUserAccount(string userid, string password)
		{
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_checkUserAccount");
				mySqlCommandHelper.AddParamVarString("userid", userid);
				mySqlCommandHelper.AddParamVarString("password", password);
				mySqlCommandHelper.ExecuteSingle();
				if (mySqlCommandHelper.HasResult() && mySqlCommandHelper.GetString("result") == "1")
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_checkUserAccount Error: {0}", ex.Message);
			}
			return false;
		}

		private static void getUserCash(Account User)
		{
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_getUserCash");
				mySqlCommandHelper.AddParamInt("usernum", User.UserNum);
				mySqlCommandHelper.ExecuteSingle();
				if (mySqlCommandHelper.HasResult())
				{
					User.Cash = mySqlCommandHelper.GetInt("cash");
				}
			}
			catch (Exception ex)
			{
				Log.Error("usp_getUserCash Error: {0}", ex.Message);
			}
			User.CashNeedUpdateFromDB = false;
		}

		public static void getExtraAbilities(Account User, byte last)
		{
			Dictionary<int, ExtraAbilityInfo> dictionary = new Dictionary<int, ExtraAbilityInfo>();
			bool flag = false;
			try
			{
				using MySqlCommandHelper mySqlCommandHelper = new MySqlCommandHelper("usp_getExtraAbilities");
				mySqlCommandHelper.AddParamInt("pUserNum", User.UserNum);
				mySqlCommandHelper.Execute();
				while (mySqlCommandHelper.HasResult())
				{
					while (mySqlCommandHelper.HasResult())
					{
						short key = (short)mySqlCommandHelper.GetInt("attrType");
						float @float = mySqlCommandHelper.GetFloat("attrValue");
						int @int = mySqlCommandHelper.GetInt("itemnum");
						int iLimitTime = mySqlCommandHelper.GetInt("limit") * 1000;
						long dateTime = mySqlCommandHelper.GetDateTime("gottime", 0L);
						if (!dictionary.TryGetValue(@int, out var value))
						{
							value = new ExtraAbilityInfo();
						}
						value.iItemDescNum = @int;
						value.iLimitTime = iLimitTime;
						value.tGotTime = dateTime;
						value.mapAttributes[key] = @float;
						if (!dictionary.ContainsKey(@int))
						{
							dictionary.Add(@int, value);
						}
					}
				}
				flag = true;
			}
			catch (Exception ex)
			{
				flag = false;
				Log.Error("usp_getExtraAbilities Error: {0}", ex.Message);
			}
			if (flag)
			{
				User.Connection.SendAsync(new GetExtraAbilities_ACK(dictionary, last));
			}
		}

		private static string pwdecode(byte[] pw)
		{
			int num = pw.Length / 4;
			int num2 = 0;
			string text = "";
			for (int i = 0; i < num; i++)
			{
				int num3 = ((pw[i * 4] >= pw[1 + i * 4]) ? (256 + pw[1 + i * 4] - pw[i * 4]) : (pw[1 + i * 4] - pw[i * 4]));
				int num4;
				num2 = ((pw[2 + i * 4] != byte.MaxValue) ? (num4 = pw[3 + i * 4] - pw[2 + i * 4] << 8) : (num4 = 256 - (pw[2 + i * 4] - pw[3 + i * 4]) << 8));
				num4 = num4 >> 11 << 11;
				num2 -= num4;
				int value = num3 + num2 >> 4;
				text += (char)Convert.ToByte(value);
			}
			return text;
		}

		private static string CreateMD5(string input)
		{
			using MD5 mD = MD5.Create();
			byte[] bytes = Encoding.ASCII.GetBytes(input);
			byte[] array = mD.ComputeHash(bytes);
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < array.Length; i++)
			{
				stringBuilder.Append(array[i].ToString("x2"));
			}
			return stringBuilder.ToString();
		}
	}
}
