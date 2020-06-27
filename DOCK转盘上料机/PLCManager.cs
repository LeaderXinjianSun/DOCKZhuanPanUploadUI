using HslCommunication;
using HslCommunication.Profinet.Melsec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOCK转盘上料机
{
    public static class PLCManager
    {
        public static MelsecMcNet melsec_net;//三菱PLC FX5U

        public static event EventHandler<bool> PLCStateChanged;
        static bool mConnect;
        public static bool Connect
        {
            get { return mConnect; }
            set
            {
                if (mConnect != value)
                {
                    mConnect = value;
                    if (PLCStateChanged != null)
                        PLCStateChanged(null, mConnect);
                }

            }
        }


        public static bool[] FX5uOut { get; set; }
        public static bool[] FX5uIn { get; set; }



        public static void Initialize()
        {
            FX5uOut = new bool[32];
            FX5uIn = new bool[32];
            melsec_net = new MelsecMcNet("192.168.3.250", 3000);   //melsec_net = new MelsecMcNet("192.168.3.250", 3000);
            melsec_net.ConnectTimeOut = 2000;
            melsec_net.ReceiveTimeOut = 100;
            OperateResult connect = melsec_net.ConnectServer();
            if (connect.IsSuccess)
            {
                Connect = true;
                Console.WriteLine("连接成功");
            }
            else
            {
                Connect = false;
                Console.WriteLine("连接失败");
            }
        }

        public static async void RUN()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(50);
                Task Task_SQLSTA = Task.Run(() =>
                {
                    try
                    {
                        OperateResult<bool[]> read = melsec_net.ReadBool("M1750", 32);
                        if (read.IsSuccess)
                        {
                            FX5uOut = read.Content;
                            Connect = true;
                        }
                        else
                        {
                            Connect = false;
                        }
                        if (FX5uIn != null)
                        {
                            OperateResult write = melsec_net.Write("M1700", FX5uIn);
                            if (write.IsSuccess)
                            {
                                Connect = true;
                            }
                            else
                            {
                                Connect = false;
                            }
                        }

                    }
                    catch { }

                });
                await Task_SQLSTA;
            }
        }







        ////PLCM点上升沿的时候插入数据库
        //static bool mPLCToMySQL = false;
        //static string PreRsedStr = "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        //public async static void StartPLCToMySQL()
        //{
        //    if (!mPLCToMySQL)
        //        mPLCToMySQL = true;
        //    else
        //        return;
        //    await Task.Delay(300);
        //    string CurRsedStr = "";
        //    while (mPLCToMySQL)
        //    {
        //        System.Threading.Thread.Sleep(300);
        //        OperateResult<bool[]> PLCTOMySQLread = melsec_net.ReadBool("M2600", 100);
        //        if (PLCTOMySQLread.IsSuccess)
        //        {
        //            bool[] FX5uOutTOMySQL = PLCTOMySQLread.Content;

        //            for (int i = 0; i < 100; i++)
        //            {
        //                CurRsedStr += FX5uOutTOMySQL[i] ? "1" : "0";//PLC读出的数据，如果有变化，就写入数据库
        //            }
        //            for (int i = 0; i < 100; i++)
        //            {
        //                if (PreRsedStr.Substring(i, 1) == "0" && CurRsedStr.Substring(i, 1) == "1")//上升沿
        //                {

        //                }
        //            }
        //            PreRsedStr = CurRsedStr;
        //        }

        //    }
        //}






















    }
}
