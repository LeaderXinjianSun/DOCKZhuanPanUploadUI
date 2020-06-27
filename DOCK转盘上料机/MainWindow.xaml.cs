using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Threading;
using HslCommunication;
using MySql.Data.MySqlClient;
using AvaryAPI;
using System.Threading;
using System.Timers;

namespace DOCK转盘上料机
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            LoadIni();

            PLCManager.Initialize();
            PLCManager.RUN();

            RobotManager.Initialize();
            RobotManager.ModelPrint += RobotManager_ModelPrint;
            RobotManager.RobotStateChanged += RobotManager_RobotStateChanged;

            SQLTest();

            isAllow.IsChecked = ORACLE_Enable;

            // ShowTime();
            ShowTimer = new System.Windows.Threading.DispatcherTimer();
            ShowTimer.Tick += new EventHandler(ShowCurTimer);
            ShowTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            ShowTimer.Start();

            System.Timers.Timer t = new System.Timers.Timer(60000);
  
            t.Elapsed +=new ElapsedEventHandler(T_Elapsed);
            t.AutoReset = true;
            t.Enabled = true;


            PLCManager.PLCStateChanged += PLCManager_PLCStateChanged;

            if (PLCManager.Connect)
            {
                PLC_State.Text = "已连接";

            }
            else
            {
                PLC_State.Text = "未连接";

            }

            SQLClient.SQLStateChanged += SQLClient_SQLStateChanged;
            SQLSTART();

            if (SQLClient.Connect)
            {
                SQL_State.Text = "已连接";
            }
            else
            {
                SQL_State.Text = "未连接";
            }
 
            StartPLCToMySQL();

            getWorkNo();
            IORun();
           // TimerDelete_Tick2();
        }
        private async void IORun()
        {
            while (true)
            {
                await Task.Delay(50);
                for (int i = 0; i < 32; i++)
                {
                    PLCManager.FX5uIn[i] = RobotManager.RobotOUT[i];
                    RobotManager.RobotIN[i] = PLCManager.FX5uOut[i];
                }
            }
        }
        static int DaijiCount = 0;

        //心跳表数据上传
        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (USEOracleCheck==10)
            {
                return;
            }
            try
            {
                OperateResult<bool[]> PLCTOMySQLread2 = PLCManager.melsec_net.ReadBool("M93", 3);
                if (PLCTOMySQLread2.IsSuccess)
                {
                    bool[] FX5uOutTOMySQL2 = PLCTOMySQLread2.Content;
                    if (FX5uOutTOMySQL2[0])
                    {
                        DaijiCount = 0;
                        string SQLValue = "insert into TED_HEART_DATA （TestStation,MachineNumber,TestDate,TestTime,AlarmCode,Status,ProgramName,Barcode,Supplier,SystemDate,SystemTime) value('" + WorkStation + "','" + FixtureNumber + "','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','','R','" + PN + "','','LDR','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "')";
                        try
                        {                         
                            InsertAlarm(SQLValue);                         
                        }
                        catch (Exception ex) {  };
                    }
                    else if (FX5uOutTOMySQL2[1])
                    {
                        DaijiCount++;
                        if (DaijiCount>=15)
                        {
                           // LdrLog("待机超过15分钟，已发送停机信号");
                            // Timecount = 5;
                            OperateResult write = PLCManager.melsec_net.Write("M96", true);//PLC写入
                        }
                        string SQLValue = "insert into TED_HEART_DATA （TestStation,MachineNumber,TestDate,TestTime,AlarmCode,Status,ProgramName,Barcode,Supplier,SystemDate,SystemTime) value('" + WorkStation + "','" + FixtureNumber + "','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','','H','" + PN + "','','LDR','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "')";

                        try
                        {

                            InsertAlarm(SQLValue);


                        }
                        catch (Exception ex) { };
                    }
                    else if (FX5uOutTOMySQL2[2])
                    {
                        DaijiCount = 0;
                        string SQLValue = "insert into TED_HEART_DATA （TestStation,MachineNumber,TestDate,TestTime,AlarmCode,Status,ProgramName,Barcode,Supplier,SystemDate,SystemTime) value('" + WorkStation + "','" + FixtureNumber + "','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','"+ PREBaojing + "','A','" + PN + "','','LDR','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "')";

                        try
                        {

                            InsertAlarm(SQLValue);


                        }
                        catch (Exception ex) { };
                    }
                    else
                    {
                        DaijiCount = 0;
                        string SQLValue = "insert into TED_HEART_DATA （TestStation,MachineNumber,TestDate,TestTime,AlarmCode,Status,ProgramName,Barcode,Supplier,SystemDate,SystemTime) value('" + WorkStation + "','" + FixtureNumber + "','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','','R','" + PN + "','','LDR','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "')";

                        try
                        {

                            InsertAlarm(SQLValue);


                        }
                        catch (Exception ex) { };
                    }
                }
            }
            catch { }

            
        }


        //员工号表数据上传
        public void Yuangongbiao(string m,string n, string a)
        {
            if (USEOracleCheck == 10)
            {
                return;
            }
            string SQLValue = "insert into TED_CARD_DATA （TestStation,MachineNumber,TestDate,TestTime,CardNumber,WorkerNumber,EnableRun,ErrMessage,Supplier,SystemDate,SystemTime) value('" + WorkStation + "','" + FixtureNumber + "','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + m + "','" + n+"','" + a + "','','LDR','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "')";
            try//                                                                                                                                                                              TestStation         ,MachineNumber,              TestDate,                                          TestTime,            CardNumber, WorkerNumber,EnableRun,ErrMessage,Supplier,SystemDate,
            {
                InsertAlarm(SQLValue);
            }
            catch (Exception ex) { };
        }

        private string getCardSN(byte[] buf2)
        {
            string str1 = "";
            try
            {
                if (buf2.Length > 0)
                {
                    int sum = 0;
                    string strCard = "";
                    int count = buf2.Length - 1;
                    while (buf2[count] == 0x00) { count--; };
                    for (int i = 0; i <= count; i++)
                    {
                        strCard += string.Format("{0:X2} ", buf2[i]);
                        sum += buf2[i] << i * 8;
                    }
                    str1 = sum.ToString("0000000000"); //0756267432
                }
            } catch { }
            
            return str1;
        }

        //  static int Timecount = 0;//记录倒计时总毫秒数
        //static string str1 = "";
        static string strOperatorSN = ""; //作业员工号 
        static bool res1 = false;
        //获取员工工号

        System.IO.Ports.SerialPort SP = new System.IO.Ports.SerialPort();
       
        public async void getWorkNo()
        {
           
                string tsrt = "";
                CardVerify CV = new CardVerify();
            try
            {
                CV.checkOperatorAbility("12345678", ref tsrt);
            }
            catch
            {

            }
           
            await Task.Delay(500);
           
            while (true)
            {
                await Task.Delay(100);
                string str1 = "";
                
                Task Task_Test = Task.Run(() =>
                {
                    try
                    {
                       
                        SP = new System.IO.Ports.SerialPort();
                        SP.BaudRate = 9600;
                        SP.PortName = DuKaQi;
                        SP.ReceivedBytesThreshold = 1;
                        


                        if (!SP.IsOpen)
                        {
                            SP.Open();
                        }

                        Thread.Sleep(500);
                        int len = SP.BytesToRead;
                        if (len >= 8)
                        {
                            byte[] buf = new byte[len];
                            SP.Read(buf, 0, len);

                           str1 = getCardSN(new byte[] { buf[2], buf[3], buf[4], buf[5] });
                            SP.DiscardInBuffer();
                        }

                        SP.Close();

                    }
                catch
                    {
                        if (SP.IsOpen)
                        {
                            SP.Close();
                        }
                    }
                });
                await Task_Test;
               if (str1.Length < 8)
                   
                    {
                    continue;

                }
               try
                {
                    string str2 = "";
                    
                    CV = new CardVerify();
                     res1 = CV.checkOperatorAbility(str1, ref str2);
                    strOperatorSN = new CardVerify().getOperatorNumber(str1);
                } catch { }
                             
                WorkNo_TB.Text = strOperatorSN;
                 LdrLog("工号为：" + strOperatorSN);//有上岗证                  
                if (res1)
                {
                    LdrLog("欢迎，您有上岗证,已发送启动信号");
                    OperateResult write = PLCManager.melsec_net.Write("M97", true);//PLC写入
                    Yuangongbiao(str1, strOperatorSN, "Y");
                }
                else
                {
                    LdrLog("抱歉，您无上岗证,已发送停机信号");                 
                    OperateResult write = PLCManager.melsec_net.Write("M96", true);//PLC写入    
                    Yuangongbiao(str1, strOperatorSN, "N");
                }             
            }          
        }
        static int Timecount = 60*600;

        static bool starttime = false;
        private async void TimerDelete_Tick2()
        {
            if (!starttime)
                starttime = true;
            else
                return;
           // LdrLog("调用方法");
            try
            {
                while (starttime)
                {
                    await Task.Delay(1000);
                    txthour.Text = (Timecount / 3600).ToString();
                    txtmm.Text = ((Timecount / 60) % 60).ToString();
                    txtss.Text = ((Timecount / 1) % 60).ToString();

                    //label1.Text = hour.ToString() + "时 " + minute.ToString() + "分 " + second.ToString() + "秒" + millsecond + "毫秒";
                    if (Timecount == 0)
                    {



                        LdrLog("时间结束，请重新刷卡");
                        OperateResult write = PLCManager.melsec_net.Write("M96", true);//PLC写入
                        starttime = false;
                        //try
                        //{
                        //    Thread currthread = Thread.CurrentThread;
                        //    currthread.Abort();// 终止当前进程，会触发ThreadAbortException异常，从而终止进程，所以下面需要捕获该异常才能终止进程
                        //}
                        //catch (ThreadAbortException) { }
                    }
                    Timecount -= 1;
                    //   Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }//处理异常关闭情况下的异常问题
        }

       

        //读D值获取产量

        //public int DFx5u1;//总产量 D804   以D5000测试  
        //public int DFx5u2;//当班产量  D59   以D5005测试  
        ////int all=0;//总产量 D804   以D5000测试  
        ////int cur=0;//当前产量
        ////int db=0;//当班产量  D59   以D5005测试  

        //private void DispatcherTimerTickUpdateUi(object sender, EventArgs e)
        //{
        //    OperateResult<int> read1 = PLCManager.melsec_net.ReadInt32("D804");
        //    OperateResult<int> read2 = PLCManager.melsec_net.ReadInt32("D59");
        //    if (read1.IsSuccess && read1.IsSuccess)
        //    {
        //        DFx5u1 = read1.Content;
        //        DFx5u2 = read2.Content;

        //        ALL_Number.Text = DFx5u1.ToString();

        //        DB_Number.Text = DFx5u2.ToString();


        //    }
        //}


        private void SQLClient_SQLStateChanged(object sender, bool e)
        {

            this.Dispatcher.BeginInvoke(new mydelegate(SQLSTATE), e);
        }



        public void SQLSTATE(bool _State)
        {

            if (SQLClient.Connect)
            {
                SQL_State.Text = "已连接";
            }
            else
            {
                SQL_State.Text = "未连接";
            }
        }


        string SQL_Result = "";
        /// <summary>
        /// 数据库状态检测，每3秒刷新一次
        /// </summary>
        public async void SQLSTART()
        {
            while (true)
            {
                SQL_Result = "";
                Task Task_SQLSTA = Task.Run(() =>
                {

                    System.Threading.Thread.Sleep(10000);
                    try
                    {
                        SQL_Result = SQLClient.test("Server=" + ShuJuKuIP + ";database=" + ShuJuKuFW + ";uid=" + ShuJuKuYH + ";pwd=" + ShuJuKuMM);
                        // SQL_Result = SQLClient.test("Server=qhdcsgc01.eavarytech.com;database=dcdb;uid=dcu;pwd=dcudata");

                    }
                    catch
                    {


                    }


                });
                await Task_SQLSTA;
                if (SQLClient.Connect)
                {
                    SQLClient.Connect = true;
                    Console.WriteLine("数据库已连接");
                }
                else
                {
                    SQLClient.Connect = false;
                    SQL_State.Text = "未连接";
                    LdrLog("数据库重新连接中......");
                }

            }
        }



        public delegate void mydelegate(bool _State);

        private void PLCManager_PLCStateChanged(object sender, bool e)
        {
            this.Dispatcher.BeginInvoke(new mydelegate(PLCStateChanged), e);
        }
        private void RobotManager_RobotStateChanged(object sender, bool e)
        {
            string str = "机械手" + (e ? "连接" : "断开");
            LdrLog(str);
        }
        private void RobotManager_ModelPrint(string e)
        {
            LdrLog(e);
        }


        public void PLCStateChanged(bool _State)
        {
            if (PLCManager.Connect)
            {
                PLC_State.Text = "已连接";

            }
            else
            {
                PLC_State.Text = "未连接";

            }
        }



        static string Banci = "D";

        private DispatcherTimer ShowTimer;
        public void ShowCurTimer(object sender, EventArgs e)
        {
            ShowTime();
            string nowHour = DateTime.Now.Hour.ToString();
            string nowMinute = DateTime.Now.Minute.ToString();
            string nowSecond = DateTime.Now.Second.ToString();

            if ((nowHour == "8" && nowMinute == "0" && nowSecond == "0") || (nowHour == "8" && nowMinute == "0" && nowSecond == "1") )
            {
                LdrLog("白班开始!!!,请重新的刷卡");
                Banci = "D";
               // LdrLog("抱歉，您无上岗证,已发送停机信号");
                // Timecount = 5;
                OperateResult write = PLCManager.melsec_net.Write("M96", true);//PLC写入

            }
            if ((nowHour == "20" && nowMinute == "0" && nowSecond == "0") || (nowHour == "20" && nowMinute == "0" && nowSecond == "1") )
            {
                LdrLog("夜班开始!!!,请重新的刷卡");
                Banci = "N";
                OperateResult write = PLCManager.melsec_net.Write("M96", true);//PLC写入
            }
            if (nowHour == "8" || nowHour == "9" || nowHour == "10" || nowHour == "11" || nowHour == "12" || nowHour == "13" || nowHour == "14" || nowHour == "15" || nowHour == "16" || nowHour == "17" || nowHour == "18" || nowHour == "19")
            {
                Banci = "D";
            }
            else
            {
                Banci = "N";
            }
        }

        //显示当前时间
        private void ShowTime()
        {
            this.localTime_tb.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");   //yyyy/MM/dd
        }




        //MYSQL数据库
        public async void SQLTest()
        {
            string SQL_Result = "";

            try
            {
                mShuJuKuIP = DXH.Ini.DXHIni.ContentReader("数据库", "数据库IP", "", GlobalFile);
                mShuJuKuFW = DXH.Ini.DXHIni.ContentReader("数据库", "数据库服务名", "", GlobalFile);
                mShuJuKuYH = DXH.Ini.DXHIni.ContentReader("数据库", "数据库账号", "", GlobalFile);
                mShuJuKuMM = DXH.Ini.DXHIni.ContentReader("数据库", "数据库密码", "", GlobalFile);
                ShuJuKuIP = mShuJuKuIP;
                ShuJuKuFW = mShuJuKuFW;
                ShuJuKuYH = mShuJuKuYH;
                ShuJuKuMM = mShuJuKuMM;
            }
            catch { }


            Task Task_SQLTest = Task.Run(() =>
            {
                try
                {

                    SQL_Result = SQLClient.test("Server=" + ShuJuKuIP + ";database=" + ShuJuKuFW + ";uid=" + ShuJuKuYH + ";pwd=" + ShuJuKuMM);

                    // Console.WriteLine("1111");

                }
                catch
                {

                }
            });
            await Task_SQLTest;
            LdrLog(SQL_Result);
            //LdrLog("111");
            //LdrLog("111");
            //LdrLog("111");



        }


        //运行日志部分
        public string Log { get; set; }
        string LogHeader = " -> ";
        object LogLock = new object();
        int LogLine = 0;
        public async void LdrLog(string strtoappend)
        {
            Task task_Log = Task.Run(() =>
            {
                lock (LogLock)//多线程同时输出时会丢失Log
                {
                    LogLine++;
                    if (LogLine > 500)//最多500行。
                    {
                        Log = "";
                        LogLine = 1;
                    }
                    Log = Log + CurTime() + LogHeader + strtoappend + Environment.NewLine;
                }
            });
            await task_Log;
            TextBox.Text = Log;
        }
        public string CurTime()
        {
            return DateTime.Now.ToString();
        }


        double ScrollOffset = 0;
        int SelectionStart = 0;
        int SelectionLength = 0;
        private void TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScrollOffset = Scroll.VerticalOffset;
            if (!Scroll.IsMouseOver && SelectionLength == 0)
            {
                Scroll.ScrollToEnd();
                SelectionStart = 0;
                SelectionLength = 0;
            }
            else
            {
                if (SelectionLength == 0 && SelectionStart == 0)
                { }
                else
                    TextBox.Select(SelectionStart, SelectionLength);
                Scroll.ScrollToVerticalOffset(ScrollOffset);
            }
        }
        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (TextBox.SelectionStart == 0 && TextBox.SelectionLength == 0)
            {
            }
            else
            {
                SelectionStart = TextBox.SelectionStart;
                SelectionLength = TextBox.SelectionLength;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            MessageBoxResult result = MessageBox.Show("确定退出软件吗？", "退出前确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

            //关闭窗口 
            if (result == MessageBoxResult.Yes)
            {
                if (SQL.IsEnabled)
                {

                    e.Cancel = false;
                }
                else
                {
                    System.Windows.MessageBox.Show("未登录，无权限退出！！！");
                    e.Cancel = true;
                }

            }


            //不关闭窗口
            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }

        private void SQL_Click(object sender, RoutedEventArgs e)
        {
            SQLTest();

            //string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('DOCK','FQAPKC0','DOCK','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL10','A033','1','','','','','','','','','','右工位取料气缸复位报')";

            //try
            //{

            //    InsertAlarm(SQLValue);


            //}
            //catch (Exception ex) { Console.WriteLine(ex.ToString()); };
        }


        public short[] DFx5u2 = new short[3];
        public string b1 = "";
        public string b2 = "";
        public string b3 = "";
        //为解决ldrlog无法在其他线程使用
        string mm = "";

        string m1 = "";
        string m2 = "";
        string m3 = "";
        string m4 = "";
        string m5 = "";
        string m6 = "";
        string m7 = "";
        string m8 = "";
        string m9 = "";
        string m10 = "";
        string m11 = "";
        string m12 = "";
        string m13 = "";
        string m14 = "";
        string m15 = "";
        string m16 = "";
        string m17 = "";
        string m18 = "";
        string m19 = "";
        string m20 = "";
        string m21 = "";
        string m22 = "";
        string m23 = "";
        string m24 = "";
        string m25 = "";
        string m26 = "";
        string m27 = "";
        string m28 = "";
        string m29 = "";
        string m30 = "";
        string m31 = "";
        string m32 = "";
        string m33 = "";
        string m34 = "";
        string m35 = "";
        string m36 = "";
        string m37 = "";
        string m38 = "";
        string m39 = "";
        string m40 = "";
        string m41 = "";
        string m42 = "";
        string m43 = "";
        string m44 = "";
        string m45 = "";
        string m46 = "";
        string m47 = "";
        string m48 = "";
        string m49 = "";
        string m50 = "";
        string m51 = "";
        string m52 = "";
        string m53 = "";
        string m54 = "";
        string m55 = "";
        string m56 = "";
        string m57 = "";
        string m58 = "";
        string m59 = "";
        string m60 = "";
        string m61 = "";
        string m103 = "";
        string m104 = "";
        string m105 = "";
        string m106 = "";
        string m117 = "";

        static string PREBaojing = "";
        //读M上传报警信息
        //PLCM点上升沿的时候插入数据库
        static bool mPLCToMySQL = false;//1
        static string PreRsedStr = "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        public async void StartPLCToMySQL()
        {
            if (!mPLCToMySQL)
                mPLCToMySQL = true;
            else
                return;
            await Task.Delay(4000);

            while (mPLCToMySQL)
            {
                await Task.Delay(100);
                string CurRsedStr = "";
                mm = "";

                m1 = "";
                m2 = "";
                m3 = "";
                m4 = "";
                m5 = "";
                m6 = "";
                m7 = "";
                m8 = "";
                m9 = "";
                m10 = "";
                m11 = "";
                m12 = "";
                m13 = "";
                m14 = "";
                m15 = "";
                m16 = "";
                m17 = "";
                m18 = "";
                m19 = "";
                m20 = "";
                m21 = "";
                m22 = "";
                m23 = "";
                m24 = "";
                m25 = "";
                m26 = "";
                m27 = "";
                m28 = "";
                m29 = "";
                m30 = "";
                m31 = "";
                m32 = "";
                m33 = "";
                m34 = "";
                m35 = "";
                m36 = "";
                m37 = "";
                m38 = "";
                m39 = "";
                m40 = "";

                m41 = "";//PLCM90
                m42 = "";
                m43 = "";
                m44 = "";
                m45 = "";
                m46 = "";
                m47 = "";
                m48 = "";
                m49 = "";
                m50 = "";
                m51 = "";
                m52 = "";
                m53 = "";
                m54 = "";
                m55 = "";
                m56 = "";
                m57 = "";
                m58 = "";
                m59 = "";
                m60 = "";
                m61 = "";
                m103 = "";
                m104 = "";
                m105 = "";
                m106 = "";
                m117 = "";
                try
                {//'DOCK','FQAPKC0','QHDOCKFQAPKCO-01'
                    mMarkAddress = DXH.Ini.DXHIni.ContentReader("计算机", "物理地址", "", GlobalFile);
                    mGZVersion = DXH.Ini.DXHIni.ContentReader("计算机", "故障代码版本", "", GlobalFile);
                    mWorkStation = DXH.Ini.DXHIni.ContentReader("计算机", "测试工站", "", GlobalFile);
                    mPN = DXH.Ini.DXHIni.ContentReader("计算机", "测试料号", "", GlobalFile);
                    mFixtureNumber = DXH.Ini.DXHIni.ContentReader("计算机", "治具编号", "", GlobalFile);
                    MarkAddress = mMarkAddress;
                    GZVersion = mGZVersion;
                    WorkStation = mWorkStation;
                    PN = mPN;
                    FixtureNumber = mFixtureNumber;
                }
                catch { }

                Task Read_D = Task.Run(() =>
                {




                    OperateResult<bool[]> PLCTOMySQLread = PLCManager.melsec_net.ReadBool("M50", 120);

                    try
                    {
                        OperateResult<short[]> read2 = PLCManager.melsec_net.ReadInt16("D60", 3);
                        if (read2.IsSuccess)
                        {
                            //DFx5u1 = read1.Content;
                            DFx5u2 = read2.Content;


                            b1 = Convert.ToString(DFx5u2[0], 2);
                            b2 = Convert.ToString(DFx5u2[1], 2);
                            b3 = Convert.ToString(DFx5u2[2], 2);
                        }
                    }
                    catch
                    {
                        b1 = "0";
                        b2 = "0";
                        b3 = "0";
                    }
                    if (PLCTOMySQLread.IsSuccess)
                    {
                        bool[] FX5uOutTOMySQL = PLCTOMySQLread.Content;

                        for (int i = 0; i < 120; i++)
                        {
                            CurRsedStr += FX5uOutTOMySQL[i] ? "1" : "0";//PLC读出的数据，如果有变化，就写入数据库
                        }
                        for (int i = 0; i < 120; i++)
                        {
                            if (PreRsedStr.Substring(i, 1) == "0" && CurRsedStr.Substring(i, 1) == "1")//上升沿
                            {
                                mm = "mm";


                                if (i == 0)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-1','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-1";
                                        m1 = "m1";
                                        InsertAlarm(SQLValue);

                                        SaveTest("机械手异常报警");

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 1)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-2','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-2";
                                        m2 = "m2";
                                        InsertAlarm(SQLValue);

                                        SaveTest("机械手重故障报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 2)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-3','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-3";
                                        m3 = "m3";
                                        InsertAlarm(SQLValue);

                                        SaveTest("左_X轴伺服报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 3)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-4','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-4";
                                        m4 = "m4";
                                        InsertAlarm(SQLValue);

                                        SaveTest("左_Y轴伺服报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 4)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-5','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-5";
                                        m5 = "m5";
                                        InsertAlarm(SQLValue);

                                        SaveTest("右_X轴伺服报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 5)
                                {

                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-6','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-6";
                                        m6 = "m6";
                                        InsertAlarm(SQLValue);

                                        SaveTest("右_Y轴伺服报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 6)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-7','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-7";
                                        m7 = "m7";
                                        InsertAlarm(SQLValue);

                                        SaveTest("左工位吸料失败");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 7)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-8','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-8";
                                        m8 = "m8";
                                        InsertAlarm(SQLValue);

                                        SaveTest("右工位吸料失败");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 8)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-9','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-9";
                                        m9 = "m9";
                                        InsertAlarm(SQLValue);

                                        SaveTest("气缸未复位完成报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 9)//m59
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-10','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-10";
                                        m10 = "m10";
                                        InsertAlarm(SQLValue);

                                        SaveTest("上料位 载盘顶升气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 10)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-11','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-11";
                                        m11 = "m11";
                                        InsertAlarm(SQLValue);

                                        SaveTest("上料位 载盘顶升气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 11)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-12','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-12";
                                        m12 = "m12";
                                        InsertAlarm(SQLValue);

                                        SaveTest("上料位 分盘上下气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 12)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-13','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-13";
                                        m13 = "m13";
                                        InsertAlarm(SQLValue);

                                        SaveTest("上料位 分盘上下气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 13)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-14','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-14";
                                        m14 = "m14";
                                        InsertAlarm(SQLValue);

                                        SaveTest("等待位 阻挡气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 14)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-15','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-15";
                                        m15 = "m15";
                                        InsertAlarm(SQLValue);

                                        SaveTest("等待位 阻挡气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 15)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-16','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-16";
                                        m16 = "m16";
                                        InsertAlarm(SQLValue);

                                        SaveTest("取料位 阻挡气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 16)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-17','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-17";
                                        m17 = "m17";
                                        InsertAlarm(SQLValue);

                                        SaveTest("取料位 阻挡气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 17)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-18','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-18";
                                        m18 = "m18";
                                        InsertAlarm(SQLValue);

                                        SaveTest("取料位 顶升气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 18)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-19','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-19";
                                        m19 = "m19";
                                        InsertAlarm(SQLValue);

                                        SaveTest("取料位 顶升气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 19)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-20','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-20";
                                        m20 = "m20";
                                        InsertAlarm(SQLValue);

                                        SaveTest("下料位 顶升气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 20)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-21','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-21";
                                        m21 = "m21";
                                        InsertAlarm(SQLValue);

                                        SaveTest("下料位 顶升气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 21)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-22','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-22";
                                        m22 = "m22";
                                        InsertAlarm(SQLValue);

                                        SaveTest("左工位矫正气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 22)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-23','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-23";
                                        m23 = "m23";
                                        InsertAlarm(SQLValue);

                                        SaveTest("左工位矫正气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                //if (i == 23)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('DOCK','FQAPKC0','DOCK','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL10','A024','1','','','','','','','','','','')";

                                //    try
                                //    {
                                //        m24 = "m24";
                                //        InsertAlarm(SQLValue);

                                //        SaveTest("左工位矫正气缸2置位报警");
                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                //if (i == 24)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('DOCK','FQAPKC0','DOCK','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL10','A025','1','','','','','','','','','','')";

                                //    try
                                //    {
                                //        m25 = "m25";
                                //        InsertAlarm(SQLValue);

                                //        SaveTest("左工位矫正气缸2复位报警");
                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                if (i == 25)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-24','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-24";
                                        m26 = "m26";
                                        InsertAlarm(SQLValue);

                                        SaveTest("右工位矫正气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 26)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-25','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-25";
                                        m27 = "m27";
                                        InsertAlarm(SQLValue);

                                        SaveTest("右工位矫正气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                //if (i == 27)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('DOCK','FQAPKC0','DOCK','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL10','A028','1','','','','','','','','','','')";

                                //    try
                                //    {
                                //        m28 = "m28";
                                //        InsertAlarm(SQLValue);

                                //        SaveTest("右工位矫正气缸2置位报警");
                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                //if (i == 28)
                                //{

                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('DOCK','FQAPKC0','DOCK','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL10','A029','1','','','','','','','','','','')";

                                //    try
                                //    {
                                //        m29 = "m29";
                                //        InsertAlarm(SQLValue);

                                //        SaveTest("右工位矫正气缸2复位报警");
                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                if (i == 29)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-26','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-26";
                                        m30 = "m30";
                                        InsertAlarm(SQLValue);

                                        SaveTest("左工位取料气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 30)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-27','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-27";
                                        m31 = "m31";
                                        InsertAlarm(SQLValue);

                                        SaveTest("左工位取料气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 31)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-28','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-28";
                                        m30 = "m30";
                                        InsertAlarm(SQLValue);

                                        SaveTest("右工位取料气缸置位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 32)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-29','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-29";
                                        m31 = "m31";
                                        InsertAlarm(SQLValue);

                                        SaveTest("右工位取料气缸复位报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 33)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-30','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-30";
                                        m34 = "m34";
                                        InsertAlarm(SQLValue);

                                        SaveTest("气缸有报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 34)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-31','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-31";
                                        m35 = "m35";
                                        //InsertAlarm(SQLValue);

                                        SaveTest("急停报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 35)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-32','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-32";
                                        m36 = "m36";
                                        InsertAlarm(SQLValue);

                                        SaveTest("右侧门报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 36)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-33','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-33";
                                        m37 = "m37";
                                        InsertAlarm(SQLValue);

                                        SaveTest("前门报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 37)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-34','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-34";
                                        m38 = "m38";
                                        InsertAlarm(SQLValue);

                                        SaveTest("左1门报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 38)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-35','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-35";
                                        m39 = "m39";
                                        InsertAlarm(SQLValue);

                                        SaveTest("左2门报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 39)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-36','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-36";
                                        m40 = "m40";
                                        InsertAlarm(SQLValue);

                                        SaveTest("后门报警");
                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 40)
                                {
                                    try
                                    {
                                        OperateResult<short[]> read2 = PLCManager.melsec_net.ReadInt16("D60", 3);
                                        if (read2.IsSuccess)
                                        {
                                            //DFx5u1 = read1.Content;
                                            DFx5u2 = read2.Content;


                                            b1 = Convert.ToString(DFx5u2[0], 2);
                                            b2 = Convert.ToString(DFx5u2[1], 2);
                                            b3 = Convert.ToString(DFx5u2[2], 2);

                                            c1 = Convert.ToInt32(b1, 2);
                                            c2 = Convert.ToInt32(b2, 2);
                                            c3 = Convert.ToInt32(b3, 2);

                                            // LdrLog("读取成功" + "D60为：" + c1.ToString() + "  D61为：" + c2.ToString() + "  D62为：" + c3.ToString());
                                        }
                                        else
                                        {
                                            //  LdrLog("读取失败");
                                            c1 = 0;
                                            c2 = 0;
                                            c3 = 0;
                                        }
                                    }
                                    catch
                                    {

                                        // LdrLog("读取失败");
                                        c1 = 0;
                                        c2 = 0;
                                        c3 = 0;
                                    }



                                    string nn = c1.ToString() + "S " + c2.ToString() + "S " + c3.ToString() + "S";
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-45','','1','','','','','','','','','','" + nn + "','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        m41 = "m41";
                                        InsertAlarm(SQLValue);

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };


                                    //string nn = b1 + "S:" + b2 + "S:" + b3 + "S";
                                    //string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','F-LDR-45','','1','','','','','','','','','','" + nn + "','LDR','" + GZVersion + "')";

                                    //try
                                    //{
                                    //  
                                    //    InsertAlarm(SQLValue);


                                    //}
                                    //catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 41)
                                {

                                    //try
                                    //{
                                    //    OperateResult<short[]> read2 = PLCManager.melsec_net.ReadInt16("D60", 3);
                                    //    if (read2.IsSuccess)
                                    //    {
                                    //        //DFx5u1 = read1.Content;
                                    //        DFx5u2 = read2.Content;


                                    //        b1 = Convert.ToString(DFx5u2[0], 2);
                                    //        b2 = Convert.ToString(DFx5u2[1], 2);
                                    //        b3 = Convert.ToString(DFx5u2[2], 2);
                                    //    }
                                    //}
                                    //catch
                                    //{
                                    //    b1 = "0";
                                    //    b2 = "0";
                                    //    b3 = "0";
                                    //}
                                    try
                                    {
                                        OperateResult<short[]> read2 = PLCManager.melsec_net.ReadInt16("D60", 3);
                                        if (read2.IsSuccess)
                                        {
                                            //DFx5u1 = read1.Content;
                                            DFx5u2 = read2.Content;


                                            b1 = Convert.ToString(DFx5u2[0], 2);
                                            b2 = Convert.ToString(DFx5u2[1], 2);
                                            b3 = Convert.ToString(DFx5u2[2], 2);

                                            c1 = Convert.ToInt32(b1, 2);
                                            c2 = Convert.ToInt32(b2, 2);
                                            c3 = Convert.ToInt32(b3, 2);

                                            //   LdrLog("读取成功" + "D60为：" + c1.ToString() + "  D61为：" + c2.ToString() + "  D62为：" + c3.ToString());
                                        }
                                        else
                                        {
                                            //     LdrLog("读取失败");
                                            c1 = 0;
                                            c2 = 0;
                                            c3 = 0;
                                        }
                                    }
                                    catch
                                    {

                                        //   LdrLog("读取失败");
                                        c1 = 0;
                                        c2 = 0;
                                        c3 = 0;
                                    }



                                    string nn = c1.ToString() + "S" + c2.ToString() + "S" + c3.ToString() + "S";
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-45','','1','','','','','','','','','','" + nn + "','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        m42 = "m42";
                                        InsertAlarm(SQLValue);

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };

                                    //string nn = b1 + "S:" + b2 + "S:" + b3 + "S";
                                    //string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','F-LDR-46','','1','','','','','','','','','','" + nn + "','LDR','" + GZVersion + "')";

                                    //try
                                    //{
                                    //    m42 = "m42";
                                    //    InsertAlarm(SQLValue);


                                    //}
                                    //catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 42)
                                {

                                    //try
                                    //{
                                    //    OperateResult<short[]> read2 = PLCManager.melsec_net.ReadInt16("D60", 3);
                                    //    if (read2.IsSuccess)
                                    //    {
                                    //        //DFx5u1 = read1.Content;
                                    //        DFx5u2 = read2.Content;


                                    //        b1 = Convert.ToString(DFx5u2[0], 2);
                                    //        b2 = Convert.ToString(DFx5u2[1], 2);
                                    //        b3 = Convert.ToString(DFx5u2[2], 2);
                                    //    }
                                    //}
                                    //catch
                                    //{
                                    //    b1 = "0";
                                    //    b2 = "0";
                                    //    b3 = "0";
                                    //}
                                    try
                                    {
                                        OperateResult<short[]> read2 = PLCManager.melsec_net.ReadInt16("D60", 3);
                                        if (read2.IsSuccess)
                                        {
                                            //DFx5u1 = read1.Content;
                                            DFx5u2 = read2.Content;


                                            b1 = Convert.ToString(DFx5u2[0], 2);
                                            b2 = Convert.ToString(DFx5u2[1], 2);
                                            b3 = Convert.ToString(DFx5u2[2], 2);

                                            c1 = Convert.ToInt32(b1, 2);
                                            c2 = Convert.ToInt32(b2, 2);
                                            c3 = Convert.ToInt32(b3, 2);

                                            //  LdrLog("读取成功" + "D60为：" + c1.ToString() + "  D61为：" + c2.ToString() + "  D62为：" + c3.ToString());
                                        }
                                        else
                                        {
                                            // LdrLog("读取失败");
                                            c1 = 0;
                                            c2 = 0;
                                            c3 = 0;
                                        }
                                    }
                                    catch
                                    {

                                        //  LdrLog("读取失败");
                                        c1 = 0;
                                        c2 = 0;
                                        c3 = 0;
                                    }



                                    string nn = c1.ToString() + "S" + c2.ToString() + "S" + c3.ToString() + "S";
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-45','','1','','','','','','','','','','" + nn + "','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        m43 = "m43";
                                        InsertAlarm(SQLValue);

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                    //string nn = b1 + "S:" + b2 + "S:" + b3 + "S";
                                    //string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','F-LDR-47','','1','','','','','','','','','','" + nn + "','LDR','" + GZVersion + "')";

                                    //try
                                    //{
                                    //    m43 = "m43";
                                    //    InsertAlarm(SQLValue);


                                    //}
                                    //catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                //if (i == 43)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('DOCK','FQAPKC0','DOCK','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL02','A033','1','','','','','','','','','','')";

                                //    try
                                //    {
                                //        m44 = "m44";
                                //        InsertAlarm(SQLValue);

                                //        SaveTest("门9报警");
                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                //if (i == 45)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL02','A032','1','','','','','','','','','','')";

                                //    try
                                //    {
                                //        m45 = "m45";
                                //        InsertAlarm(SQLValue);

                                //        SaveTest("门10报警");
                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                //if (i == 45)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('DOCK','FQAPKC0','DOCK','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL02','A033','1','','','','','','','','','','')";

                                //    try
                                //    {
                                //        m46 = "m46";
                                //        InsertAlarm(SQLValue);

                                //        SaveTest("门11报警");
                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                //if (i == 46)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('DOCK','FQAPKC0','DOCK','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL02','A033','1','','','','','','','','','','')";

                                //    try
                                //    {
                                //        m47 = "m47";
                                //        InsertAlarm(SQLValue);

                                //        SaveTest("门12报警");
                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                //if (i == 48)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','FL02','A035','1','','','','','','','','','','')";

                                //    try
                                //    {
                                //        m48 = "m48";
                                //        InsertAlarm(SQLValue);

                                //        SaveTest("门13报警");
                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                if (i == 48)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-37','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-37";
                                        m49 = "m49";
                                        InsertAlarm(SQLValue);

                                        SaveTest("下料位料盘已满 ");

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }


                                if (i == 54)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-38','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-38";
                                        m55 = "m55";
                                        InsertAlarm(SQLValue);

                                        SaveTest("回抓左侧转盘料失败，检查位置偏差 ");

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }

                                if (i == 55)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-39','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-39";
                                        m56 = "m56";
                                        InsertAlarm(SQLValue);

                                        SaveTest("回抓右侧转盘料失败，检查位置偏差 ");

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }


                                //if (i == 56)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','F-LDR-43','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                //    try
                                //    {
                                //        m57 = "m57";
                                //        InsertAlarm(SQLValue);
                                //        
                                //        SaveTest("左Y轴软限位报警 ");

                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}


                                //if (i == 57)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','F-LDR-44','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                //    try
                                //    {
                                //        m58 = "m58";
                                //        InsertAlarm(SQLValue);
                                //     
                                //        SaveTest("右X轴软限位报警 ");

                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}


                                //if (i == 58)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','F-LDR-45','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                //    try
                                //    {
                                //        m59 = "m59";
                                //        InsertAlarm(SQLValue);
                                //      
                                //        SaveTest("右Y轴软限位报警 ");

                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}


                                //if (i == 59)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','F-LDR-46','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                //    try
                                //    {
                                //        m60 = "m60";
                                //        InsertAlarm(SQLValue);
                                //      
                                //        SaveTest("左电测机报警 ");

                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}


                                //if (i == 60)
                                //{
                                //    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','F-LDR-47','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                //    try
                                //    {
                                //        m61 = "m61";
                                //        InsertAlarm(SQLValue);
                                //      
                                //        SaveTest("右电测机报警 ");

                                //    }
                                //    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                //}
                                if (i == 102)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-40','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-40";
                                        m103 = "m103";
                                        InsertAlarm(SQLValue);

                                        SaveTest("机械手A爪吸料失败，检查位置偏差 ");

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 103)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-41','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-41";
                                        m104 = "m104";
                                        InsertAlarm(SQLValue);

                                        SaveTest("机械手B爪吸料失败，检查位置偏差");

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 104)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-42','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-42";
                                        m105 = "m105";
                                        InsertAlarm(SQLValue);

                                        SaveTest("机械手A爪取样本失败，检查位置偏差");

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 105)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-43','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-43";
                                        m106 = "m106";
                                        InsertAlarm(SQLValue);

                                        SaveTest("PLC与机械手通信失败，通知电测人员");

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }
                                if (i == 116)
                                {
                                    string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-44','','1','','','','','','','','','','','LDR','" + GZVersion + "')";

                                    try
                                    {
                                        PREBaojing = "W-LDR-44";
                                        m117 = "m117";
                                        InsertAlarm(SQLValue);

                                        SaveTest("上料区剥盘失败，检查气缸");

                                    }
                                    catch (Exception ex) { Console.WriteLine(ex.ToString()); };
                                }


                                //新加
                            }
                        }
                        PreRsedStr = CurRsedStr;
                    }
                });
                await Read_D;
                if (mm == "mm")
                {

                    if (m1 == "m1")
                    {
                        LdrLog("机械手异常报警，重开电");
                    }
                    if (m2 == "m2")
                    {
                        LdrLog("机械手重故障报警，重开电");
                    }
                    if (m3 == "m3")
                    {
                        LdrLog("左_X轴伺服报警，重开电");
                    }
                    if (m4 == "m4")
                    {
                        LdrLog("左_Y轴伺服报警，重开电");
                    }
                    if (m5 == "m5")
                    {
                        LdrLog("右_X轴伺服报警，重开电");
                    }
                    if (m6 == "m6")
                    {
                        LdrLog("右_Y轴伺服报警，重开电");
                    }
                    if (m7 == "m7")
                    {
                        LdrLog("左工位吸料失败，检查位置和真空");
                    }
                    if (m8 == "m8")
                    {
                        LdrLog("右工位吸料失败，检查位置和真空");
                    }
                    if (m9 == "m9")
                    {
                        LdrLog("气缸未复位完成报警，检查气缸");
                    }
                    if (m10 == "m10")
                    {
                        LdrLog("上料位 载盘顶升气缸置位报警，检查气缸");
                    }
                    if (m11 == "m11")
                    {
                        LdrLog("上料位 载盘顶升气缸复位报警，检查气缸");
                    }
                    if (m12 == "m12")
                    {
                        LdrLog("上料位 分盘上下气缸置位报警，检查气缸");
                    }
                    if (m13 == "m13")
                    {
                        LdrLog("上料位 分盘上下气缸复位报警，检查气缸");
                    }
                    if (m14 == "m14")
                    {
                        LdrLog("等待位 阻挡气缸置位报警，检查气缸");
                    }
                    if (m15 == "m15")
                    {
                        LdrLog("等待位 阻挡气缸复位报警，检查气缸");
                    }
                    if (m16 == "m16")
                    {
                        LdrLog("取料位 阻挡气缸置位报警，检查气缸");
                    }
                    if (m17 == "m17")
                    {
                        LdrLog("取料位 阻挡气缸复位报警，检查气缸");
                    }
                    if (m18 == "m18")
                    {
                        LdrLog("取料位 顶升气缸置位报警，检查气缸");
                    }
                    if (m19 == "m19")
                    {
                        LdrLog("取料位 顶升气缸复位报警，检查气缸");
                    }
                    if (m20 == "m20")
                    {
                        LdrLog("下料位 顶升气缸置位报警，检查气缸");
                    }
                    if (m21 == "m21")
                    {
                        LdrLog("下料位 顶升气缸复位报警，检查气缸");
                    }
                    if (m22 == "m22")
                    {
                        LdrLog("左工位矫正气缸置位报警，检查气缸");
                    }
                    if (m23 == "m23")
                    {
                        LdrLog("左工位矫正气缸复位报警，检查气缸");
                    }

                    if (m26 == "m26")
                    {
                        LdrLog("右工位矫正气缸置位报警，检查气缸");
                    }
                    if (m27 == "m27")
                    {
                        LdrLog("右工位矫正气缸复位报警，检查气缸");
                    }

                    if (m30 == "m30")
                    {
                        LdrLog("左工位取料气缸置位报警，检查气缸");
                    }
                    if (m31 == "m31")
                    {
                        LdrLog("左工位取料气缸复位报警，检查气缸");
                    }
                    if (m32 == "m32")
                    {
                        LdrLog("右工位取料气缸置位报警，检查气缸");
                    }
                    if (m33 == "m33")
                    {
                        LdrLog("右工位取料气缸复位报警，检查气缸");
                    }
                    if (m33 == "m34")
                    {
                        LdrLog("气缸有报警，检查气缸");
                    }
                    if (m33 == "m35")
                    {
                        LdrLog("急停报警，检查急停按钮");
                    }
                    if (m36 == "m36")
                    {
                        LdrLog("右侧门报警，检查门");
                    }
                    if (m37 == "m37")
                    {
                        LdrLog("前门报警，检查门");
                    }
                    if (m38 == "m38")
                    {
                        LdrLog("左1门报警，检查门");
                    }
                    if (m39 == "m39")
                    {
                        LdrLog("左2门报警，检查门");
                    }
                    if (m40 == "m40")
                    {
                        LdrLog("后门报警，检查门");
                    }
                    if (m41 == "m41")
                    {
                        LdrLog("上传CT");
                    }
                    if (m42 == "m42")
                    {
                        LdrLog("上传CT");
                    }
                    if (m43 == "m43")
                    {
                        LdrLog("上传CT");
                    }

                    if (m49 == "m49")
                    {
                        LdrLog("下料位料盘已满，请取走料盘");
                    }


                    if (m55 == "m55")
                    {
                        LdrLog("回抓左侧转盘料失败，检查位置偏差");
                    }
                    if (m56 == "m56")
                    {
                        LdrLog("回抓右侧转盘料失败，检查位置偏差");
                    }

                    if (m103 == "m103")
                    {
                        LdrLog("机械手A爪吸料失败，检查位置偏差");
                    }
                    if (m104 == "m104")
                    {
                        LdrLog("机械手B爪吸料失败，检查位置偏差");
                    }
                    if (m105 == "m105")
                    {
                        LdrLog("机械手A爪取样本失败，检查位置偏差");
                    }
                    if (m106 == "m106")
                    {
                        LdrLog("PLC与机械手通信失败，通知电测人员");
                    }
                    if (m117 == "m117")
                    {
                        LdrLog("上料区剥盘失败，检查气缸");
                    }

                }
            }
        }


        public bool InsertAlarm(string SQLValue)//自己定义的  插入sql数据方法
        {
            if (USEOracleCheck == 10)
            {
                return false;
            }
            else
            {

            
            //SQL_Result = SQLClient.test("Server="+ShuJuKuIP+ ";database=" + ShuJuKuFW + ";uid=" + ShuJuKuYH + ";pwd=" + ShuJuKuMM);
            MySqlConnection sqlCon = new MySqlConnection("Server=" + ShuJuKuIP + ";database=" + ShuJuKuFW + ";uid=" + ShuJuKuYH + ";pwd=" + ShuJuKuMM);
            MySqlCommand cmd = new MySqlCommand(SQLValue, sqlCon);
            MySqlDataReader reader = null;
            try
            {
                sqlCon.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            int affected = 0;
            try
            {
                reader = cmd.ExecuteReader();

                affected = reader.RecordsAffected;

                reader.Close();

            }
            catch (Exception ex)
            {
                // LdrLog("上传数据库失败！" + ex.Message);
            }
            try
            {
                sqlCon.Close();
            }
            catch (Exception ex)
            {
                // LdrLog("上传数据库失败！" + ex.Message);
            }

            if (affected == 0)
                return false;
            else
                return true;
            }
        }


        //string baojingXM = "";
        /// <summary>
        /// 保存测试文件   本地路径=D:\CSV\\
        /// </summary>
        public async void SaveTest(string baojingXM)
        {
            Task Task_Save = Task.Run(() =>
            {

                System.IO.DirectoryInfo LocalDir = new System.IO.DirectoryInfo("D:\\Alarm\\");
                try
                {
                    LocalDir.Create();
                }
                catch
                {

                }
                try
                {
                    string mLocalFile = LocalDir.FullName + DateTime.Today.ToString("yyyyMMdd") + ".csv";
                    string mLocalData = DateTime.Now.ToString("HH:mm:ss") + "," + baojingXM;
                    if (!System.IO.File.Exists(mLocalFile))
                    {
                        string mHeader = @"时间,报警项";
                        System.IO.File.WriteAllText(mLocalFile, mHeader);
                    }
                    System.IO.File.AppendAllText(mLocalFile, Environment.NewLine + mLocalData);
                }
                catch
                {

                }
            });
            await Task_Save;
        }

        //登录按钮
        string mdenglu = "nopass";
        private void denglu_Click(object sender, RoutedEventArgs e)
        {
            if (mdenglu == "nopass")
            {
                if (passwordBox.Password == "123456")
                {
                    SQL.IsEnabled = true;
                    mdenglu = "pass";
                    LdrLog("已登录");
                    denglu.Content = "登出";
                    MarkAddress_TB.IsReadOnly = false;
                    Version_TB.IsReadOnly = false;
                    WorkStation_TB.IsReadOnly = false;
                    PN_TB.IsReadOnly = false;
                    FixtureNumber_TB.IsReadOnly = false;
                    ShuJuKuIP_TB.IsReadOnly = false;
                    ShuJuKuFW_TB.IsReadOnly = false;
                    ShuJuKuYH_TB.IsReadOnly = false;
                    ShuJuKuMM_TB.IsReadOnly = false;
                    Button.IsEnabled = true;
                    isAllow.IsEnabled = true;
                }
                else
                {
                    System.Windows.MessageBox.Show("密码错误或未输入密码！！！");
                    return;
                }
            }
            else
            {
                SQL.IsEnabled = false;
                mdenglu = "nopass";
                denglu.Content = "登录";
                passwordBox.Password = "";
                MarkAddress_TB.IsReadOnly = true;
                Version_TB.IsReadOnly = true;
                WorkStation_TB.IsReadOnly = true;
                PN_TB.IsReadOnly = true;
                FixtureNumber_TB.IsReadOnly = true;
                ShuJuKuIP_TB.IsReadOnly = true;
                ShuJuKuFW_TB.IsReadOnly = true;
                ShuJuKuYH_TB.IsReadOnly = true;
                ShuJuKuMM_TB.IsReadOnly = true;
                Button.IsEnabled = false;
                isAllow.IsEnabled = false;
            }
        }

        public string GlobalFile = AppDomain.CurrentDomain.BaseDirectory + "Global.ini";
        public void LoadIni()
        {
            if (File.Exists(GlobalFile))
            {
                mMarkAddress = DXH.Ini.DXHIni.ContentReader("计算机", "物理地址", "", GlobalFile);
                mGZVersion = DXH.Ini.DXHIni.ContentReader("计算机", "故障代码版本", "", GlobalFile);
                mWorkStation = DXH.Ini.DXHIni.ContentReader("计算机", "测试工站", "", GlobalFile);
                mPN = DXH.Ini.DXHIni.ContentReader("计算机", "测试料号", "", GlobalFile);
                mFixtureNumber = DXH.Ini.DXHIni.ContentReader("计算机", "治具编号", "", GlobalFile);
                mDuKaQi = DXH.Ini.DXHIni.ContentReader("计算机", "读卡器串口", "", GlobalFile);

                mShuJuKuIP = DXH.Ini.DXHIni.ContentReader("数据库", "数据库IP", "", GlobalFile);
                mShuJuKuFW = DXH.Ini.DXHIni.ContentReader("数据库", "数据库服务名", "", GlobalFile);
                mShuJuKuYH = DXH.Ini.DXHIni.ContentReader("数据库", "数据库账号", "", GlobalFile);
                mShuJuKuMM = DXH.Ini.DXHIni.ContentReader("数据库", "数据库密码", "", GlobalFile);

                DXH.Ini.DXHIni.TryToBool(ref mORACLE_Enable, DXH.Ini.DXHIni.ContentReader("数据库", "是否启用上传", mORACLE_Enable.ToString(), GlobalFile));
                //ShuJuKuIP.text = mShuJuKuIP;
                //ShuJuKuFW = mShuJuKuFW;
                //ShuJuKuYH = mShuJuKuYH;
                //ShuJuKuMM = mShuJuKuMM;
                MarkAddress_TB.Text = mMarkAddress;
                Version_TB.Text = mGZVersion;
                WorkStation_TB.Text = mWorkStation;
                PN_TB.Text = mPN;
                FixtureNumber_TB.Text = mFixtureNumber;
                ShuJuKuIP_TB.Text = mShuJuKuIP;
                ShuJuKuFW_TB.Text = mShuJuKuFW;
                ShuJuKuYH_TB.Text = mShuJuKuYH;
                ShuJuKuMM_TB.Text = mShuJuKuMM;
            }
            else
            {
                MarkAddress = mMarkAddress;
                ShuJuKuIP = mShuJuKuIP;
                ShuJuKuFW = mShuJuKuFW;
                ShuJuKuYH = mShuJuKuYH;
                ShuJuKuMM = mShuJuKuMM;
                DuKaQi = mDuKaQi;


                ORACLE_Enable = mORACLE_Enable;
            }
        }


        public string mMarkAddress = "111";
        public string MarkAddress
        {
            get { return mMarkAddress; }
            set
            {
                mMarkAddress = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "物理地址", mMarkAddress, GlobalFile);
            }
        }//mGZVersion

        public string mGZVersion = "V01";
        public string GZVersion
        {
            get { return mGZVersion; }
            set
            {
                mGZVersion = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "故障代码版本", mGZVersion, GlobalFile);
            }
        }//mGZVersion
         //测试工站
        public string mWorkStation = "LOADUP";
        public string WorkStation
        {
            get { return mWorkStation; }
            set
            {
                mWorkStation = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "测试工站", mWorkStation, GlobalFile);
            }
        }
        //测试料号
        public string mPN = "LOADUP";
        public string PN
        {
            get { return mPN; }
            set
            {
                mPN = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "测试料号", mPN, GlobalFile);
            }
        }
        //治具编号
        public string mFixtureNumber = "LOADUP";
        public string FixtureNumber
        {
            get { return mFixtureNumber; }
            set
            {
                mFixtureNumber = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "治具编号", mFixtureNumber, GlobalFile);
            }
        }




        public string mShuJuKuIP = "szcsgcdb01.eavarytech.com";
        public string ShuJuKuIP
        {
            get { return mShuJuKuIP; }
            set
            {
                mShuJuKuIP = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("数据库", "数据库IP", mShuJuKuIP, GlobalFile);
            }
        }

        public string mShuJuKuFW = "dcdb";
        public string ShuJuKuFW
        {
            get { return mShuJuKuFW; }
            set
            {
                mShuJuKuFW = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("数据库", "数据库服务名", mShuJuKuFW, GlobalFile);
            }
        }

        public string mShuJuKuYH = "dcu";
        public string ShuJuKuYH
        {
            get { return mShuJuKuYH; }
            set
            {
                mShuJuKuYH = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("数据库", "数据库账号", mShuJuKuYH, GlobalFile);
            }
        }

        public string mShuJuKuMM = "dcudata";
        public string ShuJuKuMM
        {
            get { return mShuJuKuMM; }
            set
            {
                mShuJuKuMM = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("数据库", "数据库密码", mShuJuKuMM, GlobalFile);
            }
        }

        public string mDuKaQi= "COM1";
        public string DuKaQi
        {
            get { return mDuKaQi; }
            set
            {
                mDuKaQi = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "读卡器串口", mDuKaQi, GlobalFile);
            }
        }

        public bool mORACLE_Enable = false;
        public  bool ORACLE_Enable
        {
            get { return mORACLE_Enable; }
            set
            {
                mORACLE_Enable = value;
                DXH.Ini.DXHIni.WritePrivateProfileString("数据库", "是否启用上传", mORACLE_Enable.ToString(), GlobalFile);
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "物理地址", MarkAddress_TB.Text, GlobalFile);
            DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "故障代码版本", Version_TB.Text, GlobalFile);
            DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "测试工站", WorkStation_TB.Text, GlobalFile);
            DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "测试料号", PN_TB.Text, GlobalFile);
            DXH.Ini.DXHIni.WritePrivateProfileString("计算机", "治具编号", FixtureNumber_TB.Text, GlobalFile);



            DXH.Ini.DXHIni.WritePrivateProfileString("数据库", "数据库IP", ShuJuKuIP_TB.Text, GlobalFile);
            DXH.Ini.DXHIni.WritePrivateProfileString("数据库", "数据库服务名", ShuJuKuFW_TB.Text, GlobalFile);
            DXH.Ini.DXHIni.WritePrivateProfileString("数据库", "数据库账号", ShuJuKuYH_TB.Text, GlobalFile);
            DXH.Ini.DXHIni.WritePrivateProfileString("数据库", "数据库密码", ShuJuKuMM_TB.Text, GlobalFile);


            LdrLog("保存成功");

        }

        public int c1 = 0;
        public int c2 = 0;
        public int c3 = 0;

        private void button1_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                OperateResult<short[]> read2 = PLCManager.melsec_net.ReadInt16("D60", 3);
                if (read2.IsSuccess)
                {
                    //DFx5u1 = read1.Content;
                    DFx5u2 = read2.Content;


                    b1 = Convert.ToString(DFx5u2[0], 2);
                    b2 = Convert.ToString(DFx5u2[1], 2);
                    b3 = Convert.ToString(DFx5u2[2], 2);

                    c1 = Convert.ToInt32(b1, 2);
                    c2 = Convert.ToInt32(b2, 2);
                    c3 = Convert.ToInt32(b3, 2);

                    LdrLog("读取成功" + "D60为：" + c1.ToString() + "  D61为：" + c2.ToString() + "  D62为：" + c3.ToString());
                }
                else
                {
                    LdrLog("读取失败");
                    c1 = 0;
                    c2 = 0;
                    c3 = 0;
                }
            }
            catch
            {

                LdrLog("读取失败");
                c1 = 0;
                c2 = 0;
                c3 = 0;
            }



            string nn = c1.ToString() + "S:" + c2.ToString() + "S:" + c3.ToString() + "S";
            string SQLValue = "insert into TED_WARN_DATA（WORKSTATION,PARTNUM,MACID,LOADID,PETID,TDATE,TTIME,CLASS,WARNID,DETAILID,WARNNUM,FL01,FL02,FL03,FL04,FL05,FL06,FL07,FL08,FL09,FL10,SUPPLIER,WARNVER) value('" + WorkStation + "','" + PN + "','" + FixtureNumber + "','" + MarkAddress + "','','" + DateTime.Now.ToString("yyyyMMdd") + "','" + DateTime.Now.ToString("HHmmss") + "','" + Banci + "','W-LDR-45','','1','','','','','','','','','','" + nn + "','LDR','" + GZVersion + "')";

            try
            {

                bool a = InsertAlarm(SQLValue);
                if (a)
                {
                    LdrLog("测试上传成功");
                }
                else
                {
                    LdrLog("测试上传失败");
                }

            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); };
        }


        static int USEOracleCheck = 1;
        private void isAllow_Checked(object sender, RoutedEventArgs e)
        {
            ORACLE_Enable = true;
            USEOracleCheck = 1;
            LdrLog("启用上传");
        }

        private void isAllow_Unchecked(object sender, RoutedEventArgs e)
        {
            ORACLE_Enable = false;
            USEOracleCheck = 10;
            LdrLog("屏蔽上传");
        }

        //private void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    starttime = false;
        //    Thread.Sleep(50);
        //  //  TimerDelete_Tick2();
        //   // starttime = true;
        //    Timecount = 60 * 600;
        //}

        //private void Button_Click_2(object sender, RoutedEventArgs e)
        //{
        //    Timecount = 0;
        //    starttime = false;
            
        //}
    }
}
