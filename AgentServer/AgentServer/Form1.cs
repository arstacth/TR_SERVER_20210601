using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AgentServer.Database;
using AgentServer.Dialog;
using AgentServer.EasyAntiCheat;
using AgentServer.Holders;
using AgentServer.Network.Connections;
using AgentServer.Packet.Send;
using AgentServer.Structuring;
using Akka.Actor;
using Akka.Configuration;
using Akka.Quartz.Actor;
using Akka.Quartz.Actor.Commands;
using IniParser;
using IniParser.Model;
using MySql.Data.MySqlClient;
using NetMsg.LBS;
using NetMsg.Room;
using Quartz;
using Serilog;
using Serilog.Events;
using TRCommon;

namespace AgentServer
{
	public class Form1 : Form
	{
		private delegate void EnableDelegate(int id);

		private CapsuleMachineManager cpm;

		private ServerSettingManager ssm;

		private GMTool gmtool;

		private System.Threading.Timer sockettimer;

		private System.Threading.Timer checktimer;

		private System.Threading.Timer checktimer2;

		private System.Threading.Timer eactimer;

		private static Form1 form;

		private IContainer components;

		private RichTextBox richTextBox1;

		private Button btnStopServer;

		private Label label1;

		private Label label2;

		private TextBox txtNotice;

		private Button btnNotice;

		private Button btnOpenHash;

		private Button btnOpenDir;

		private Button btnShowUserNum;

		private Button btnReloadtblServerSettingInfo;

		private Button btnCapsuleMachineManager;

		private Button btnReloadMap;

		private Button btnGMTool;

		private Button btnReloadGameReward;

		private Button btnReloadHash;

		private Button btnReloadHackingToolHash;

		private Button btnReloadRenewalShop;

		private Label label5;

		private Button btnReloadFishing;

		private Button btnReloadMonthlyGacha;

		private Button btnReloadHuMongPickBoard;

		private CheckBox chkUseLuckyBag;

		private Label label6;

		public CheckBox chkOpenShop;

		public CheckBox chkServerReady;
        private Button btnServerSettingManager;
        private Button btnUpdatCollectioneRanking;
        private Button btnUpdateRanking;
        private Button btnReloadSingleChallenge;

		public Form1()
		{
			InitializeComponent();
			form = this;
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			base.Activated += Init;
		}

