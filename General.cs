using System;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Net.Mail;
using System.Net;
using System.Threading;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;


namespace PIS_Engine
{
    public static class Extenstion
    {
        public static int CustomIndexOf(this string source, char toFind, int position)
        {
            int index = -1;
            for (int i = 0; i < position; i++)
            {
                index = source.IndexOf(toFind, index + 1);

                if (index == -1)
                    break;
            }

            return index;
        }
    }


    public  class General
    {
        //// this is the main connection string 

        //public static readonly string connectionString = "Data Source=45.113.189.23;Initial Catalog=newtrack;User ID=newtrack;Password=55hD&44m7E3jnd; Max Pool Size=32767;";
        //public static readonly string connectionString = "Data Source=192.168.23.131,15433;Initial Catalog=atltracking;User ID=newtrack;Password=55hD&44m7E3jnd; Max Pool Size=32767;";
        public static readonly string connectionString = "Data Source=103.108.12.184,15433;Initial Catalog=atltracking;User ID=newtrack;Password=55hD&44m7E3jnd; Max Pool Size=32767;";

        public static readonly string MySqlConnectionString = "Data Source=49.50.68.155;Initial Catalog=e_ticket;User ID=newtrack_test;Password=qwert@123; Maximum Pool Size=5000";

        private static readonly string logFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Todays_Logs"); // Single folder
        private static readonly string logFilePath = Path.Combine(logFolderPath, "ApplicationLog.txt"); // Single file

