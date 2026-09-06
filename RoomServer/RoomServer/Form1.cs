using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Akka.Actor;
using Akka.Configuration;
using IniParser;
using IniParser.Model;
using MySql.Data.MySqlClient;
using RoomServer.Holders;
using RoomServer.Room;
using RoomServer.Structuring;
using Serilog;
using Serilog.Events;

namespace RoomServer
{
	public class Form1 : Form
	{
		private delegate void EnableDelegate(int id);

		private static Form1 form;

		private IContainer components;

		private RichTextBox richTextBox1;

		private Label label1;

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
			string dates = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_Room.log";
			InMemorySink logEventSink = new InMemorySink(richTextBox1);
			Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().MinimumLevel.Override("Quartz", LogEventLevel.Information).WriteTo.Sink(logEventSink).WriteTo.Logger(delegate(LoggerConfiguration l)
			{
				l.Filter.ByIncludingOnly((LogEvent e1) => e1.Level == LogEventLevel.Information).WriteTo.File(".\\Logs\\Room\\Info\\" + dates, LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, 1073741824L, null, buffered: false, shared: false, null, RollingInterval.Infinite, rollOnFileSizeLimit: false, 31);
			}).WriteTo.Logger(delegate(LoggerConfiguration l)
			{
				l.Filter.ByIncludingOnly((LogEvent e1) => e1.Level == LogEventLevel.Warning).WriteTo.File(".\\Logs\\Room\\Warning\\" + dates, LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, 1073741824L, null, buffered: false, shared: false, null, RollingInterval.Infinite, rollOnFileSizeLimit: false, 31);
			}).WriteTo.Logger(delegate(LoggerConfiguration l)
			{
				l.Filter.ByIncludingOnly((LogEvent e1) => e1.Level == LogEventLevel.Error).WriteTo.File(".\\Logs\\Room\\Error\\" + dates, LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, 1073741824L, null, buffered: false, shared: false, null, RollingInterval.Infinite, rollOnFileSizeLimit: false, 31);
			}).CreateLogger();
			if (!File.Exists("settings.ini"))
			{
				Log.Error("settings.ini doesn't exist!");
				return;
			}
			Boot();
			Config config = ConfigurationFactory.ParseString("\r\n                                akka { \r\n                                    loglevel = INFO,\r\n                                    actor {\r\n                                        provider = remote\r\n                                    }\r\n                                    remote {\r\n                                        dot-netty.tcp {\r\n                                            port = {RMPORT} \r\n                                            hostname = {RMIP}\r\n                                            public-hostname = {RMIP}\r\n                                        }\r\n                                    }\r\n                                    log-dead-letters-during-shutdown = off,\r\n                                    log-dead-letters = 0,\r\n                                    loggers = [\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]\r\n                                }".Replace("{RMIP}", Conf.RMServerIP).Replace("{RMPORT}", Conf.RMLocalPort.ToString()));
			ServerStatus.MainActorSystem = ActorSystem.Create("Room", config);
			RoomHolder.LoadRoomKindInfo();
			MapHolder.LoadMapInfo();
			MapHolder.LoadMapRoomKind();
			MapHolder.LoadAssaultModeLimitInfo();
			MapHolder.LoadRunlympicMapInfo();
			MapHolder.LoadBonusStageInfo();
			MapItemHolder.LoadCapsuleItemMapJoint();
			MapItemHolder.LoadMapCapsuleItemInfo();
			MapItemHolder.LoadAssaultModeRewardInfo();
			MapCardHolder.LoadMapCardRateInfo();
			ItemHolder.LoadItemInfo();
			ItemHolder.LoadItemSetInfo();
			ItemHolder.LoadPetExpInfo();
			ItemHolder.LoadExchangeSystemInfo();
			ItemHolder.LoadItemTransformInfo();
			AccountHolder.LoadLevelInfo();
			RunQuizHolder.LoadRunQuizInfo();
			GameModeHolder.LoadCorunModeResultInfo();
			GameRewardHolder.LoadGameRewardInfo();
			ServerSettingHolder.LoadHashList();
			SubjectKingHolder.LoadSubjectKingQuestionAnswer();
			ItemRacingHolder.LoadItemRacingData();
			TypingRunHolder.LoadTypingRunData();
			TowerEventHolder.LoadTowerQuizInfo();
			if (ServerSettingHolder.ServerSettings.useThankOfferingSystem)
			{
				ThankOfferingSystem.LoadSchedule();
			}
			IceFlowerHolder.LoadIceFlowerInfo();
			RoomResultTable.InitResultTable();
			ServerStatus.ServerActor = ServerStatus.MainActorSystem.ActorOf(Props.Create(() => new AgentServer()), "AgentClient");
		}