		private void Init(object sender, EventArgs e)
		{
			base.Activated -= Init;
			string dates = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_Agent.log";
			InMemorySink logEventSink = new InMemorySink(richTextBox1);
			Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().MinimumLevel.Override("Quartz", LogEventLevel.Information).WriteTo.Sink(logEventSink).WriteTo.Logger(delegate(LoggerConfiguration l)
			{
				l.Filter.ByIncludingOnly((LogEvent e1) => e1.Level == LogEventLevel.Information).WriteTo.File(".\\Logs\\Agent\\Info\\" + dates, LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, 1073741824L, null, buffered: false, shared: false, null, RollingInterval.Infinite, rollOnFileSizeLimit: false, 31);
			}).WriteTo.Logger(delegate(LoggerConfiguration l)
			{
				l.Filter.ByIncludingOnly((LogEvent e1) => e1.Level == LogEventLevel.Warning).WriteTo.File(".\\Logs\\Agent\\Warning\\" + dates, LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, 1073741824L, null, buffered: false, shared: false, null, RollingInterval.Infinite, rollOnFileSizeLimit: false, 31);
			}).WriteTo.Logger(delegate(LoggerConfiguration l)
			{
				l.Filter.ByIncludingOnly((LogEvent e1) => e1.Level == LogEventLevel.Error).WriteTo.File(".\\Logs\\Agent\\Error\\" + dates, LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, 1073741824L, null, buffered: false, shared: false, null, RollingInterval.Infinite, rollOnFileSizeLimit: false, 31);
			}).CreateLogger();
			if (!Boot())
			{
				return;
			}
			Config config = ConfigurationFactory.ParseString("\r\n                                akka { \r\n                                    loglevel = INFO,\r\n                                     actor {\r\n                                        provider = remote\r\n                                    }\r\n                                    remote {\r\n                                        dot-netty.tcp {\r\n                                            port = 0 \r\n                                            hostname = localhost\r\n                                        }\r\n                                    }\r\n                                    log-dead-letters-during-shutdown = off,\r\n                                    log-dead-letters = 0,\r\n                                    loggers = [\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]\r\n                                }");
			ServerStatus.MainActorSystem = ActorSystem.Create("Agent", config);
			ServerStatus.QuartzActor = ServerStatus.MainActorSystem.ActorOf(Props.Create(() => new QuartzActor()), "Quartz");
			ServerSettingHolder.LoadHashList();
			Task.Run(delegate
			{
				DBInit.startServerInitDB();
				LoginTrafficManager.Init();
				RoomHolder.LoadRoomKindInfo();
				MapHolder.LoadMapInfo();
				MapHolder.LoadAssaultModeLimitInfo();
				SingleChallengeHolder.LoadChallengeMapInfo();
				ShopHolder.LoadShopInfo();
				ItemHolder.LoadItemInfo();
				ItemHolder.LoadItemSetInfo();
				ItemHolder.LoadPetExpInfo();
				ItemHolder.LoadExchangeSystemInfo();
				ItemHolder.LoadItemTransformInfo();
				CapsuleMachineHolder.LoadCapsuleMachineInfo();
				AccountHolder.LoadLevelInfo();
				HotTimeHolder.LoadHotTimeInfo();
				FishingHolder.LoadFishingInfo();
				EventPickBoardHolder.LoadEventPickBoardInfo();
				EventPickBoardHolder.LoadHuMongPickBoardInfo();
				EventPickBoardHolder.LoadDiceBoardOpenList();
				TalesKnightHolder.LoadTalesKnightStageReward();
				MissionHolder.LoadMissionDataInfo();
				AnniversaryHolder.LoadAnniversaryInfo();
				ItemCubeHolder.LoadItemCubeInfo();
				ItemTradingHolder.LoadItemTradeInfo();
				CombinationShopHolder.LoadCombinationShopInfo();
			});
			ServerStatus.ServerActor = ServerStatus.MainActorSystem.ActorOf(Props.Create(() => new AgentServer(new IPEndPoint(IPAddress.Parse(Conf.ServerIP), Conf.AgentPort))), "Client");
			Log.Information("AgentServer is listening on {0}:{1}", Conf.ServerIP, Conf.AgentPort);
			ServerStatus.RoomServerActor = ServerStatus.MainActorSystem.ActorOf(Props.Create<RoomServer>(Array.Empty<object>()));
			ServerStatus.LBServerActor = ServerStatus.MainActorSystem.ActorOf(Props.Create<LBServer>(Array.Empty<object>()), "LBServerActor");
			Task.Run(async delegate
			{
				while (!ServerStatus.RoomServerConnected)
				{
					try
					{
						ServerStatus.RoomServerActor.Tell(new AgentConnectRequest
						{
							Username = "Agent"
						});
					}
					catch (Exception)
					{
						Log.Information("Unable to Connect RoomServer, Retry After 5 Second");
					}
					await Task.Delay(5000);
				}
			});
			Task.Run(async delegate
			{
				while (!ServerStatus.LBServerConnected)
				{
					try
					{
						ServerStatus.LBServerActor.Tell(new AgentToLBSRequest
						{
							Port = Conf.AgentPort
						});
					}
					catch (Exception)
					{
						Log.Information("Unable to Connect LBServer, Retry After 5 Second");
					}
					await Task.Delay(5000);
				}
			});
			if (EACServer.DoStartup())
			{
				EACTimer();
			}
			if (Conf.EnableTimedOutCheck)
			{
				IActorRef to = ServerStatus.MainActorSystem.ActorOf(Props.Create(() => new SocketCheck()), "TimeOutCheck");
				ServerStatus.QuartzActor.Tell(new CreateJob(to, "", TriggerBuilder.Create().WithSimpleSchedule(delegate(SimpleScheduleBuilder x)
				{
					x.WithIntervalInSeconds(60).RepeatForever();
				}).Build()));
			}
			SocketTimeoutCheck();
		}