        public  void DML(string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                      //  connection.Open();
                        cmd.CommandTimeout = 100000;
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch { }
                        finally { connection.Close(); }
                      //  Console.WriteLine("data inserted");
                    }
                }
            }
            catch (SqlException se)
            {
                if (se.ErrorCode != -2146232060)
                {
                    throw se;
                }


            }
        }
        public static void my_SendNotification(string msg, int user_id)
        {
            // EtaPredict p = new EtaPredict();


            string address = "https://fcm.googleapis.com/fcm/send";
            DataTable dt = null;


            try
            {

                dt = General.SelectQuery("select auid,iuid from user_did where sys_user_id=" + user_id);
                if (dt != null)
                {
                    if (dt.Rows.Count != 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            using (WebClient wc = new WebClient())
                            {
                                wc.Headers.Add("Content-Type", "application/json");
                                wc.Headers.Add("Authorization", "key=AAAAT3DETk8:APA91bF6KEzkfSZPWohBFj1eCct1U3JlbtWulHxNFqjmw9n75TFYsI3dpogkCsoUkndJvo9nN8WfGngbKw7Yw4puzdX_KX3tHmi3-ShrWHEsgsRyDo6hnU_zQeLiZS9hLJaTosmwm-MT");
                                if (!string.IsNullOrEmpty(dr["auid"].ToString()))
                                {
                                    try
                                    {
                                        wc.UploadString(address, "{\"to\":\"" + dr["auid"].ToString().Trim() + "\",\"data\": {\"abctraq\": \"" + msg + "\"}}");

                                    }
                                    catch (Exception e)
                                    {
                                        General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
                                    }


                                }
                                if (!string.IsNullOrEmpty(dr["iuid"].ToString()))
                                {
                                    try
                                    {

                                        // string s = wc.UploadString(address, "{\"to\":\"" + dr["iuid"].ToString().Trim() + "\",\"notification\": {\"body\": \"" + msg + "\"}}");
                                        wc.UploadString(address, "{\"to\":\"" + dr["iuid"].ToString().Trim() + "\",\"notification\": {\"body\": \"" + msg + "\",\"priority\" : \"high\",\"sound\": \"default\"}}");


                                    }
                                    catch (Exception e)
                                    {
                                        General.WriteToLogFile(e.Message, "E:\\shubham\\Services\\bin\\Error", "notification.txt");
                                    }


                                }

                            }
                        }
                        // DML("insert into alert_notification_log(alert_setting_id,message,sent_on,gps_latitude,gps_longitude) values (" + alert_setting_id + ",'" + msg + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + lat + "," + lng + ")");

                    }
                }

            }
            catch (Exception e)
            {
                General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "Notification_Exp.txt");
            }

        }

        public static string AsciiToHex(string str)
        {
            
            char[] charValues = str.ToCharArray();
            string hexOutput = "";
            foreach (char _eachChar in charValues)
            {
                // Get the integral value of the character.
                int value = Convert.ToInt32(_eachChar);
                // Convert the decimal value to a hexadecimal value in string form.
                hexOutput += String.Format("{0:X}", value);
                
            }
            return hexOutput;
        }
        public static void DML(string query, string userName)
        {
            string connString = "Data Source=49.50.68.213;Initial Catalog=newtrack_" + userName + "; User ID = newtrack; Password=qwert@123; Max Pool Size=32767";
            try
            {
                using (SqlConnection connection = new SqlConnection(connString))
                {

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        if (connection.State  == ConnectionState.Closed)
                        {
                            connection.Open();
                        }

                        cmd.CommandTimeout = 100000;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException se)
            {
                General.WriteToLogFile(se.Message, AppDomain.CurrentDomain.BaseDirectory, "db.txt");
            }
        }

        public static DataTable SelectQuery(string query, string userName = "")
        {
            string connString = "";

            if (string.IsNullOrEmpty(userName))
                connString = "Data Source=49.50.68.213;Initial Catalog=newtrack;User ID=newtrack;Password=qwert@123; Max Pool Size=32767";
            else
                connString = "Data Source=49.50.68.213;Initial Catalog=newtrack_" + userName + "; User ID = newtrack; Password=qwert@123; Max Pool Size=32767";

            using (SqlConnection conn = new SqlConnection(connString))
            {
               // conn.Open();
                SqlCommand command = new SqlCommand(query, conn);
                command.CommandTimeout = 5000000;
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                adapter.Dispose();
                return dt;
            }
        }

        public static DataTable SelectQuery(string query)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        //  conn.Open();
                        SqlCommand command = new SqlCommand(query, conn);
                        // command.CommandTimeout = 5000000;
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        adapter.Dispose();
                        return dt;
                    }
                    catch (Exception e)
                    {

                        General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "db1.txt");
                        return null;
                    }
                    finally
                    {
                        conn.Dispose();

                    }
                }
            }
            catch (Exception e)
            {

                General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "db1.txt");
                return null;

            }
        }

        public static DataTable SelectQuery(string query,bool isMySQL)
        {
            using (MySqlConnection conn = new MySqlConnection(MySqlConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand command = new MySqlCommand(query, conn);
                    //command.CommandTimeout = 5000000;
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
                catch (Exception e)
                {
                    General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "db.txt");
                    return null;

                }
            }
        }

        public static StringBuilder htmlcreation(DataTable dt2)
        {
            string lasttime = dt2.Rows[0][0].ToString();
            string Lat = dt2.Rows[0][1].ToString();
            string Long = dt2.Rows[0][2].ToString();
            string Imei = dt2.Rows[0][3].ToString();
            string vehreg = dt2.Rows[0][4].ToString();
            string Mainpower = dt2.Rows[0][5].ToString();
            string Ignition = dt2.Rows[0][6].ToString();
            string battery_status = dt2.Rows[0][7].ToString();
            string signalStrength = dt2.Rows[0][8].ToString();

            if (battery_status == null || battery_status == "")
            {
                battery_status = "not found";

            }

            //$"Dear Customer,\n \n  your vehicle  {vehreg } \n(Imei {Imei }) is off \n since {lasttime} (time{15} minutes) ,latitude = {Lat} ,longitude= {Long} MainPower = {Mainpower},Ignition ={Ignition}";

            StringBuilder strHTMLBuilder = new StringBuilder();

            strHTMLBuilder.Append("<html>");
            strHTMLBuilder.Append("<head>");
            strHTMLBuilder.Append("</head>");
            strHTMLBuilder.Append("<body>");
            strHTMLBuilder.Append("<div>");
            strHTMLBuilder.Append("<b>Dear  Customer ,</b>");
            strHTMLBuilder.Append("<br/>");
            strHTMLBuilder.Append("<br/>");


            strHTMLBuilder.Append("<b> Device Off Email  </b>");
            //strHTMLBuilder.Append("</br> demo <br/>");
            strHTMLBuilder.Append("<br/>");
            strHTMLBuilder.Append("<br/>");
            strHTMLBuilder.Append("<br/>");
            // strHTMLBuilder.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:arial'>");

            strHTMLBuilder.Append($" your vehicle {vehreg} (Imei {Imei}) is off since {lasttime}");
            strHTMLBuilder.Append("<br/>");
            strHTMLBuilder.Append($"Latitude = {Lat}");
            strHTMLBuilder.Append($"Longitude= {Long}");

            strHTMLBuilder.Append("<br/>");
            strHTMLBuilder.Append($"MainPower = {Mainpower}");
            strHTMLBuilder.Append($"Ignition = {Ignition}");

            strHTMLBuilder.Append($"MainPower = {Mainpower}");
            strHTMLBuilder.Append($"Ignition = {Ignition}");

            strHTMLBuilder.Append($"Battery status = {battery_status}");
            strHTMLBuilder.Append($"Signal Strength = {signalStrength}");
            strHTMLBuilder.Append("</div>");
            strHTMLBuilder.Append("</body>");
            strHTMLBuilder.Append("</html>");


            return strHTMLBuilder;
        }

        public static void DML(string query, SqlParameter[] param)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        try
                        {
                            connection.Open();
                            //cmd.CommandTimeout = 100000;
                            cmd.Parameters.AddRange(param);
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {

                            General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "db.txt");
                           // return null;

                        }

                    }
                }
            }
            catch (SqlException se)
            {
                if (se.ErrorCode != -2147467259)
                {
                    throw se;
                }

            }
        }

        public static void DML(string query, bool isMySql)
        {

            try
            {
                using (MySqlConnection connection = new MySqlConnection(MySqlConnectionString))
                {

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {

                        connection.Open();
                        cmd.CommandTimeout = 100000;
                        cmd.ExecuteNonQuery();

                    }
                }
            }
            catch (MySqlException se)
            {
                if (se.ErrorCode != -2147467259)
                {
                    throw se;
                }

            }

        }

        public static int DML(string query, SqlCommand cmd, SqlTransaction trans)
        {

            try
            {

                     cmd.CommandText = query;
                    cmd.Transaction = trans;
                    cmd.CommandTimeout = 100000;
                   return cmd.ExecuteNonQuery();


                
            }
            catch (SqlException se)
            {
                if (se.ErrorCode != -2147467259)
                {
                    throw se;
                }

            }
            return 0;
        }

        public static bool IsLatest(string serviceId, DateTime gpsTime)
        {
            DataTable dt = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {

                SqlCommand command = new SqlCommand("select max(gps_time) from  latest_telemetry with(nolock) where sys_service_id = '" + serviceId + "'", connection);
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dt);
                if (dt != null)
                    if (dt.Rows.Count > 0)
                    {
                        if (Convert.ToDateTime(dt.Rows[0][0]) < gpsTime && gpsTime < DateTime.UtcNow)
                            return true;
                    }
            }
            return false;
        }

        public static string GetServiceID(string IMEINumber, string receivedPacket)
        {

            string str = "0";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {

                    connection.Open();
                    SqlDataAdapter SqlDataAdapter = new SqlDataAdapter("select services.id,imei from devices inner join services on devices. id = services.sys_device_id where imei = '" + IMEINumber + "'", connection);
                    DataSet dataSet = new DataSet();
                    ((DataAdapter)SqlDataAdapter).Fill(dataSet);
                    DataTable dataTable = dataSet.Tables[0];
                    if (dataTable.Rows.Count > 0)
                        str = dataTable.Rows[0][0].ToString();
                    //else
                    //WriteToLogFile(IMEINumber,
                    //Logger.Log(FolderPath + "\\InvalidServiceID.txt", IMEINumber);
                    dataTable.Dispose();
                    dataSet.Dispose();
                    SqlDataAdapter.Dispose();
                }

            }
            catch (Exception ex)
            {

                throw ex;

            }

            return str;
        }

        public static string SetDeviceId(string imei)
        {
            string str = "0";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlDataAdapter SqlDataAdapter = new SqlDataAdapter(@"select s.id, d.device_id from services s, devices d where s.sys_device_id=d.id and d.imei='" + imei + "'",conn);
                
                DataSet dataSet = new DataSet();
                ((DataAdapter)SqlDataAdapter).Fill(dataSet);
                DataTable dataTable = dataSet.Tables[0];
                if (dataTable.Rows.Count > 0)
                {
                    str = dataTable.Rows[0][0].ToString();
                    //if (string.IsNullOrEmpty(dataTable.Rows[0][0]))
                }
                dataTable.Dispose();
                dataSet.Dispose();
                SqlDataAdapter.Dispose();
            }
            return str;
        }

        public static string GetServiceID(string IMEINumber, bool isMySql)
        {

            string str = "0";
            try
            {
                if (isMySql)
                {
                    using (MySqlConnection connection = new MySqlConnection(MySqlConnectionString))
                    {

                        connection.Open();
                        MySqlDataAdapter SqlDataAdapter = new MySqlDataAdapter("select services.id,imei from devices inner join services on devices. id = services.sys_device_id where imei = '" + IMEINumber + "'", connection);
                        DataSet dataSet = new DataSet();
                        ((DataAdapter)SqlDataAdapter).Fill(dataSet);
                        DataTable dataTable = dataSet.Tables[0];
                        if (dataTable.Rows.Count > 0)
                            str = dataTable.Rows[0][0].ToString();
                        
                        dataTable.Dispose();
                        dataSet.Dispose();
                        SqlDataAdapter.Dispose();
                    }
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {

                        connection.Open();
                        SqlDataAdapter SqlDataAdapter = new SqlDataAdapter("select services.id,imei from devices inner join services on devices. id = services.sys_device_id where imei = '" + IMEINumber + "'", connection);
                        DataSet dataSet = new DataSet();
                        ((DataAdapter)SqlDataAdapter).Fill(dataSet);
                        DataTable dataTable = dataSet.Tables[0];
                        if (dataTable.Rows.Count > 0)
                            str = dataTable.Rows[0][0].ToString();
                        
                        dataTable.Dispose();
                        dataSet.Dispose();
                        SqlDataAdapter.Dispose();
                    }
                }


            }
            catch (Exception ex)
            {
                throw ex;

            }

            return str;
        }

        public static string GetServiceID(string IMEINumber, bool isMySql, string userName)
        {
            string connString = "Data Source=49.50.68.213;Initial Catalog=newtrack_" + userName + "; User ID = newtrack; Password=qwert@123; Max Pool Size=32767";
            string str = "0";
            try
            {
                if (isMySql)
                {
                    using (MySqlConnection connection = new MySqlConnection(connString))
                    {

                        connection.Open();
                        MySqlDataAdapter SqlDataAdapter = new MySqlDataAdapter("select services.id,imei from devices inner join services on devices. id = services.sys_device_id where imei = '" + IMEINumber + "'", connection);
                        DataSet dataSet = new DataSet();
                        ((DataAdapter)SqlDataAdapter).Fill(dataSet);
                        DataTable dataTable = dataSet.Tables[0];
                        if (dataTable.Rows.Count > 0)
                            str = dataTable.Rows[0][0].ToString();
                     
                        dataTable.Dispose();
                        dataSet.Dispose();
                        SqlDataAdapter.Dispose();
                    }
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(connString))
                    {

                        connection.Open();
                        SqlDataAdapter SqlDataAdapter = new SqlDataAdapter("select services.id,imei from devices inner join services on devices. id = services.sys_device_id where imei = '" + IMEINumber + "'", connection);
                        DataSet dataSet = new DataSet();
                        ((DataAdapter)SqlDataAdapter).Fill(dataSet);
                        DataTable dataTable = dataSet.Tables[0];
                        if (dataTable.Rows.Count > 0)
                            str = dataTable.Rows[0][0].ToString();
                        
                        dataTable.Dispose();
                        dataSet.Dispose();
                        SqlDataAdapter.Dispose();
                    }
                }


            }
            catch (Exception ex)
            {
                throw ex;

            }

            return str;
        }

        public static double GetLatitude(string latitude)
        {
            if (latitude.Length <= 4)
                return 0.0;
            string[] strArray = latitude.Split('.');
            double num = Convert.ToDouble(strArray[0].Substring(strArray[0].Length - 2) + "." + strArray[1]) / 60.0;
            return Convert.ToDouble(strArray[0].Substring(0, strArray[0].Length - 2)) + num;
        }

        public static double GetLongitude(string longitude)
        {
            if (longitude.Length <= 4)
                return 0.0;
            string[] strArray = longitude.Split('.');
            double num = Convert.ToDouble(strArray[0].Substring(strArray[0].Length - 2) + "." + strArray[1]) / 60.0;
            return Convert.ToDouble(strArray[0].Substring(0, strArray[0].Length - 2)) + num;
        }

        public static DateTime GetGPSDateTime(string dateStamp, string timeStamp)
        {
            try
            {
                return new DateTime(Convert.ToInt32("20" + dateStamp.Substring(0, 2)), Convert.ToInt32(dateStamp.Substring(2, 2)),
                                    Convert.ToInt32(dateStamp.Substring(4, 2)), Convert.ToInt32(timeStamp.Substring(0, 2)),
                                    Convert.ToInt32(timeStamp.Substring(2, 2)), Convert.ToInt32(timeStamp.Substring(4, 2)));
            }
            catch (Exception)
            {

                return new DateTime();
            }
        }

        public static DateTime GetGPSDateTimeWiFi(string dateStamp, string timeStamp)
        {
            try
            {
                return new DateTime(Convert.ToInt32(dateStamp.Substring(4, 4)), Convert.ToInt32(dateStamp.Substring(2, 2)),
                                    Convert.ToInt32(dateStamp.Substring(0, 2)), Convert.ToInt32(timeStamp.Substring(0, 2)),
                                    Convert.ToInt32(timeStamp.Substring(2, 2)), Convert.ToInt32(timeStamp.Substring(4, 2)));
            }
            catch (Exception)
            {
                return new DateTime();
            }
        }

        public static DateTime GetGPSDateTimeForTaxiMeter(string dateStamp, string timeStamp)
        {
            try
            {
                return new DateTime(Convert.ToInt32("20" + dateStamp.Substring(6, 2)), Convert.ToInt32(dateStamp.Substring(2, 2)),
                                    Convert.ToInt32(dateStamp.Substring(0, 2)), Convert.ToInt32(timeStamp.Substring(0, 2)),
                                    Convert.ToInt32(timeStamp.Substring(2, 2)), Convert.ToInt32(timeStamp.Substring(4, 2)));
            }
            catch (Exception )
            {
                return new DateTime();
            }
        }

        public static string GetProtocol(string content)
        {
            content = content.Trim();
            if (content.Contains("DIMTS"))
                return "DIMTS";
            if (content.Contains("ATL") && content.Contains("$GPRMC"))
                return "ATLMaster";
            if ((content.IndexOf('$') == 16 && content.IndexOf('#') == -1) || content.Contains("$GPGGA"))
                return "AtlantaOldRoboG";
            if (content.IndexOf('$') == 16)
                return "AtlantaOld";
            if (content.IndexOf('$') == 0)
                return "TaxiMeter";
            if (content.IndexOf('$') == 23)
                return "AKS";
            if (content.LastIndexOf('@') >= 0)
                return "Wtrack";
            if (content.IndexOf("ATLIMG") >= 0)
                return "Camera";

            return null;

        }
        public delegate T RetryOpenDelegate<T>();

        public static T RetryOpen<T>(RetryOpenDelegate<T> action)
        {

            while (true)
            {

                try
                {

                    return action();

                }

                catch (IOException)
                {

                    System.Threading.Thread.Sleep(50);

                }

            }

        }
        public static void WriteToLogFile2(string route_name, int user_id, string msg)
        {
            string now = DateTime.Now.ToString("MM-dd-yyyy");

            bool exists = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/" + now);

            if (!exists)
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/" + now);
            }

            exists = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id);

            if (!exists)
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id);
            }


            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                fs = RetryOpen<FileStream>(delegate () { return new FileStream(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id + "/" + route_name + ".txt", FileMode.Append, FileAccess.Write); });
                sw = RetryOpen<StreamWriter>(delegate () { return new StreamWriter(fs); });
                msg += " -------------- " + DateTime.Now.ToString("HH:mm:ss");
                sw.WriteLine(msg);
                sw.WriteLine();
            }
            catch { }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    fs.Close();
                }
            }
        }
        public static void WriteToLogFile3(string route_name, int user_id, string msg)
        {
            string now = DateTime.Now.ToString("dd-MM-yyyy");

            bool exists = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/" + now);

            if (!exists)
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/" + now);
            }

            exists = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id);

            if (!exists)
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id);
            }


            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                fs = RetryOpen<FileStream>(delegate () { return new FileStream(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id + "/" + route_name + ".txt", FileMode.Append, FileAccess.Write); });
                sw = RetryOpen<StreamWriter>(delegate () { return new StreamWriter(fs); });
                msg += " -------------- " + DateTime.Now.ToString("HH:mm:ss");
                sw.WriteLine(msg);
                sw.WriteLine();
            }
            catch { }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    fs.Close();
                }
            }
        }
        public static void WriteToLogFile1(string route_name, int user_id, string msg)
        {
            string now = DateTime.Now.ToString("dd-MM-yyyy");
           
            bool exists = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/" + now);

            if (!exists)
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/" + now);
            }

            exists = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id);

            if (!exists)
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id);
            }


            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                fs = RetryOpen<FileStream>(delegate () { return new FileStream(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id + "/" + route_name + ".txt", FileMode.Append, FileAccess.Write); });
                sw = RetryOpen<StreamWriter>(delegate () { return new StreamWriter(fs); });
                msg += " -------------- " + DateTime.Now.ToString("HH:mm:ss");
                sw.WriteLine(msg);
                sw.WriteLine();
            }
            catch { }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    fs.Close();
                }
            }
        }


        public static void WriteToLogFile10(string route_name, int user_id, string msg)
        {
            try
            {
                // Ensure the log folder exists
                if (!Directory.Exists(logFolderPath))
                {
                    Directory.CreateDirectory(logFolderPath);
                }

                // Format the log message
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UserID: {user_id}, Route: {route_name} - {msg}";

                // Write to a single log file
                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    sw.WriteLine(logEntry);
                    sw.WriteLine(new string('-', 80)); // Separator for readability
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Log writing failed: {ex.Message}");
            }
        }


        public static void WriteToLogFile(string msg, string folderPath, string logFile, bool isAppend = true, bool isDateTime = true)
        {

     

            string dir = folderPath + "\\" + DateTime.Now.ToString("ddMMyyyy");

            if (!Directory.Exists(dir))

                Directory.CreateDirectory(dir);

            if (!Directory.Exists(dir + "\\ReceivedData"))
                Directory.CreateDirectory(dir + "\\ReceivedData");

            if (!Directory.Exists(dir + "\\WrongProtocol"))
                Directory.CreateDirectory(dir + "\\WrongProtocol");


            string LogFile = dir + "\\" + logFile;


            TextWriter tw = null;

            try
            {

                // create a writer and open the file

                tw = RetryOpen<StreamWriter>(delegate ()
                {

                    return new StreamWriter(LogFile, isAppend);



                });


                // write a line of text to the file
                if (isDateTime)
                    tw.WriteLine(DateTime.Now + Environment.NewLine + msg);
                else
                    tw.WriteLine(msg);

            }

            catch(Exception ) {
            }

            finally
            {

                // close the stream

                if (tw != null)
                {

                    tw.Close();

                    tw.Dispose();

                }

            }

        }

        public static void WriteToLogFile(string route_name, int user_id, string msg)
        {
            string now = DateTime.Now.ToString("ddMMyy");
            bool exists = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/" + now);

            if (!exists)
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/" + now);
            }

            exists = Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id);

            if (!exists)
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id);
            }


            FileStream fs = null;
            StreamWriter sw = null;
            
            try
            {
                fs = RetryOpen<FileStream>(delegate() { return new FileStream(AppDomain.CurrentDomain.BaseDirectory + "/" + now + "/" + user_id + "/" + route_name + ".txt", FileMode.Append, FileAccess.Write); });
                sw = RetryOpen<StreamWriter>(delegate() { return new StreamWriter(fs); });
                msg += " -------------- " + DateTime.Now.ToString("HH:mm:ss");
                sw.WriteLine(msg);
                sw.WriteLine();
            }
            catch { }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    fs.Close();
                }
            }
        }

        public static void SendSms(string message, string mobileNo)
        {
            string receivedData = string.Empty;

            try
            {
                //string msgSenderUrl = "http://bulksms.a-tracker.com/sendsms.php?UserId=abctraq&Pwd=abctraq&Mobileno=" + mobileNo + "&Msg=" + message + "&SenderId=CellApps";
                message = message.Replace("(", "");
                message = message.Replace(")", "");
                message = message.Replace(@"\", "");
                message = message.Replace(@"-", " ");

                // string msgSenderUrl = "http://182.18.176.147/pushsms.php?username=abctrq&password=90340&sender=abctrq&message=" + message + "&numbers=" + mobileNo;
                string msgSenderUrl = "http://sms.jayinegroup.com/api/pushsms.php?username=abctrq&password=90340&sender=abctrq&message=" + message + "&numbers=" + mobileNo + "&unicode=false";
                WebRequest webRequest = HttpWebRequest.Create(msgSenderUrl);
                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                Stream stream = (Stream)response.GetResponseStream();

                using (StreamReader sr = new StreamReader(stream))
                {
                    receivedData = sr.ReadToEnd();
                }



                response.Close();
                stream.Close();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

     
        public static bool IsAlertTime(DateTime timeFrom, DateTime timeTo)
        {
            try
            {

                if (timeTo < timeFrom)
                    timeTo = timeTo.AddDays(1);

                // Checking if current time is within specified range
                if (DateTime.Now >= timeFrom && DateTime.Now <= timeTo)
                    return true;

                return false;
            }
            catch (Exception ) { return false; }

        }

        static double _eQuatorialEarthRadius = 6378.1370D;
        static double _d2r = (Math.PI / 180D);

        public static double HaversineInKM(double lat1, double long1, double lat2, double long2)
        {
            double dlong = (long2 - long1) * _d2r;
            double dlat = (lat2 - lat1) * _d2r;
            double a = Math.Pow(Math.Sin(dlat / 2D), 2D) + Math.Cos(lat1 * _d2r) * Math.Cos(lat2 * _d2r) * Math.Pow(Math.Sin(dlong / 2D), 2D);
            double c = 2D * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1D - a));
            double d = _eQuatorialEarthRadius * c;

            return d;
        }



        //public static void update_stop_status(int sys_user_id, int route_id, int stop_id)
        //{
        //    General gen = new General();
        //    try
        //    {

        //        //update stop status 1 when bus will be reached within 10 min.
        //        string query_for_updating_stop_status_1 = $@"update bs_stop_master set status = 1
        //                                                where sys_user_id = {sys_user_id} and route_id = {route_id}
        //                                                and id = {stop_id}";

        //        //select stops status whose status is 1.
        //        string query = $@"select * from bs_stop_master where sys_user_id = {sys_user_id} and route_id = {route_id} 
        //                          and status = 1";

        //        DataTable dt = General.SelectQuery(query);
        //        if (dt != null) //datatable is not null
        //        {
        //            if (dt.Rows.Count > 0)// rows present
        //            {

        //                //update stop status 2 when bus already reached to stop.
        //                string query_for_updating_stop_status_2 = $@"update bs_stop_master set status = 2
        //                                                where sys_user_id = {sys_user_id} and route_id = {route_id}
        //                                                and status = 1";
        //                gen.DML(query_for_updating_stop_status_2);

        //            }

        //            gen.DML(query_for_updating_stop_status_1);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        General.WriteToLogFile("errorstop", sys_user_id, $"Error while updating the stop status : {ex.Message}");


        //    }

        //}

        public static void update_stop_status(int sys_user_id, int route_id, int stop_id)
        {
            General gen = new General();
            try
            {
                // Step 1: Mark current stop as "reached" (status = 2)
                string query_for_updating_stop_status_2 = $@"
            UPDATE bs_stop_master SET status = 2
            WHERE sys_user_id = {sys_user_id} AND route_id = {route_id} AND id = {stop_id}";

                // Step 2: Mark all previous stops as "passed" (status = 3)
                string query_for_updating_passed_stops = $@"
            UPDATE bs_stop_master 
            SET status = 3 
            WHERE sys_user_id = {sys_user_id} AND route_id = {route_id} 
              AND stop_order < (SELECT stop_order FROM bs_stop_master WHERE id = {stop_id})
              AND status NOT IN (2, 3)";

                // Step 3: Execute updates
                gen.DML(query_for_updating_stop_status_2);
                gen.DML(query_for_updating_passed_stops);

                // Step 4: Check if all stops are covered
                string query_check_remaining = $@"
            SELECT COUNT(*) FROM bs_stop_master 
            WHERE route_id = {route_id} AND sys_user_id = {sys_user_id}
              AND status NOT IN (2, 3)";

                int remaining = Convert.ToInt32(General.ExecuteScalar(query_check_remaining));

                // Step 5: If none left, mark all as "completed" (status = 4)
                if (remaining == 0)
                {
                    string query_complete_all = $@"
                UPDATE bs_stop_master 
                SET status = 4 
                WHERE route_id = {route_id} AND sys_user_id = {sys_user_id}";
                    gen.DML(query_complete_all);
                }
            }
            catch (Exception ex)
            {
                General.WriteToLogFile("errorstop", sys_user_id, $"Error while updating the stop status : {ex.Message}");
            }
        }



        public static object ExecuteScalar(string query)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    try
                    {
                        conn.Open();
                        object result = cmd.ExecuteScalar();
                        return result;
                    }
                    catch (Exception ex)
                    {
                        WriteToLogFile("ExecuteScalarError", 0, ex.Message);
                        return null;
                    }
                    finally
                    {
                        conn.Close();
                        conn.Dispose();
                        SqlConnection.ClearPool(conn);
                    }
                }
            }
        }

    }

    public class Firebase
    {
        string pathToServiceAccountKey = AppDomain.CurrentDomain.BaseDirectory;

        public Firebase()
        {
            var defaultApp = FirebaseApp.DefaultInstance;
            if (defaultApp == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    //Credential = GoogleCredential.FromFile($"{pathToServiceAccountKey}\\schoolbuddy-4fc6d-firebase-adminsdk-xh2kk-f674f1807a.json"),
                    Credential = GoogleCredential.FromFile($"{pathToServiceAccountKey}\\schoolbuddy-4fc6d-firebase-adminsdk-xh2kk-5fac47bd25.json"),
                });
            }
        }

        public async Task FirebaseNotifications(string auidoriuid, string msg, string uid)
        {
            try
            {
                var message = new Message()
                {
                    Token = auidoriuid,
                    Notification = new Notification()
                    {
                        Title = "School Bus Alert",
                        Body = msg
                    },
                    Data = new Dictionary<string, string>()
                {
                    { "title", "School Bus Alert" },
                    { "body", msg },
                    { "click_action", "FLUTTER_NOTIFICATION_CLICK" }  // Ensures app opens when tapped
                }
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                General.WriteToLogFile($"{uid}: {msg} - Notification Sent", AppDomain.CurrentDomain.BaseDirectory, "notification.txt");
            }
            catch (Exception ex)
            {
                General.WriteToLogFile($"Notification Error: {uid}: {msg} - {ex.Message}", AppDomain.CurrentDomain.BaseDirectory, "notificationerror.txt");
            }
        }
    }

    //public class Firebase
    //{
    //    string pathToServiceAccountKey = AppDomain.CurrentDomain.BaseDirectory;
    //    public Firebase()
    //    {

    //        var defaultApp = FirebaseApp.DefaultInstance;
    //        if (defaultApp == null)
    //        {
    //            FirebaseApp.Create(new AppOptions()
    //            {
    //                //Credential = GoogleCredential.FromFile($"{pathToServiceAccountKey}\\abctraq-98702-firebase-adminsdk-h5ff2-1ededa7acd.json"),
    //                Credential = GoogleCredential.FromFile($"{pathToServiceAccountKey}\\schoolbuddy-4fc6d-firebase-adminsdk-xh2kk-f674f1807a.json"),
    //            });
    //        }
    //        // Initialize Firebase App

    //    }


    //    public async Task FirebaseNotifications(string auidoriuid, string msg, string uid)
    //    {
    //        try
    //        {
    //            var registrationToken = $"{auidoriuid}";

    //            // See documentation on defining a message payload.
    //            var message = new Message()
    //            {
    //                Data = new Dictionary<string, string>()
    //            {
    //                { "abctraq", $"{msg}" },

    //            },
    //                Token = registrationToken,
    //            };

    //            // Send a message to the device corresponding to the provided
    //            // registration token.
    //            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
    //            // Response is a message ID string.
    //            General.WriteToLogFile($"{uid} :{msg} : sent notification", AppDomain.CurrentDomain.BaseDirectory, "notification.txt");
    //        }
    //        catch (Exception ex)
    //        {
    //            General.WriteToLogFile($"Notification: {uid} : {msg}" + ex.Message + "  " + Environment.NewLine + ex.Message, AppDomain.CurrentDomain.BaseDirectory, "notificationerror.txt");

    //        }
    //        // This registration token comes from the client FCM SDKs.

    //    }
    //}
}
