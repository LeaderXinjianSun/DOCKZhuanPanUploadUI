using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BingLibrary.hjb.net;

namespace DOCK转盘上料机
{
    public static class RobotManager
    {
        public static bool[] RobotOUT { get; set; }
        public static bool[] RobotIN { get; set; }
        static bool mConnect;
        public static bool Connect {
            get { return mConnect; }
            set
            {
                if (mConnect != value)
                {
                    mConnect = value;
                    RobotStateChanged?.Invoke(null, mConnect);
                }
            }
            }
        public static TcpIpClient IOReceiveNet;
        public delegate void PrintEventHandler(string ModelMessageStr);
        public static event PrintEventHandler ModelPrint;
        public static event EventHandler<bool> RobotStateChanged;
        public static void Initialize()
        {
            RobotOUT = new bool[32];
            RobotIN = new bool[32];
            IOReceiveNet = new TcpIpClient();
            checkIOReceiveNet();
            IORevAnalysis();
        }
        public static async void checkIOReceiveNet()
        {
            while (true)
            {
                await Task.Delay(400);
                if (!IOReceiveNet.tcpConnected)
                {
                    await Task.Delay(1000);
                    if (!IOReceiveNet.tcpConnected)
                    {
                        bool r1 = await IOReceiveNet.Connect("192.168.3.100", 2000);
                        if (r1)
                        {
                            Connect = true;
                            //ModelPrint("机械手IOReceiveNet连接");
                        }
                        else
                            Connect = false;
                    }
                }
                else
                { await Task.Delay(15000); }
            }
        }
        private static async void IORevAnalysis()
        {
            while (true)
            {
                if (Connect == true)
                {
                    string s = await IOReceiveNet.ReceiveAsync();

                    string[] ss = s.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    try
                    {
                        s = ss[0];

                    }
                    catch
                    {
                        s = "error";
                    }

                    if (s == "error")
                    {
                        IOReceiveNet.tcpConnected = false;
                        Connect = false;
                        //ModelPrint("机械手IOReceiveNet断开");
                    }
                    else
                    {
                        string[] strs = s.Split(',');
                        if (strs[0] == "IOCMD" && strs[1].Length == 32)
                        {
                            for (int i = 0; i < 32; i++)
                            {
                                RobotOUT[i] = strs[1][i] == '1' ? true : false;
                            }
                            try
                            {
                                string RsedStr = "";
                                for (int i = 0; i < 32; i++)
                                {
                                    RsedStr += RobotIN[i] ? "1" : "0";
                                }
                                await IOReceiveNet.SendAsync(RsedStr);
                            }
                            catch { }

                        }
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }
    }
}
/*
'发送消息到上位机
Function TcpIpIOSend
	Integer chknet4, errTask, i
	OpenNet #208 As Server
	Print "端口208打开"
	WaitNet #208
	Print "端口208连接"
	String SendString$
	String RevString$
	
	Do
		OnErr GoTo NetErr
		chknet4 = ChkNet(208)
		If chknet4 >= 0 Then
			SendString$ = "IOCMD,"
			For i = 0 To 99
				If MemSw(200 + i) = 1 Then
					SendString$ = SendString$ + "1"
				Else
					SendString$ = SendString$ + "0"
				
				EndIf
			Next
			Print #208, SendString$
			Input #208, RevString$
'			Print RevString$
'			Print Len(RevString$)
			If Len(RevString$) = 100 Then
				
				For i = 1 To 100
					If Mid$(RevString$, i, 1) = "1" Then
						MemOn 100 + i - 1
					Else
						MemOff 100 + i - 1
					EndIf
				Next
				
			EndIf

		Else
			CloseNet #208
			Print "端口208关闭"
			Wait 0.1
			OpenNet #208 As Server
			Print "端口208重新打开"
			WaitNet #208
			Print "端口208重新连接"
		EndIf
	Loop
	 
	NetErr:
		Print "The Error code is ", Err
		Print "The Error Message is ", ErrMsg$(Err, LANGID_SIMPLIFIED_CHINESE)
		errTask = Ert
		If errTask > 0 Then
			Print "Task number in which error occurred is ", errTask
			Print "The line where the error occurred is Line ", Erl(errTask)
			If Era(errTask) > 0 Then
				Print "Joint which caused the error is ", Era(errTask)
			EndIf
		EndIf
		EResume Next
Fend
 */