		private bool Boot()
		{
			try
			{
				Version version = Assembly.GetEntryAssembly().GetName().Version;
				string text = " (RELEASE)";
				Text = "TR SERVER : AgentServer v" + version.ToString() + " [THTR Client R144739]" + text;
				IniData iniData = new FileIniDataParser().ReadFile("settings.ini");
				Conf.ServerIP = iniData["Server"]["AgentServerIP"].Replace(" ", "");
				Conf.AgentPort = Convert.ToUInt16(iniData["Server"]["AgentServerTCPPort"].Replace(" ", ""));
				Conf.AgentPort2 = Convert.ToUInt16(iniData["Server"]["AgentServerTCPPort2"].Replace(" ", ""));
				Conf.RelayPort = Convert.ToUInt16(iniData["Server"]["RelayServerPort"].Replace(" ", ""));
				Conf.CommunityAgentServerPort = Convert.ToUInt16(iniData["Server"]["CommunityServerPort"].Replace(" ", ""));
				Conf.LBSLocalPort = Convert.ToUInt16(iniData["Server"]["LoadBalanceServerLocalPort"].Replace(" ", ""));
				Conf.RMServerIP = iniData["Server"]["RoomServerIP"].Replace(" ", "");
				Conf.RMLocalPort = Convert.ToUInt16(iniData["Server"]["RoomServerLocalPort"].Replace(" ", ""));
				Conf.Connstr = iniData["Server"]["MySQLConnection"];
				Conf.HashCheck = Convert.ToBoolean(iniData["Server"]["HashCheck"].Replace(" ", ""));
				Conf.MaxUserCount = Convert.ToInt32(iniData["Server"]["MaxUserCount"].Replace(" ", ""));
				Conf.MaxTotalAgentUserCount = Convert.ToInt32(iniData["Server"]["MaxTotalAgentUserCount"].Replace(" ", ""));
				Conf.BlockDDOS = Convert.ToBoolean(iniData["Server"]["BlockDDOS"].Replace(" ", ""));
				Conf.JudgeTime = Convert.ToInt32(iniData["Server"]["JudgeTime"].Replace(" ", ""));
				Conf.MaxConnectTime = Convert.ToInt32(iniData["Server"]["MaxConnectTime"].Replace(" ", ""));
				Conf.TimedOutCheckTime = Convert.ToInt32(iniData["Server"]["TimedOutCheckTime"].Replace(" ", ""));
				Conf.EnableTimedOutCheck = Convert.ToBoolean(iniData["Server"]["EnableTimedOutCheck"].Replace(" ", ""));
				Log.Information("Loading Settings.ini........Done");
				label2.Text = Conf.ServerIP + ":" + Conf.AgentPort;
				MySqlConnection mySqlConnection = CreateConnection();
				if (mySqlConnection == null)
				{
					return false;
				}
				mySqlConnection.Close();
				DBInit.initlevel_run();
				DBInit.inithacktool_run();
				ServerSettingHolder.LoadServerSettingInfo();
				DBInit.initSmartChannelModeInfo_run();
				DBInit.initSmartChannelScheduleInfo_run();
				DBInit.initRoomKindPenaltyInfo_run();
				Log.Information("Initializing DB........Done");
				return true;
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
			}
			return false;
		}

		private void SocketTimeoutCheck()
		{
		}

		private void EACTimer()
		{
			eactimer = new System.Threading.Timer(delegate
			{
				EACServer.DoUpdate();
			}, null, 1000, 1000);
		}

		private void btnStopServer_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Do you want to shut down the server?", "TRServer", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
			{
				return;
			}
			foreach (Account item in ClientConnection.CurrentAccounts.Values.Where((Account w) => w.isLogin))
			{
				HandleLogout(item);
			}
			EACServer.DoShutdown();
			Close();
		}

		private void chkServerReady_CheckedChanged(object sender, EventArgs e)
		{
			if (!ShopItemTable.isRecvItemListFromDB)
			{
				MessageBox.Show("Loading ItemInfo From DB!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				bool isReady = (chkServerReady.Checked = false);
				ServerStatus.isReady = isReady;
			}
			else
			{
				ServerStatus.isReady = chkServerReady.Checked;
				ServerStatus.LBServerActor.Tell(new SetServerReady
				{
					isSet = chkServerReady.Checked
				});
				Log.Information("Set Server Ready : {0}", ServerStatus.isReady);
			}
		}

		private void btnNotice_Click(object sender, EventArgs e)
		{
			string text = txtNotice.Text;
			if (text.Length > 0)
			{
				ServerStatus.LBServerActor.Tell(new NoticePacket(text, 16));
				Log.Information("Send notice NoticeType : 0, noticeKind : 1,  {0}", text);
			}
		}

		private void btnOpenHash_Click(object sender, EventArgs e)
		{
			Process.Start("hash.ini");
		}

		private void btnOpenDir_Click(object sender, EventArgs e)
		{
			string startupPath = Application.StartupPath;
			Process.Start("explorer.exe", startupPath);
		}

		private async void btnShowUserNum_Click(object sender, EventArgs e)
		{
			await Task.Run(delegate
			{
				int propertyValue = ClientConnection.CurrentAccounts.Count((KeyValuePair<int, Account> c) => c.Value.isLogin);
				int propertyValue2 = Rooms.RoomList.Values.Count((NormalRoom rm) => rm.RoomKindID != 74 && rm.PlayerCount > 0);
				int propertyValue3 = Rooms.RoomList.Values.Count((NormalRoom rm) => rm.RoomKindID == 74);
				Log.Information("ShowInfo - user({0}), room({1}), parkroom({2})", propertyValue, propertyValue2, propertyValue3);
			});
		}

		private void btnReloadtblServerSettingInfo_Click(object sender, EventArgs e)
		{
			ServerStatus.LBServerActor.Tell(new ReloadSetting
			{
				Code = 8
			});
			ServerStatus.ToAllRoomServer(new ReloadSetting
			{
				Code = 8
			});
			Log.Information("Reloaded tblServerSettingInfo!");
		}

		private void richTextBox1_TextChanged(object sender, EventArgs e)
		{
			int num = 200;
			if (richTextBox1.Lines.Length > num)
			{
				int sourceIndex = richTextBox1.Lines.Length - num;
				string[] array = new string[num];
				Array.Copy(richTextBox1.Lines, sourceIndex, array, 0, num);
				richTextBox1.Lines = array;
			}
			richTextBox1.SelectionStart = richTextBox1.Text.Length;
			richTextBox1.ScrollToCaret();
		}

		private void btnCapsuleMachineManager_Click(object sender, EventArgs e)
		{
			if (cpm == null || cpm.IsDisposed)
			{
				cpm = new CapsuleMachineManager();
			}
			cpm.Show();
			cpm.Focus();
		}

		private void btnReloadMap_Click(object sender, EventArgs e)
		{
			ServerStatus.ToAllRoomServer(new ReloadSetting
			{
				Code = 5
			});
		}

		private void btnGMTool_Click(object sender, EventArgs e)
		{
			if (gmtool == null || gmtool.IsDisposed)
			{
				gmtool = new GMTool();
			}
			gmtool.Show();
			gmtool.Focus();
		}

		private void btnReloadGameReward_Click(object sender, EventArgs e)
		{
			ServerStatus.ToAllRoomServer(new ReloadSetting
			{
				Code = 6
			});
		}

		private void btnReloadHash_Click(object sender, EventArgs e)
		{
			ServerStatus.LBServerActor.Tell(new ReloadHash
			{
				Hash = File.ReadAllLines("hash.ini")
			});
		}

		private void btnReloadHackingToolHash_Click(object sender, EventArgs e)
		{
			DBInit.inithacktool_run();
			ServerStatus.MainActorSystem.ActorSelection("/user/Client/*").Tell(new NP_Byte(DBInit.HackTools));
			Log.Information("Reloaded tblHackingToolHash!");
		}

		private void HandleLogout(Account User)
		{
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				using MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_logout";
				mySqlCommand.Parameters.Add("usernum", MySqlDbType.Int32).Value = User.UserNum;
				mySqlCommand.Parameters.Add("nickName", MySqlDbType.VarString).Value = User.NickName;
				mySqlCommand.Parameters.Add("puid", MySqlDbType.VarString).Value = User.UserID;
				mySqlCommand.Parameters.Add("pexp", MySqlDbType.Int64).Value = User.Exp;
				mySqlCommand.Parameters.Add("ip", MySqlDbType.VarString).Value = User.LastIp;
				mySqlCommand.Parameters.Add("logintime", MySqlDbType.DateTime).Value = User.LoginDateTime;
				mySqlCommand.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Log.Error("Logout sql error: {0}", ex.Message);
			}
		}