		private bool Boot()
		{
			try
			{
				Text = "TR SERVER : RoomServer (RELEASE)";
				IniData iniData = new FileIniDataParser().ReadFile("settings.ini");
				Conf.ServerIP = iniData["Server"]["AgentServerIP"].Replace(" ", "");
				Conf.AgentPort = Convert.ToUInt16(iniData["Server"]["AgentServerTCPPort"].Replace(" ", ""));
				Conf.AgentPort2 = Convert.ToUInt16(iniData["Server"]["AgentServerTCPPort2"].Replace(" ", ""));
				Conf.RelayPort = Convert.ToUInt16(iniData["Server"]["RelayServerPort"].Replace(" ", ""));
				Conf.CommunityAgentServerPort = Convert.ToUInt16(iniData["Server"]["CommunityServerPort"].Replace(" ", ""));
				Conf.LoadBalanceServerPort = Convert.ToUInt16(iniData["Server"]["LoadBalanceServerPort"].Replace(" ", ""));
				Conf.RMLocalPort = Convert.ToUInt16(iniData["Server"]["RoomServerLocalPort"].Replace(" ", ""));
				Conf.RMServerIP = iniData["Server"]["RoomServerIP"].Replace(" ", "");
				Conf.Connstr = iniData["Server"]["MySQLConnection"];
				Conf.HashCheck = Convert.ToBoolean(iniData["Server"]["HashCheck"].Replace(" ", ""));
				Conf.MaxUserCount = Convert.ToInt32(iniData["Server"]["MaxUserCount"].Replace(" ", ""));
				Conf.BlockDDOS = Convert.ToBoolean(iniData["Server"]["BlockDDOS"].Replace(" ", ""));
				Conf.JudgeTime = Convert.ToInt32(iniData["Server"]["JudgeTime"].Replace(" ", ""));
				Conf.MaxConnectTime = Convert.ToInt32(iniData["Server"]["MaxConnectTime"].Replace(" ", ""));
				Conf.TimedOutCheckTime = Convert.ToInt32(iniData["Server"]["TimedOutCheckTime"].Replace(" ", ""));
				Conf.EnableTimedOutCheck = Convert.ToBoolean(iniData["Server"]["EnableTimedOutCheck"].Replace(" ", ""));
				Log.Information("Loading Settings.ini........Done");
				MySqlConnection mySqlConnection = CreateConnection();
				if (mySqlConnection == null)
				{
					return false;
				}
				mySqlConnection.Close();
				ServerSettingHolder.LoadServerSettingInfo();
				Log.Information("Initializing DB........Done");
				return true;
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
			}
			return false;
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
				label1.Text = $"Connected Agent: {ID}";
			}
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBox1.Location = new System.Drawing.Point(12, 37);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(702, 389);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            this.richTextBox1.TextChanged += new System.EventHandler(this.richTextBox1_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(594, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(120, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Connected Agent: 0";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(734, 439);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.richTextBox1);
            this.Font = new System.Drawing.Font("Microsoft JhengHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
          //  this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "RoomServer";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
	}
}
