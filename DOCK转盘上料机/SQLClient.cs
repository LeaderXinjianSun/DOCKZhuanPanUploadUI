using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOCK转盘上料机
{
    //MYSQL数据库
    public static class SQLClient
    {
        static MySqlConnection mysql;


        public static event EventHandler<bool> SQLStateChanged;
        static bool mConnect;
        public static bool Connect
        {
            get { return mConnect; }
            set
            {
                if (mConnect != value)
                {
                    mConnect = value;
                    if (SQLStateChanged != null)
                        SQLStateChanged(null, mConnect);
                }

            }
        }

        public static string test(string mConnectString)
        {

            string result = "";
            try
            {
                mysql = new MySqlConnection(mConnectString);

                mysql.Open();
                Connect = true;
                //Console.WriteLine(SQLServerConnect.State.ToString());
                result = "数据库检测：连接正常！";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                Connect = false;
                result = "数据库检测：连接异常！";
            }
            finally
            {
                mysql.Close();
            }
            return result;
        }

      
    }
}