		private void btnReloadRenewalShop_Click(object sender, EventArgs e)
		{
			ServerStatus.LBServerActor.Tell(new ReloadSetting
			{
				Code = 2
			});
		}

		private void btnReloadFishing_Click(object sender, EventArgs e)
		{
			ServerStatus.LBServerActor.Tell(new ReloadSetting
			{
				Code = 7
			});
		}

		private void btnReloadMonthlyGacha_Click(object sender, EventArgs e)
		{
		}

		private void btnReloadHuMongPickBoard_Click(object sender, EventArgs e)
		{
			ServerStatus.LBServerActor.Tell(new ReloadSetting
			{
				Code = 3
			});
		}

		private MySqlConnection CreateConnection()
		{
			try
			{
				using MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr);
				mySqlConnection.Open();
				return mySqlConnection;
			}
			catch (Exception ex)
			{
				Log.Error("Error on DB connect: {0}", ex.Message);
				return null;
			}
		}

		private void chkUseLuckyBag_CheckedChanged(object sender, EventArgs e)
		{
			ServerStatus.enableUseLuckyBag = chkUseLuckyBag.Checked;
			Log.Information("Set EnableUseLuckyBag : {0}", ServerStatus.enableUseLuckyBag);
		}

		private void chkOpenShop_CheckedChanged(object sender, EventArgs e)
		{
			ServerStatus.CanShopOperation = chkOpenShop.Checked;
			ServerStatus.LBServerActor.Tell(new CanShopOperation
			{
				isSet = chkOpenShop.Checked
			});
			Log.Information("Set CanShopOperation : {0}", ServerStatus.CanShopOperation);
		}

		public static void UpdateLableStatic(int ID)
		{
			if (form != null)
			{
				form.UpdateLable(ID);
			}
		}

		private void UpdateLable(int ID)
		{
			if (base.InvokeRequired)
			{
				Invoke(new EnableDelegate(UpdateLable), ID);
			}
			else
			{
				label6.Text = $"ServerID: {ID}";
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.btnStopServer = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.chkServerReady = new System.Windows.Forms.CheckBox();
            this.txtNotice = new System.Windows.Forms.TextBox();
            this.btnNotice = new System.Windows.Forms.Button();
            this.btnOpenHash = new System.Windows.Forms.Button();
            this.btnOpenDir = new System.Windows.Forms.Button();
            this.btnShowUserNum = new System.Windows.Forms.Button();
            this.btnReloadtblServerSettingInfo = new System.Windows.Forms.Button();
            this.btnCapsuleMachineManager = new System.Windows.Forms.Button();
            this.btnReloadMap = new System.Windows.Forms.Button();
            this.btnGMTool = new System.Windows.Forms.Button();
            this.btnReloadGameReward = new System.Windows.Forms.Button();
            this.btnReloadHash = new System.Windows.Forms.Button();
            this.btnReloadHackingToolHash = new System.Windows.Forms.Button();
            this.btnReloadRenewalShop = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.btnReloadFishing = new System.Windows.Forms.Button();
            this.btnReloadMonthlyGacha = new System.Windows.Forms.Button();
            this.btnReloadHuMongPickBoard = new System.Windows.Forms.Button();
            this.chkUseLuckyBag = new System.Windows.Forms.CheckBox();
            this.chkOpenShop = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.btnServerSettingManager = new System.Windows.Forms.Button();
            this.btnUpdatCollectioneRanking = new System.Windows.Forms.Button();
            this.btnUpdateRanking = new System.Windows.Forms.Button();
            this.btnReloadSingleChallenge = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.BackColor = System.Drawing.Color.White;
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBox1.Location = new System.Drawing.Point(10, 48);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(731, 487);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            this.richTextBox1.TextChanged += new System.EventHandler(this.richTextBox1_TextChanged);
            // 
            // btnStopServer
            // 
            this.btnStopServer.Location = new System.Drawing.Point(604, 630);
            this.btnStopServer.Name = "btnStopServer";
            this.btnStopServer.Size = new System.Drawing.Size(137, 31);
            this.btnStopServer.TabIndex = 1;
            this.btnStopServer.Text = "Shut Down";
            this.btnStopServer.UseVisualStyleBackColor = true;
            this.btnStopServer.Click += new System.EventHandler(this.btnStopServer_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(102, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Server IP:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(167, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 16);
            this.label2.TabIndex = 3;
            this.label2.Text = "127.0.0.1:9153";
            // 
            // chkServerReady
            // 
            this.chkServerReady.AutoSize = true;
            this.chkServerReady.Location = new System.Drawing.Point(601, 579);
            this.chkServerReady.Name = "chkServerReady";
            this.chkServerReady.Size = new System.Drawing.Size(118, 20);
            this.chkServerReady.TabIndex = 4;
            this.chkServerReady.Text = "SetServerREADY";
            this.chkServerReady.UseVisualStyleBackColor = true;
            this.chkServerReady.CheckedChanged += new System.EventHandler(this.chkServerReady_CheckedChanged);
            // 
            // txtNotice
            // 
            this.txtNotice.Location = new System.Drawing.Point(10, 726);
            this.txtNotice.Name = "txtNotice";
            this.txtNotice.Size = new System.Drawing.Size(615, 23);
            this.txtNotice.TabIndex = 5;
            // 
            // btnNotice
            // 
            this.btnNotice.Location = new System.Drawing.Point(633, 722);
            this.btnNotice.Name = "btnNotice";
            this.btnNotice.Size = new System.Drawing.Size(108, 31);
            this.btnNotice.TabIndex = 6;
            this.btnNotice.Text = "Send Notice";
            this.btnNotice.UseVisualStyleBackColor = true;
            this.btnNotice.Click += new System.EventHandler(this.btnNotice_Click);
            // 
            // btnOpenHash
            // 
            this.btnOpenHash.Location = new System.Drawing.Point(221, 578);
            this.btnOpenHash.Name = "btnOpenHash";
            this.btnOpenHash.Size = new System.Drawing.Size(101, 31);
            this.btnOpenHash.TabIndex = 7;
            this.btnOpenHash.Text = "Open Hash";
            this.btnOpenHash.UseVisualStyleBackColor = true;
            this.btnOpenHash.Click += new System.EventHandler(this.btnOpenHash_Click);
            // 
            // btnOpenDir
            // 
            this.btnOpenDir.Location = new System.Drawing.Point(129, 578);
            this.btnOpenDir.Name = "btnOpenDir";
            this.btnOpenDir.Size = new System.Drawing.Size(86, 31);
            this.btnOpenDir.TabIndex = 8;
            this.btnOpenDir.Text = "Open Dir";
            this.btnOpenDir.UseVisualStyleBackColor = true;
            this.btnOpenDir.Click += new System.EventHandler(this.btnOpenDir_Click);
            // 
            // btnShowUserNum
            // 
            this.btnShowUserNum.Location = new System.Drawing.Point(10, 578);
            this.btnShowUserNum.Name = "btnShowUserNum";
            this.btnShowUserNum.Size = new System.Drawing.Size(113, 31);
            this.btnShowUserNum.TabIndex = 9;
            this.btnShowUserNum.Text = "Show User Num";
            this.btnShowUserNum.UseVisualStyleBackColor = true;
            this.btnShowUserNum.Click += new System.EventHandler(this.btnShowUserNum_Click);
            // 
            // btnReloadtblServerSettingInfo
            // 
            this.btnReloadtblServerSettingInfo.Location = new System.Drawing.Point(10, 542);
            this.btnReloadtblServerSettingInfo.Name = "btnReloadtblServerSettingInfo";
            this.btnReloadtblServerSettingInfo.Size = new System.Drawing.Size(205, 30);
            this.btnReloadtblServerSettingInfo.TabIndex = 11;
            this.btnReloadtblServerSettingInfo.Text = "Reload tblServerSettingInfo";
            this.btnReloadtblServerSettingInfo.UseVisualStyleBackColor = true;
            this.btnReloadtblServerSettingInfo.Click += new System.EventHandler(this.btnReloadtblServerSettingInfo_Click);
            // 
            // btnCapsuleMachineManager
            // 
            this.btnCapsuleMachineManager.Location = new System.Drawing.Point(221, 542);
            this.btnCapsuleMachineManager.Name = "btnCapsuleMachineManager";
            this.btnCapsuleMachineManager.Size = new System.Drawing.Size(187, 30);
            this.btnCapsuleMachineManager.TabIndex = 12;
            this.btnCapsuleMachineManager.Text = "CapsuleMachine Manager";
            this.btnCapsuleMachineManager.UseVisualStyleBackColor = true;
            this.btnCapsuleMachineManager.Click += new System.EventHandler(this.btnCapsuleMachineManager_Click);
            // 
            // btnReloadMap
            // 
            this.btnReloadMap.Location = new System.Drawing.Point(328, 578);
            this.btnReloadMap.Name = "btnReloadMap";
            this.btnReloadMap.Size = new System.Drawing.Size(119, 31);
            this.btnReloadMap.TabIndex = 14;
            this.btnReloadMap.Text = "Reload MapInfo";
            this.btnReloadMap.UseVisualStyleBackColor = true;
            this.btnReloadMap.Click += new System.EventHandler(this.btnReloadMap_Click);
            // 
            // btnGMTool
            // 
            this.btnGMTool.Location = new System.Drawing.Point(414, 542);
            this.btnGMTool.Name = "btnGMTool";
            this.btnGMTool.Size = new System.Drawing.Size(96, 30);
            this.btnGMTool.TabIndex = 15;
            this.btnGMTool.Text = "GM Tool";
            this.btnGMTool.UseVisualStyleBackColor = true;
            this.btnGMTool.Click += new System.EventHandler(this.btnGMTool_Click);
            // 
            // btnReloadGameReward
            // 
            this.btnReloadGameReward.Location = new System.Drawing.Point(10, 615);
            this.btnReloadGameReward.Name = "btnReloadGameReward";
            this.btnReloadGameReward.Size = new System.Drawing.Size(205, 31);
            this.btnReloadGameReward.TabIndex = 16;
            this.btnReloadGameReward.Text = "Reload GameRewardGroupInfo";
            this.btnReloadGameReward.UseVisualStyleBackColor = true;
            this.btnReloadGameReward.Click += new System.EventHandler(this.btnReloadGameReward_Click);
            // 
            // btnReloadHash
            // 
            this.btnReloadHash.Location = new System.Drawing.Point(453, 578);
            this.btnReloadHash.Name = "btnReloadHash";
            this.btnReloadHash.Size = new System.Drawing.Size(110, 31);
            this.btnReloadHash.TabIndex = 17;
            this.btnReloadHash.Text = "Reload Hash";
            this.btnReloadHash.UseVisualStyleBackColor = true;
            this.btnReloadHash.Click += new System.EventHandler(this.btnReloadHash_Click);
            // 
            // btnReloadHackingToolHash
            // 
            this.btnReloadHackingToolHash.Location = new System.Drawing.Point(221, 615);
            this.btnReloadHackingToolHash.Name = "btnReloadHackingToolHash";
            this.btnReloadHackingToolHash.Size = new System.Drawing.Size(198, 31);
            this.btnReloadHackingToolHash.TabIndex = 18;
            this.btnReloadHackingToolHash.Text = "Reload tblHackingToolHash";
            this.btnReloadHackingToolHash.UseVisualStyleBackColor = true;
            this.btnReloadHackingToolHash.Click += new System.EventHandler(this.btnReloadHackingToolHash_Click);
            // 
            // btnReloadRenewalShop
            // 
            this.btnReloadRenewalShop.Location = new System.Drawing.Point(425, 615);
            this.btnReloadRenewalShop.Name = "btnReloadRenewalShop";
            this.btnReloadRenewalShop.Size = new System.Drawing.Size(138, 31);
            this.btnReloadRenewalShop.TabIndex = 20;
            this.btnReloadRenewalShop.Text = "Reload RenewalShop";
            this.btnReloadRenewalShop.UseVisualStyleBackColor = true;
            this.btnReloadRenewalShop.Click += new System.EventHandler(this.btnReloadRenewalShop_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft JhengHei", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(85, 559);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(0, 21);
            this.label5.TabIndex = 24;
            // 
            // btnReloadFishing
            // 
            this.btnReloadFishing.Location = new System.Drawing.Point(10, 652);
            this.btnReloadFishing.Name = "btnReloadFishing";
            this.btnReloadFishing.Size = new System.Drawing.Size(124, 31);
            this.btnReloadFishing.TabIndex = 25;
            this.btnReloadFishing.Text = "Reload FishingInfo";
            this.btnReloadFishing.UseVisualStyleBackColor = true;
            this.btnReloadFishing.Click += new System.EventHandler(this.btnReloadFishing_Click);
            // 
            // btnReloadMonthlyGacha
            // 
            this.btnReloadMonthlyGacha.Enabled = false;
            this.btnReloadMonthlyGacha.Location = new System.Drawing.Point(140, 652);
            this.btnReloadMonthlyGacha.Name = "btnReloadMonthlyGacha";
            this.btnReloadMonthlyGacha.Size = new System.Drawing.Size(182, 31);
            this.btnReloadMonthlyGacha.TabIndex = 26;
            this.btnReloadMonthlyGacha.Text = "Reload Monthly Gacha";
            this.btnReloadMonthlyGacha.UseVisualStyleBackColor = true;
            this.btnReloadMonthlyGacha.Click += new System.EventHandler(this.btnReloadMonthlyGacha_Click);
            // 
            // btnReloadHuMongPickBoard
            // 
            this.btnReloadHuMongPickBoard.Location = new System.Drawing.Point(328, 652);
            this.btnReloadHuMongPickBoard.Name = "btnReloadHuMongPickBoard";
            this.btnReloadHuMongPickBoard.Size = new System.Drawing.Size(169, 31);
            this.btnReloadHuMongPickBoard.TabIndex = 27;
            this.btnReloadHuMongPickBoard.Text = "Reload HuMongPickBoard";
            this.btnReloadHuMongPickBoard.UseVisualStyleBackColor = true;
            this.btnReloadHuMongPickBoard.Click += new System.EventHandler(this.btnReloadHuMongPickBoard_Click);
            // 
            // chkUseLuckyBag
            // 
            this.chkUseLuckyBag.AutoSize = true;
            this.chkUseLuckyBag.Checked = true;
            this.chkUseLuckyBag.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUseLuckyBag.Location = new System.Drawing.Point(601, 604);
            this.chkUseLuckyBag.Name = "chkUseLuckyBag";
            this.chkUseLuckyBag.Size = new System.Drawing.Size(139, 20);
            this.chkUseLuckyBag.TabIndex = 28;
            this.chkUseLuckyBag.Text = "EnableUseLuckyBag";
            this.chkUseLuckyBag.UseVisualStyleBackColor = true;
            this.chkUseLuckyBag.CheckedChanged += new System.EventHandler(this.chkUseLuckyBag_CheckedChanged);
            // 
            // chkOpenShop
            // 
            this.chkOpenShop.AutoSize = true;
            this.chkOpenShop.Location = new System.Drawing.Point(601, 554);
            this.chkOpenShop.Name = "chkOpenShop";
            this.chkOpenShop.Size = new System.Drawing.Size(136, 20);
            this.chkOpenShop.TabIndex = 29;
            this.chkOpenShop.Text = "CanShopOperation";
            this.chkOpenShop.UseVisualStyleBackColor = true;
            this.chkOpenShop.CheckedChanged += new System.EventHandler(this.chkOpenShop_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 17);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(67, 16);
            this.label6.TabIndex = 30;
            this.label6.Text = "ServerID: 0";
            // 
            // btnServerSettingManager
            // 
            this.btnServerSettingManager.Location = new System.Drawing.Point(10, 689);
            this.btnServerSettingManager.Name = "btnServerSettingManager";
            this.btnServerSettingManager.Size = new System.Drawing.Size(163, 31);
            this.btnServerSettingManager.TabIndex = 38;
            this.btnServerSettingManager.Text = "ServerSetting Manager";
            this.btnServerSettingManager.UseVisualStyleBackColor = true;
            this.btnServerSettingManager.Click += new System.EventHandler(this.btnServerSettingManager_Click);
            // 
            // btnUpdatCollectioneRanking
            // 
            this.btnUpdatCollectioneRanking.Enabled = false;
            this.btnUpdatCollectioneRanking.Location = new System.Drawing.Point(316, 689);
            this.btnUpdatCollectioneRanking.Name = "btnUpdatCollectioneRanking";
            this.btnUpdatCollectioneRanking.Size = new System.Drawing.Size(180, 31);
            this.btnUpdatCollectioneRanking.TabIndex = 41;
            this.btnUpdatCollectioneRanking.Text = "Updat CollectioneRanking";
            this.btnUpdatCollectioneRanking.UseVisualStyleBackColor = true;
            this.btnUpdatCollectioneRanking.Click += new System.EventHandler(this.btnUpdatCollectioneRanking_Click);
            // 
            // btnUpdateRanking
            // 
            this.btnUpdateRanking.Location = new System.Drawing.Point(179, 689);
            this.btnUpdateRanking.Name = "btnUpdateRanking";
            this.btnUpdateRanking.Size = new System.Drawing.Size(131, 31);
            this.btnUpdateRanking.TabIndex = 40;
            this.btnUpdateRanking.Text = "Update Ranking";
            this.btnUpdateRanking.UseVisualStyleBackColor = true;
            this.btnUpdateRanking.Click += new System.EventHandler(this.btnUpdateRanking_Click);
            // 
            // btnReloadSingleChallenge
            // 
            this.btnReloadSingleChallenge.Location = new System.Drawing.Point(502, 689);
            this.btnReloadSingleChallenge.Name = "btnReloadSingleChallenge";
            this.btnReloadSingleChallenge.Size = new System.Drawing.Size(158, 31);
            this.btnReloadSingleChallenge.TabIndex = 39;
            this.btnReloadSingleChallenge.Text = "Reload SingleChallenge";
            this.btnReloadSingleChallenge.UseVisualStyleBackColor = true;
            this.btnReloadSingleChallenge.Click += new System.EventHandler(this.btnReloadSingleChallenge_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(755, 765);
            this.Controls.Add(this.btnUpdatCollectioneRanking);
            this.Controls.Add(this.btnUpdateRanking);
            this.Controls.Add(this.btnReloadSingleChallenge);
            this.Controls.Add(this.btnServerSettingManager);
            this.Controls.Add(this.chkOpenShop);
            this.Controls.Add(this.btnReloadHuMongPickBoard);
            this.Controls.Add(this.chkUseLuckyBag);
            this.Controls.Add(this.chkServerReady);
            this.Controls.Add(this.btnReloadGameReward);
            this.Controls.Add(this.btnReloadMonthlyGacha);
            this.Controls.Add(this.btnReloadHackingToolHash);
            this.Controls.Add(this.btnReloadFishing);
            this.Controls.Add(this.btnReloadRenewalShop);
            this.Controls.Add(this.btnReloadHash);
            this.Controls.Add(this.btnShowUserNum);
            this.Controls.Add(this.btnReloadMap);
            this.Controls.Add(this.btnGMTool);
            this.Controls.Add(this.btnOpenHash);
            this.Controls.Add(this.btnOpenDir);
            this.Controls.Add(this.btnCapsuleMachineManager);
            this.Controls.Add(this.btnReloadtblServerSettingInfo);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btnNotice);
            this.Controls.Add(this.txtNotice);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnStopServer);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.richTextBox1);
            this.Font = new System.Drawing.Font("Microsoft JhengHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "AgentServer";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

        private void btnServerSettingManager_Click(object sender, EventArgs e)
        {
			if (ssm == null || ssm.IsDisposed)
			{
				ssm = new ServerSettingManager();
			}
			ssm.Show();
			ssm.Focus();
		}

        private void btnUpdateRanking_Click(object sender, EventArgs e)
        {
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_makerank";
				mySqlCommand.Parameters.Add("detailRank", MySqlDbType.Int32).Value = 0;
				MySqlDataAdapter mySqlDataAdapter = new MySqlDataAdapter(mySqlCommand);
				mySqlCommand.ExecuteNonQuery();
				mySqlCommand.Dispose();
				mySqlConnection.Close();
			}
			Log.Information("Update Ranking !");
		}

        private void btnReloadSingleChallenge_Click(object sender, EventArgs e)
        {
			SingleChallengeHolder.LoadChallengeMapInfo();
			Log.Information("Reloaded ChallengeMapInfo !!");
		}

        private void btnUpdatCollectioneRanking_Click(object sender, EventArgs e)
        {
			using (MySqlConnection mySqlConnection = new MySqlConnection(Conf.Connstr))
			{
				mySqlConnection.Open();
				MySqlCommand mySqlCommand = new MySqlCommand(string.Empty, mySqlConnection);
				mySqlCommand.Parameters.Clear();
				mySqlCommand.CommandType = CommandType.StoredProcedure;
				mySqlCommand.CommandText = "usp_itemCollection_makeRank";
				MySqlDataAdapter mySqlDataAdapter = new MySqlDataAdapter(mySqlCommand);
				mySqlCommand.ExecuteNonQuery();
				mySqlCommand.Dispose();
				mySqlConnection.Close();
			}
			Log.Information("Update Collectione Ranking !");
		}
    }
}
