using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Threading;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
namespace PIS_Engine
{
    class Program : ServiceBase
    {


        public static double GetDistanceBetweenPoints(double sourcelat, double sourcelng, double destlat, double destlng,double viaslat,double viaslng)
        {
            double distance = 0;

            double dLat = (viaslat - sourcelat) / 180 * Math.PI;
            double dLong = (viaslng - sourcelng) / 180 * Math.PI;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(viaslat) * Math.Sin(dLong / 2) * Math.Sin(dLong / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            //Calculate radius of earth
            // For this you can assume any of the two points.
            double radiusE = 6378135; // Equatorial radius, in metres
            double radiusP = 6356750; // Polar Radius

            //Numerator part of function
            double nr = Math.Pow(radiusE * radiusP * Math.Cos(sourcelat / 180 * Math.PI), 2);
            //Denominator part of the function
            double dr = Math.Pow(radiusE * Math.Cos(sourcelat / 180 * Math.PI), 2)
                            + Math.Pow(radiusP * Math.Sin(sourcelat / 180 * Math.PI), 2);
            double radius = Math.Sqrt(nr / dr);

            //Calaculate distance in metres.
            distance = radius * c;
            distance = distance / 1000;

            double d = Getviasdestination(viaslat, viaslng, destlat, destlng);
            distance = distance + d;


            return distance;
        }

        public static double Getviasdestination(double lat1, double long1, double lat2, double long2)
        {
            double distance = 0;

            double dLat = (lat2 - lat1) / 180 * Math.PI;
            double dLong = (long2 - long1) / 180 * Math.PI;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(lat2) * Math.Sin(dLong / 2) * Math.Sin(dLong / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            //Calculate radius of earth
            // For this you can assume any of the two points.
            double radiusE = 6378135; // Equatorial radius, in metres
            double radiusP = 6356750; // Polar Radius

            //Numerator part of function
            double nr = Math.Pow(radiusE * radiusP * Math.Cos(lat1 / 180 * Math.PI), 2);
            //Denominator part of the function
            double dr = Math.Pow(radiusE * Math.Cos(lat1 / 180 * Math.PI), 2)
                            + Math.Pow(radiusP * Math.Sin(lat1 / 180 * Math.PI), 2);
            double radius = Math.Sqrt(nr / dr);

            //Calaculate distance in metres.
            distance = radius * c;
            distance = distance / 1000;
            return distance;
        }






        static void Main(string[] args)
        {
            ETAPredictor ee = new ETAPredictor();

            //ee.GetEta_here('28.55002,77.45936','' )


            // GetDistanceBetweenPoints(28.5169834,77.2580424,28.64028,77.31527);








            //  WebClient wc = new WebClient();


            //  string downloaded_string = wc.DownloadString("https://wse.ls.hereapi.com/2/findsequence.json?apiKey=ahPiCejvJMgVjh1IGbsmLaE2rzziLEEtIH6YCa15pLM&start=28.5169834,77.2580424&end=28.64028,77.31527&destination1=28.5755844,77.2359461&improveFor=time&departure=now&mode=fastest;car;traffic:enabled;");

            //hereTraffic traffic = JsonConvert.DeserializeObject<hereTraffic>(downloaded_string);

            // this.stopArray = this.stops.ToArray();


            //double minutes = (DateTime.Now.TimeOfDay - TimeSpan.Parse("13:00:00")).Minutes;
            //if ((DateTime.Now.TimeOfDay - TimeSpan.Parse("13::00")).TotalMinutes > 1)
            //{
            //    Console.WriteLine("hi");
            //    //Thread bs_route_master = new Thread(new ThreadStart(() => this.DML("update bs_route_master set running=0 where id=" + Route_Id)));
            //    //bs_route_master.Start();
            //    //Thread.Sleep(500);
            //    //break;

            //}
            //else
            //{
            //    Console.WriteLine("not hi");

            //}

            //ETAPredictor e = new ETAPredictor();
            //e.GetEta_here("28.5169834,77.2580424", "28.64028,77.31527", "destination1=28.5755844,77.2359461", 0);
            //Console.ReadKey();

            //StartService();

            //int no = (Int32)40 / 23;
            //ETAPredictor e  = new ETAPredictor();
            //  DataTable viaTable = General.SelectQuery("select * from bs_route_vias where route_id=593");
            //  e.Newthread(0, "28.5876293182373,77.171012878418", viaTable, 529, "", "111", true, 8, "28.60612,77.14366", 13, 0.0, "", 1, "");

            if (args.Length > 0)
            {
                for (int ii = 0; ii < args.Length; ii++)
                {
                    switch (args[ii].ToUpper())
                    {
                        case "/I":
                            InstallService();
                            return;
                        case "/U":
                            UninstallService();
                            return;
                        default:
                            break;
                    }
                }
            }
            else
            {
                System.ServiceProcess.ServiceBase.Run(new Program());
            }

        }


        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string loggerMessage = "An unhandled exception has occured in method " + new StackTrace(((Exception)e.ExceptionObject), true).GetFrame(0).GetMethod().Name + " at line no." + new StackTrace(((Exception)e.ExceptionObject), true).GetFrame(0).GetFileLineNumber() + " : " + Environment.NewLine +
                                 "Exception is: " + ((Exception)e.ExceptionObject).Message;
            General.WriteToLogFile(loggerMessage, AppDomain.CurrentDomain.BaseDirectory, "UnhandledException.txt");
            ETAPredictor.SendMessage("School Buddy Engine  Stopped", "9540048853");
        }

        protected override void OnStart(string[] args)
        {

            base.OnStart(args);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Thread t = new Thread(new ThreadStart(StartService));
            t.Start();
            Thread.Sleep(500);
        }

        /// <summary>
        /// Routes are hidden from the scheduler while running=1. If the service was restarted or
        /// a route thread died, that flag is never cleared and the route stops running for good.
        /// A route that is genuinely being tracked logs an ETA lookup to bs_Api_test every couple
        /// of minutes, so anything flagged with no lookup for 15 minutes has no live thread behind
        /// it and can be released. Routes with a recent lookup are left alone - another instance
        /// may be running them.
        /// </summary>
        private static void ClearStaleRunningFlags()
        {
            try
            {
                new General().DML(@"
                    update bs_route_master set running = 0
                    where running = 1
                      and not exists (select 1 from bs_Api_test t
                                      where t.route_id = bs_route_master.Id
                                        and t.log_date > dateadd(minute, -15, getdate()))");
                General.WriteToLogFile("PISService_Startup", 0, "Cleared stale running flags");
            }
            catch (Exception ex)
            {
                General.WriteToLogFile("PISService_Startup", 0,
                    "Error while clearing stale running flags: " + ex.Message);
            }
        }

        private static void StartService()
        {
            ClearStaleRunningFlags();

            while (true)
            {
                DayOfWeek dayOfWeek = DateTime.Now.DayOfWeek;
                if (dayOfWeek.ToString() == "Sunday")
                {
                    Thread.Sleep(7200000);
                    continue;
                }                          
                DataTable dataTable;
                do
                {
                    dataTable = General.SelectQuery("select * from bs_route_master where is_active = 1 and is_pis_enabled=1 and running=0 order by start_time_up");
                }
                while (dataTable == null || dataTable.Rows.Count == 0);
                DataTable dataTable2 = General.SelectQuery("select * from bs_holidays");
                DataTable dataTable3 = General.SelectQuery("select * from bs_notification_subscription");
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    try
                    {
                        bool flag = false;
                        TimeSpan timeOfDay = DateTime.Now.TimeOfDay;
                        TimeSpan timeSpan = (TimeSpan)dataTable.Rows[i]["start_time_up"];
                        TimeSpan timeSpan2 = timeSpan - timeOfDay;
                        //TimeSpan timeSpan2 = ()6;
                        if (!(timeSpan2.TotalMinutes < 10.0) || !(timeSpan2.TotalMinutes > -10.0))
                        {
                            continue;
                        }
                        int id = Convert.ToInt32(dataTable.Rows[i]["Id"]);
                        int user_id = Convert.ToInt32(dataTable.Rows[i]["sys_user_id"]);
                        int num = -1; // Default value for unknown case

                        try
                        {
                            // 1️⃣ First check in atltracking (Trackofy users)
                            DataTable dtTrackofy = General.SelectQuery("select is_sat_off from atltracking.dbo.tbl_users where id=" + user_id);
                            if (dtTrackofy != null && dtTrackofy.Rows.Count > 0)
                            {
                                num = Convert.ToInt32(dtTrackofy.Rows[0]["is_sat_off"]);
                            }
                            else
                            {
                                // 2️⃣ Fallback check in newtrack (Abctrack users)
                                DataTable dtAbctrack = General.SelectQuery("select is_sat_off from users where id=" + user_id);
                                if (dtAbctrack != null && dtAbctrack.Rows.Count > 0)
                                {
                                    num = Convert.ToInt32(dtAbctrack.Rows[0]["is_sat_off"]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            General.WriteToLogFile("PISService_SatCheck", user_id, "Error while checking is_sat_off: " + ex.Message);
                        }

                        if (num == 1 && dayOfWeek.ToString() == "Saturday")
                        {
                            continue;
                        }

                        if (dataTable2 == null)
                        {
                            goto IL_0324;
                        }
                        EnumerableRowCollection<DataRow> enumerableRowCollection = from x in dataTable2.AsEnumerable()
                                                                                   where x.Field<int>("sys_user_id") == user_id
                                                                                   select x;
                        Thread.Sleep(200);
                        foreach (DataRow item in enumerableRowCollection)
                        {
                            string text = DateTime.Now.ToString("MM/dd/yyyy").Replace('-', '/');
                            if (text == item.Field<string>("from_date") || text == item.Field<string>("to_date"))
                            {
                                flag = true;
                                break;
                            }
                            //string text2 = item.Field<string>("from_date");
                            //string text3 = item.Field<string>("to_date");
                            //DateTime dateTime = new DateTime(Convert.ToInt32(text2.Substring(6, 4)), Convert.ToInt32(text2.Substring(0, 2)), Convert.ToInt32(text2.Substring(3, 2)));
                            //DateTime dateTime2 = new DateTime(Convert.ToInt32(text3.Substring(6, 4)), Convert.ToInt32(text3.Substring(0, 2)), Convert.ToInt32(text3.Substring(3, 2)));
                            //if (DateTime.Now > dateTime && DateTime.Now < dateTime2)
                            //{
                            //    flag = true;
                            //    break;
                            //}

                            string text2 = item.Field<string>("from_date")?.Trim();
                            string text3 = item.Field<string>("to_date")?.Trim();

                            string[] formats = {
    "MM/dd/yyyy",
    "M/d/yyyy",
    "yyyy-MM-dd",
    "dd-MM-yyyy",
    "dd/MM/yyyy",
    "yyyy/MM/dd",
    "yyyyMMdd"
};

                            if (DateTime.TryParseExact(text2, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime) &&
                                DateTime.TryParseExact(text3, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime2))
                            {
                                if (DateTime.Now > dateTime && DateTime.Now < dateTime2)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                            else
                            {
                                General.WriteToLogFile("DateParseError", user_id, $"Unable to parse holiday from_date: {text2} or to_date: {text3}");
                            }

                        }
                        if (!flag)
                        {
                            goto IL_0324;
                        }
                        goto end_IL_008c;
                    IL_0324:
                        string route_name = dataTable.Rows[i]["route_name"].ToString().Trim();
                        TimeSpan end_time = TimeSpan.Parse(dataTable.Rows[i]["end_time_up"].ToString());
                        int service_id = Convert.ToInt32(dataTable.Rows[i]["sys_service_id"]);
                        if (dataTable3 == null)
                        {
                            continue;
                        }
                        EnumerableRowCollection<DataRow> source = from x in dataTable3.AsEnumerable()
                                                                  where x.Field<int>("sys_user_id") == user_id
                                                                  select x;
                        Thread.Sleep(200);
                        if (source.FirstOrDefault().Field<byte>("pre_stop") == 1)
                        {
                            ETAPredictor eta = new ETAPredictor();
                            Thread thread = new Thread((ThreadStart)delegate
                            {
                                eta.Predict(id, user_id, service_id, route_name, end_time);
                            });
                            thread.Start();
                            Thread.Sleep(300);
                        }
                    end_IL_008c:;
                    }
                    catch (Exception ex)
                    {
                        General.WriteToLogFile("PISService_MainLoop", 0,
                            "Error while processing route: " + ex.Message);
                    }
                }
                Thread.Sleep(300000);
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        protected override void Dispose(bool disposing)
        {
            //clean your resources if you have to
            base.Dispose(disposing);
        }

        private static void InstallService()
        {
            if (IsServiceInstalled())
            {
                UninstallService();
            }

            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static bool IsServiceInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == "PISEngine");
        }

        private static void UninstallService()
        {

            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }

    }
}
