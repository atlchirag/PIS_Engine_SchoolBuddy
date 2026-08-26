using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.IO;
using System.Device.Location;
using System.Security.Cryptography;
using System.Threading;
namespace PIS_Engine
{
    class ETAPredictor
    {
        //private string API_KEY = "AIzaSyAPEMdDzjc_qpAFeP0BRPWOYuXYfvb3EE8";
        private int _RouteId;
        //private int _TripId = 0;
        private int _serviceId;
        private int _UserID = 0;
       // string cs1 = "Data Source=45.113.189.23;Initial Catalog=newtrack;User ID=newtrack;Password=55hD&44m7E3jnd;Max Pool Size=32767;";
        string cs1 = "Data Source=192.168.23.131,15433;Initial Catalog=newtrack;User ID=newtrack;Password=55hD&44m7E3jnd;Max Pool Size=32767;";
        //string cs1 = "Data Source=103.108.12.184,15433;Initial Catalog=newtrack;User ID=newtrack;Password=55hD&44m7E3jnd;Max Pool Size=32767;";
        //string cs = "Data Source =45.113.189.23; Initial Catalog = newtrack; User ID = newtrack; Password = 55hD&44m7E3jnd; pooling=false;timeout=3000";
        //static int totalCounter = 0;
        private bool startThread = true;
        private string sender_id = "TSTSMS";
        private int service_from = 1;
        private string _Route_Name;
        private const int _etaUpdateTime = 60000;//it is 1 min and url will be hit after 1 min
        //  string inputUrl = "https://maps.googleapis.com/maps/api/directions/xml?origin=saket&destination=connaught+place&mode=driving&alternatives=false&traffic_model=best_guess&departure_time=now&client=gme-nucleusmicrosystems&channel=track.georadius.in&sensor=true";
        private string message_api = "http://smsby2.in/sendsms.php?username=atlanta&password=atlanta@123&sender=ATLNTA&mobile={0}&message={1}&route=T";
        DateTime EngineStart;
        List<Stop> stops = new List<Stop>();
        Stop[] stopArray;


        public void Predict(int Route_Id, int user_id, int service_id, string route_name, TimeSpan end_time)
        {

            _RouteId = Route_Id;
           
            this._Route_Name = route_name;
            this._RouteId = Route_Id;
           //  _TripId = tripId;
            this._serviceId = service_id;
            this._UserID = user_id;

            Thread tUpdate = new Thread(new ThreadStart(() => this.DML("update bs_route_master set running=1 where id=" + this._RouteId)));
           tUpdate.Start();  
            Thread.Sleep(500);  

            General.WriteToLogFile(route_name, user_id, "Route Started");

            int retreivalCounter = 1;
            string qry = @"select st.id as student_id, s.Id as id,s.stop_order,s.user_stop_name,s.latitude,s.longitude,s.eta,st.mobile_no1 as Mobile_No,s.msg,s.link_no from bs_stop_master s left join bs_route_students rs 
                                         on s.Id=rs.stop_id left join bs_student_master_backup st on st.Id = rs.student_id where s.route_id=" + this._RouteId + " order by s.stop_order ";
            DataTable dt = SelectQuery(qry);

            if (dt != null)
            {
                var vDt = dt.AsEnumerable();

                var stopVar = vDt.GroupBy(m => m.Field<int>("id")).Select(group => new
                {
                    StopId = group.Key,
                    Count = group.Count()
                });

                

                foreach (var stop in stopVar)
                {
                   
                    Stop oStop = new Stop();
                    List<Student> studentList = new List<Student>();
                    var students = vDt.Where(m => m.Field<int>("id") == stop.StopId);

                    General.WriteToLogFile("Debug", _UserID, $"Processing stopId: {stop.StopId}, Count: {stop.Count}");

                    foreach (var student in students)
                    {
                       
                        if (student.Field<string>("Mobile_No") != null)
                        {
                            Student s = new Student();
                            s.StopId = stop.StopId;
                            s.PhnNO = student.Field<string>("Mobile_No");
                            s.id = student.Field<int>("student_id");
                            //Console.WriteLine(s.id);
                            //Console.WriteLine(s.PhnNO);
                            studentList.Add(s);
                          

                        }

                        //----------------------------------------------------------------
                    }
                    //Console.WriteLine(studentList+"list");
                    //  oStop.eta_msg = stop.msg;
                    oStop.Id = stop.StopId;
                    oStop.Students = studentList;
                    oStop.StopName = students.First().Field<string>("user_stop_name");

                    oStop.GeoLocation = new GeoLocation();
                    //try
                    //{
                    //    oStop.GeoLocation.Latitude = Convert.ToDouble(students.FirstOrDefault().Field<Single>("latitude"));
                    //    oStop.GeoLocation.Longitude = Convert.ToDouble(students.First().Field<Single>("longitude"));
                    //    oStop.eta_msg = Convert.ToInt32(students.FirstOrDefault().Field<int>("msg"));

                    //    oStop.linkNo = Convert.ToInt32(students.FirstOrDefault().Field<int>("link_no"));
                    //}
                    //catch(Exception ex)
                    //{
                    //    General.WriteToLogFile("GeoError", _UserID, $"LatLng missing or invalid for stopId: {stop.StopId} — {ex.Message}");

                    //    oStop.linkNo = 0;
                    //    oStop.MsgSend = true;
                    //    continue;
                    //}
                    try
                    {
                        var s = students.FirstOrDefault();
                        if (s != null)
                        {
                            oStop.GeoLocation.Latitude = Convert.ToDouble(s.Field<Single>("latitude"));
                            oStop.GeoLocation.Longitude = Convert.ToDouble(s.Field<Single>("longitude"));
                            oStop.eta_msg = s.Field<int?>("msg") ?? 10;

                            // Important:
                            // If link_no is NULL, keep it 0.
                            // Old routes with proper link_no will continue working.
                            // New auto-created stops without viasing will use fallback mode.
                            oStop.linkNo = s.Field<int?>("link_no") ?? 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        General.WriteToLogFile("GeoError", _UserID, $"LatLng missing or invalid for stopId: {oStop.Id} — {ex.Message}");
                        oStop.MsgSend = true;
                        continue;
                    }

                    //  oStop.StopOrder = Convert.ToInt32(students.First().Field<Int16>("stop_order"));
                    string via = null;//students.First().Field<string>("via");
                    if (!string.IsNullOrEmpty(via))
                    {
                        oStop.WayPoints = new List<GeoLocation>();
                        string[] waypoints = via.Split('|');

                        foreach (string waypoint in waypoints)
                        {

                            string[] latlng = waypoint.Split(',');
                            GeoLocation g = new GeoLocation();
                            g.Latitude = Convert.ToDouble(latlng[0]);
                            g.Longitude = Convert.ToDouble(latlng[1]);
                            oStop.WayPoints.Add(g);
                        }


                    }
                    //  string y = "";

                    this.stops.Add(oStop);
                }
                this.stopArray = this.stops.ToArray();


                General.WriteToLogFile(route_name, user_id, "Total Stops are " + Convert.ToString(stopArray.Length));
                string source = "";
                string destination = "";
                string gps_time = "";
                string gps_new_time = "";
                int lastViahit = 1;
                bool FromVias = true;
                //DataTable viaTable = SelectQuery("select * from bs_route_vias where route_id=" + this._RouteId + "order by via_order");
                //int exit_via_condition = 0;
                //if (viaTable != null)
                //{
                //    General.WriteToLogFile(this._Route_Name, this._UserID, "Total vias are " + viaTable.Rows.Count);
                //}

                DataTable viaTable = SelectQuery("select * from bs_route_vias where route_id=" + this._RouteId + " order by via_order");

                bool hasVias = viaTable != null && viaTable.Rows.Count > 0;

                int exit_via_condition = 0;

                General.WriteToLogFile(
                    this._Route_Name,
                    this._UserID,
                    hasVias
                        ? "Total vias are " + viaTable.Rows.Count + ". Running old VIA based ETA flow."
                        : "No route vias found. Running DIRECT GPS-to-stop ETA fallback flow."
                );

                DataTable PhnTable = SelectQuery("select * from bs_all_stop_alert where sys_user_id=" + user_id + " order by id");
                StringBuilder Fixed_No = new StringBuilder();
                try
                {
                    for (int k = 0; k < PhnTable.Rows.Count; k++)
                    {

                        Fixed_No.Append(PhnTable.Rows[k]["mobile_No"].ToString().Trim() + ",");
                    }
                }
                catch (Exception e) { throw e; }

                string AllNo = Fixed_No.ToString();

                try
                {
                    DataTable api_table = SelectQuery("select msg_api_url,sender_id,msg_service_from from bs_sms_master where sys_user_id=" + user_id);
                    if (api_table == null || api_table.Rows.Count == 0)
                    {
                        message_api = "http://smsby2.in/sendsms.php?username=atlanta&password=atlanta@123&sender=ATLNTA&mobile={0}&message={1}&route=T";
                    }
                    else
                    {
                        message_api = api_table.Rows[0]["msg_api_url"].ToString();
                        service_from = Convert.ToInt32(api_table.Rows[0]["msg_service_from"]);
                        sender_id = api_table.Rows[0]["sender_id"].ToString().Trim();

                    }
                }
                catch { message_api = "http://smsby2.in/sendsms.php?username=atlanta&password=atlanta@123&sender=ATLNTA&mobile={0}&message={1}&route=T"; }
                General.WriteToLogFile(route_name, user_id, "message Api is  " + message_api);
                if (this.stops.Count != 0)
                {
                    if (startEngine())
                    {
                        General.WriteToLogFile(route_name, user_id, "Engine Started");
                        while (true)
                        {
                            Thread.Sleep(120000);
                            //if (retreivalCounter != 1)
                            //    Console.WriteLine();
                            TimeSpan diff = DateTime.Now - EngineStart;
                            //if the engine is running since 4 hours it will stop 
                            if (diff.TotalHours > 4.0)
                            {
                                Thread trouteMaster = new Thread(new ThreadStart(() => this.DML("update bs_route_master set running=0 where id=" + Route_Id)));

                                trouteMaster.Start();
                                Thread.Sleep(300);
                                General.WriteToLogFile(route_name, user_id, "Route Stop as 4 hrs passed");
                                break;
                            }

                            if ((DateTime.Now.TimeOfDay - end_time).TotalMinutes > 1)
                            {

                                Thread bs_route_master = new Thread(new ThreadStart(() => this.DML("update bs_route_master set running=0 where id=" + Route_Id)));
                                bs_route_master.Start();
                                Thread.Sleep(500);
                                General.WriteToLogFile(route_name, user_id, "Route Stop because of end time");
                                break;

                            }
                            //the engine will run till the last is reached or the last message is send 

                            if (this.stops.Any(x => x.MsgSend == false))
                            {

                                //int NotificationTime = 12;
                                double reachDis = 140;
                                int eta = 0;
                                int j = this.stops.ToArray().Length;

                                DataTable dt1 = new DataTable();
                                if (retreivalCounter > 1)//here the last data received time from the data base is stored 
                                    gps_time = gps_new_time;
                                for (int w = 0; w < 6; w++)
                                {
                                    if (retreivalCounter != 1)
                                    {

                                        System.Threading.Thread.Sleep(9000);

                                    }

                                    if ((DateTime.Now.TimeOfDay - end_time).TotalMinutes > 1)
                                    {
                                        this.startThread = false;
                                        break;
                                    }

                                    try
                                    {
                                        // 1️⃣ Try fetching from trackofy DB first
                                        dt1 = SelectQuery("select gps_latitude, gps_longitude, gps_time, gps_speed from atltracking.dbo.tbl_latest_telemetry where sys_service_id=" + this._serviceId);

                                        // 2️⃣ Fallback to abctrack DB if no data
                                        if (dt1 == null || dt1.Rows.Count == 0)
                                        {
                                            dt1 = SelectQuery("select gps_latitude, gps_longitude, gps_time, gps_speed from latest_telemetry where sys_service_id=" + this._serviceId);
                                        }

                                        // 3️⃣ Final check
                                        if (dt1 == null || dt1.Rows.Count == 0)
                                        {
                                            continue;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        General.WriteToLogFile(_Route_Name, _UserID, "Telemetry Fetch Error: " + ex.Message);
                                        continue;
                                    }
                                    try
                                    {
                                        FromVias = true;
                                        DateTime date = Convert.ToDateTime(dt1.Rows[0]["gps_time"]).AddMinutes(330);
                                        source = dt1.Rows[0]["gps_latitude"].ToString() + "," + dt1.Rows[0]["gps_longitude"].ToString();

                                        if (retreivalCounter == 1) // for first time gps time is taken in gps_time and for all other hits it is taken in gps_new_time and the both are compared 
                                            gps_time = dt1.Rows[0]["gps_time"].ToString();
                                        else
                                        {
                                            gps_new_time = dt1.Rows[0]["gps_time"].ToString();
                                        }
                                        if (gps_time != gps_new_time || retreivalCounter == 1)
                                        {

                                            try  // here we check the geofence no where the bus is 
                                            {
                                                if (!hasVias)
                                                {
                                                    // New flow:
                                                    // No route vias available, so keep source as current vehicle GPS.
                                                    // ETA will be calculated directly from current GPS location to every stop.
                                                    FromVias = false;
                                                    break;
                                                }
                                                if (retreivalCounter != 1)
                                                {
                                                    double gps_speed = Convert.ToDouble(dt1.Rows[0]["gps_speed"].ToString());
                                                    if (gps_speed < 3)
                                                    {
                                                        if (w != 0)
                                                        {
                                                            w--;
                                                        }
                                                        continue;
                                                    }

                                                }
                                                string slat = source.Substring(0, source.IndexOf(',')).Replace(',', ' ').Trim();
                                                string slon = source.Substring(source.IndexOf(',') + 1, source.Length - source.IndexOf(',') - 1).Replace(',', ' ').Trim();
                                                for (int k = lastViahit - 1; k < viaTable.Rows.Count; k++)
                                                {

                                                    string elat = viaTable.Rows[k]["latitude"].ToString().Replace(',', ' ').Trim();
                                                    string elon = viaTable.Rows[k]["longitude"].ToString().Replace(',', ' ').Trim();
                                                    double dis = GetDistance(slat, slon, elat, elon);

                                                    if (dis < 100) // if the dis between the bus and geofence is less than 100 mts that geofence lat lng is taken as source and its index is stored
                                                    {
                                                        //double google_dis = GetDistance_Google(slat, slon, elat, elon);
                                                        //if (google_dis > 120)
                                                        //{
                                                        //    google_dis = GetDistance_Google(elat, elon, slat, slon);

                                                        //}
                                                        //if (google_dis <= 120 || google_dis == 0.0)
                                                        //{
                                                        // FromVias = true;
                                                        source = elat + "," + elon;
                                                        if (k - lastViahit >= 46)
                                                        {
                                                            exit_via_condition += 1;
                                                        }
                                                        if (k - lastViahit < 46 || retreivalCounter == 1 || exit_via_condition > 3)
                                                        {
                                                            lastViahit = k;
                                                            exit_via_condition = 0;
                                                        }
                                                        if (lastViahit == 0)
                                                            lastViahit = 1;
                                                        break;
                                                        //  }

                                                    }
                                                    if (k == viaTable.Rows.Count - 1)
                                                    {
                                                        FromVias = false;
                                                        //lastViahit += 1;
                                                    }

                                                }
                                            }

                                            catch { }
                                        }
                                        else
                                        {
                                            FromVias = false;
                                        }

                                    }
                                    catch { }
                                    //  Console.WriteLine(FromVias);
                                    for (int i = 0; i < j; i++)
                                    {
                                        try
                                        {
                                            destination = stopArray[i].GeoLocation.Latitude.ToString() + "," + stopArray[i].GeoLocation.Longitude.ToString(); ;
                                            // we keep an calculating the bus distance from every stop and if distance between stop and bus is less than specified dis stop is taken is reached 
                                            Stop stop = stopArray[i];
                                            string slat = source.Substring(0, source.IndexOf(',')).Replace(',', ' ').Trim();
                                            string slon = source.Substring(source.IndexOf(',') + 1, source.Length - source.IndexOf(',') - 1).Replace(',', ' ').Trim();
                                            string elat = destination.Substring(0, destination.IndexOf(',')).Replace(',', ' ').Trim();
                                            string elon = destination.Substring(destination.IndexOf(',') + 1, destination.Length - destination.IndexOf(',') - 1).Replace(',', ' ').Trim();
                                            if (!this.stopArray[i].IsReached)
                                                this.stopArray[i].Distance = GetDistance(slat, slon, elat, elon);

                                            //if (stopArray[i].Distance <= reachDis && stopArray[i].ETA_Default < 5 && stopArray[i].IsReached == false)
                                            //{

                                            //    Thread tbs_stop_mis = new Thread(new ThreadStart(() => this.DML("insert into bs_stop_mis(route_id,sys_service_id,bus_stop_id,ata,log_date) values(" + _RouteId + "," + _serviceId + "," + stop.Id + ",'" + DateTime.Now.ToString("HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "')")));
                                            //    tbs_stop_mis.Start();
                                            //    Thread.Sleep(100);
                                            //}
                                            // if the bus has reached the above link no for the specified stop then also stop is taken as is reached 
                                            if (hasVias && lastViahit >= 0 && lastViahit < viaTable.Rows.Count)
                                            {
                                                if (this.stopArray[i].linkNo > 0 &&
                                                    this.stopArray[i].linkNo < Convert.ToInt32(viaTable.Rows[lastViahit]["via_order"]) &&
                                                    this.stopArray[i].IsReached == false)
                                                {
                                                    this.stopArray[i].IsReached = true;

                                                    Thread Tbs_stop_mis = new Thread(new ThreadStart(() =>
                                                        this.DML("insert into bs_stop_mis(route_id,sys_service_id,stop_id,ata,log_date) values(" +
                                                            _RouteId + "," +
                                                            _serviceId + "," +
                                                            stop.Id + ",'" +
                                                            DateTime.Now.ToString("HH:mm:ss") + "','" +
                                                            DateTime.Now.ToString("yyyy-MM-dd") + "')")));

                                                    Tbs_stop_mis.Start();
                                                    Thread.Sleep(100);
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                    if (retreivalCounter == 1)
                                        break;
                                }
                                //    if(retreivalCounter !=1)

                                /// NEW THREAD
                                /// 
                                if (this.startThread)
                                {
                                    //Newthread(eta, destination, viaTable, lastViahit, gps_time, gps_new_time, FromVias, retreivalCounter, source, j, reachDis, route_name, user_id, AllNo);
                                    Thread tPredict = new Thread(new ThreadStart(() => Newthread(eta, destination, viaTable, lastViahit, gps_time, gps_new_time, FromVias, retreivalCounter, source, j, reachDis, route_name, user_id, AllNo,Route_Id)));
                                    tPredict.Start();
                                    Thread.Sleep(500);
                                }

                            }
                            else
                            {
                                Thread.Sleep(10 * 60 * 1000);
                                foreach (Stop stop in stops)
                                {
                                    stop.MsgSend = false;
                                    stop.IsReached = false;
                                    Thread tbs_stop_master = new Thread(new ThreadStart(() => this.DML("update bs_stop_master set is_arrived=0 where Id=" + stop.Id)));
                                    tbs_stop_master.Start();
                                    Thread.Sleep(500);

                                }
                                Thread bs_route_master = new Thread(new ThreadStart(() => this.DML("update bs_route_master set running=0 where id=" + Route_Id)));
                                bs_route_master.Start();
                                Thread.Sleep(500);
                                General.WriteToLogFile(route_name, user_id, "Route Stop all msg send");
                                break;
                            }
                            retreivalCounter += 1;
                        }
                        //  Console.WriteLine("Total Hits: " + totalCounter);
                    }
                }
                else
                {

                    Thread.Sleep(5 * 60 * 1000);
                }
            }
            else
            {
                Thread.Sleep(30 * 1000);
                Predict(Route_Id, user_id, service_id, route_name, end_time);

            }
        }

        //public void Newthread(int eta, string destination, DataTable viaTable, int lastViahit, string gps_time, string gps_new_time, bool FromVias, int retreivalCounter, string source, int j, double reachDis, string route_name, int user_id, string All_Nos,int _RouteId)
        //{
        //    //Console.WriteLine();
        //    this.startThread = false;
        //    for (int i = 0; i < j; i++)
        //    {
        //        try
        //        {
        //            Stop stop = this.stopArray[i];
        //            if (stop.MsgSend == true && stop.IsReached == true)
        //                continue;
        //            // string source = tblStop.Rows[i]["latitude"].ToString() + "," + tblStop.Rows[i]["longitude"].ToString();
        //            destination = this.stopArray[i].GeoLocation.Latitude.ToString() + "," + this.stopArray[i].GeoLocation.Longitude.ToString(); ;
        //            string vias = "";
        //            //if (i > 0)
        //            // in linkIndex we take the link no of the stop since we are allowed only 23 waypoints the accordingly we add waypoints by taking latest link no 

        //            if (FromVias) // we check weather the source is from the geofence we have taken if it is we taken fromvias as true otherwise as false
        //            {
        //                var linkIndex = from v in viaTable.AsEnumerable()
        //                                let row = v.Field<int>("via_order")
        //                                where row == stop.linkNo
        //                                select new { number = row };

        //                int linkIndexNo = Convert.ToInt32(linkIndex.FirstOrDefault().number);
        //                int No = 0;
        //                int difference = linkIndexNo - Convert.ToInt32(viaTable.Rows[lastViahit]["via_order"]);
        //                if (difference > 23)
        //                {
        //                    No = (Int32)difference / 23;
        //                }
        //                int viasAdded = 1;
        //                for (int z = lastViahit + 1; z < viaTable.Rows.Count; z++)
        //                {

        //                    if (Convert.ToInt32(viaTable.Rows[z]["via_order"]) < stop.linkNo)
        //                    {
        //                        vias += "destination" + viasAdded.ToString() + "=" + viaTable.Rows[z]["latitude"].ToString() + "," + viaTable.Rows[z]["longitude"].ToString() + "|";
        //                        viasAdded += 1;
        //                    }
        //                    else
        //                    {
        //                        break;
        //                    }
        //                    if (viasAdded > 23)
        //                    {
        //                        break;
        //                    }
        //                    z += No;
        //                }
        //            }
        //            else
        //            {
        //                if (i > 0)
        //                {
        //                    for (int z = 0; z < i; z++)
        //                    {
        //                        if (this.stopArray[z + 1].IsReached == true)
        //                            this.stopArray[z].IsReached = true;

        //                        if (this.stopArray[z].IsReached == true)
        //                            continue;
        //                        vias += "destination" + z.ToString() + "=" + this.stopArray[z].GeoLocation.Latitude.ToString() + "," + this.stopArray[z].GeoLocation.Longitude.ToString() + "|";
        //                    }
        //                }

        //            }
        //            //}
        //            if (gps_new_time != gps_time) // if the data is the recent data the eta is calculated from google api other wise eta is subtraced by 1 only
        //            {
        //                // Console.WriteLine("RouteId = " + _RouteId + " : Stop = " + stop.StopName);
        //                //if ((stop.ETA_Default < 19) || (retreivalCounter - stop.TripId > 15))
        //                //{

        //                if (stop.MsgSend)
        //                {
        //                    int time_diff = retreivalCounter - stop.TripId;
        //                    eta = stop.ETA_Default - time_diff;
        //                }
        //                else
        //                {
        //                    if (stop.ETA_Default < 16 || (retreivalCounter - stop.TripId > 20))
        //                    {
        //                        eta = Convert.ToInt32(GetEta_here(source, destination, vias, retreivalCounter));
        //                        General.WriteToLogFile1(route_name, _UserID, eta.ToString());
        //                        if (eta == 0)
        //                        {
        //                            int time_diff = retreivalCounter - stop.TripId;
        //                            eta = stop.ETA_Default - time_diff;

        //                        }
        //                        else
        //                        {
        //                            eta = Convert.ToInt32(Math.Round((double)eta / 60, 0));
        //                        }

        //                    }
        //                    else
        //                    {
        //                        int time_diff = retreivalCounter - stop.TripId;
        //                        eta = stop.ETA_Default - time_diff - 2;

        //                    }
        //                }



        //                //if (eta - stop.ETA_Default > 10)
        //                //    eta = stop.ETA_Default - 1;
        //                if (eta - stop.ETA_Default > 7 && retreivalCounter != 1)
        //                {
        //                    //  SendMessage("There is increment of  more than 7 min  at route no " + _RouteId + " at stop " + stop.Id + " at counter " + retreivalCounter, "9540048853,9013757900");
        //                    Thread bs_test = new Thread(new ThreadStart(() => this.DML("insert into bs_test_route(ETA_diff,stopId,RouteId,Counter,time,date) values(" + (eta - stop.ETA_Default) + "," + stop.Id + "," + _RouteId + "," + retreivalCounter + ",'" + DateTime.Now.ToString("HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "')")));
        //                    bs_test.Start();
        //                    Thread.Sleep(100);

        //                }
        //                //Console.WriteLine(" Eta is " + eta);
        //                //}
        //                //else
        //                //    eta = stop.ETA_Default - 2;
        //            }
        //            else
        //            {
        //                if (stop.ETA_Default > 1)
        //                {
        //                    int time_diff = retreivalCounter - stop.TripId;
        //                    eta = stop.ETA_Default - time_diff;
        //                }
        //                else
        //                    eta = stop.ETA_Default;

        //                if (eta == 1 && stop.IsReached == false)
        //                {
        //                    stop.IsReached = true;
        //                    Thread bs_stop_mis = new Thread(new ThreadStart(() => this.DML("insert into bs_stop_mis(route_id,sys_service_id,stop_id,ata,log_date) values(" + _RouteId + "," + _serviceId + "," + stop.Id + ",'" + DateTime.Now.ToString("HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "')")));
        //                    bs_stop_mis.Start();
        //                    Thread.Sleep(100);

        //                }
        //                // Console.WriteLine("RouteId = " + _RouteId + " : Stop = " + stop.StopName);
        //                //  Console.WriteLine("RTE Eta is : " + eta.ToString());
        //            }
        //            stop.TripId = retreivalCounter;
        //            StringBuilder remMessage = new StringBuilder();
        //            //if bus has changed its route then a reminder message is send to the parents the conditions are mentioned as follows 
        //            if (stop.ETA_Default < eta + 3 && stop.MsgSend == true && (eta > stop.eta_msg + 3) && stop.linkNo != 0)
        //            {

        //                //  List<Student> students = stop.Students;
        //                ////  string PhnNoLists = "";

        //                //  foreach (Student student in students)
        //                //  {
        //                //    //  PhnNoLists += student.PhnNO + ",";

        //                //  }

        //                // SendMessage(remMessage, PhnNoLists.ToString());
        //                // stop.MsgSend = false;
        //                // stop.IsReached = false;
        //            }

        //            stop.ETA_Default = eta;
        //            if (stop.IsReached == false)
        //            {

        //                Thread bs_stop_master = new Thread(new ThreadStart(() => this.DML("Update bs_stop_master set eta='" + DateTime.Now.AddMinutes(stop.ETA_Default).ToString("HH:mm:ss") + "' where Id=" + stop.Id)));
        //                General.WriteToLogFile(this._Route_Name, user_id, "route  updated");
        //                bs_stop_master.Start();
        //                Thread.Sleep(100);


        //            }


        //            //    Console.WriteLine(" Distance is : " + stopArray[i].Distance + " mtrs");

        //            if (eta > 0)
        //            {

        //                Thread bs_test_route = new Thread(new ThreadStart(() => this.DML("insert into bs_test_route_new values (" + _RouteId + ",'" + stop.Id + "','" + eta.ToString() + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + retreivalCounter + ",'" + gps_time + "','" + gps_new_time + "','" + FromVias.ToString() + "')")));
        //                bs_test_route.Start();
        //                Thread.Sleep(100);
        //            }
        //            string message = "Dear parent, the bus for route " + route_name + " shall reach at your stop within 10 min " + DateTime.Now.ToString("HH:mm:ss");
        //            if (!FromVias)
        //            {
        //                if (i == 0) // here we check the distance between the stop and bus if less than specified dis the stop is taken to be is reached or eta is less than equal to 1 than also 
        //                {
        //                    if ((this.stopArray[i].Distance <= reachDis && this.stopArray[i].Distance != 0.0 && this.stopArray[i].IsReached == false) || (stop.ETA_Default <= 1 && stop.IsReached == false))
        //                    {
        //                        stop.IsReached = true;

        //                    }

        //                    Thread tBstop_mis = new Thread(new ThreadStart(() => this.DML("insert into bs_stop_mis(route_id,sys_service_id,stop_id,ata,log_date) values(" + _RouteId + "," + _serviceId + "," + stop.Id + ",'" + DateTime.Now.ToString("HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "')")));
        //                    tBstop_mis.Start();
        //                    Thread.Sleep(100);
        //                }
        //                else// same as above just check one more condition if the last stop  is reached then only the stop is considered to be is reached
        //                {
        //                    if ((this.stopArray[i].Distance <= reachDis && this.stopArray[i - 1].IsReached == true && this.stopArray[i].IsReached == false) || (stop.ETA_Default <= 1 && stop.IsReached == false))
        //                    {
        //                        stop.IsReached = true;
        //                        Thread bs_stop_mis = new Thread(new ThreadStart(() => this.DML("insert into bs_stop_mis(route_id,sys_service_id,stop_id,ata,log_date) values(" + _RouteId + "," + _serviceId + "," + stop.Id + ",'" + DateTime.Now.ToString("HH:mm:ss") + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "')")));
        //                        bs_stop_mis.Start();
        //                        Thread.Sleep(100);
        //                    }
        //                }
        //            }
        //            if (stop.eta_msg >= eta && eta > 0 && stop.linkNo != 0)
        //            {
        //                if (stop.MsgSend == false) // message are send to the parents 
        //                {
        //                    //msg is sent to the parents 
        //                    List<Student> students = stop.Students;
        //                    string PhnNoLists = "";
        //                    StringBuilder student_ids = new StringBuilder();
        //                    foreach (Student student in students)
        //                    {
        //                        PhnNoLists += student.PhnNO + ",";
        //                        student_ids.Append(student.id.ToString() + ",");

        //                    }

        //                    //SendMessage(message, "9540048853," + PhnNoLists);

        //                    // ETAPredictor e = new ETAPredictor();


        //                    //Thread t = new Thread(new ThreadStart(() => SendMessage(message + " .Download the app and track bus: https://goo.gl/akxDX3", All_Nos + PhnNoLists, _RouteId, _serviceId, stop.Id, student_ids.ToString())));
        //                    //t.Priority = ThreadPriority.Highest;
        //                    //t.Start();
        //                    //Thread.Sleep(300);
        //                    stop.MsgSend = true;
        //                    //Thread bs_alert_log = new Thread(new ThreadStart(() => this.DML("insert into bs_alert_log(route_id,sys_service_id,bus_stop_id,alert_sent_on) values(" + _RouteId + "," + _serviceId + "," + stop.Id + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')")));
        //                    //bs_alert_log.Start();
        //                    //Thread.Sleep(100);
        //                    Thread t2 = new Thread(new ThreadStart(() => SendNotification(All_Nos + PhnNoLists, message)));
        //                    t2.Start();
        //                    Thread.Sleep(300);
        //                    //stops[5].MsgSend = true;
        //                    //   bs_alert_log.Start();
        //                    General gen = new General();

        //                    General.update_stop_status(user_id,_RouteId,stop.Id);

        //                    General.WriteToLogFile(route_name, user_id, "message send to stop " + stop.StopName);
        //                    stop.MsgSendAt = DateTime.Now.TimeOfDay;
        //                    System.Threading.Thread.Sleep(60000);

        //                }
        //            }


        //        }
        //        catch(Exception ex) 
        //        {
        //            General.WriteToLogFile(route_name, user_id, $"ex-{ex.Message}");

        //        }
        //    }
        //    this.startThread = true;

        //}


        public void Newthread(int eta, string destination, DataTable viaTable, int lastViahit, string gps_time, string gps_new_time, bool FromVias, int retreivalCounter, string source, int j, double reachDis, string route_name, int user_id, string All_Nos, int _RouteId)
        {
            this.startThread = false;

            // **1️⃣ Collect all stop locations for the route**
            List<string> destinations = new List<string>();
            for (int i = 0; i < j; i++)
            {
                destinations.Add(this.stopArray[i].GeoLocation.Latitude.ToString() + "," + this.stopArray[i].GeoLocation.Longitude.ToString());
            }

            // **2️⃣ Route-wise API call for all stops**
            string etaResponse = GetEta_here(source, destinations, retreivalCounter);
            List<int> etaList = JsonConvert.DeserializeObject<List<int>>(etaResponse); // Assuming JSON List Response

            if (etaList.Count != j)
            {
                General.WriteToLogFile(route_name, user_id,
                    $"Mismatch in ETA count and stop count. ETA Count: {etaList.Count}, Stop Count: {j}. Skipping update.");

                this.startThread = true;
                return;
            }

            // **3️⃣ Update each stop with the fetched ETA**
            for (int i = 0; i < j; i++)
            {
                try
                {
                    Stop stop = this.stopArray[i];
                    if (stop.MsgSend == true && stop.IsReached == true)
                        continue;

                    stop.ETA_Default = etaList[i];

                    // **Database update for ETA**
                    Thread bs_stop_master = new Thread(new ThreadStart(() =>
                        this.DML($"UPDATE bs_stop_master SET eta='{DateTime.Now.AddMinutes(stop.ETA_Default):HH:mm:ss}' WHERE Id={stop.Id}")));
                    bs_stop_master.Start();
                    Thread.Sleep(100);
                    //Thread.Sleep(50);

                    // **4️⃣ Send Notification only if ETA <= 10 minutes**
                    bool hasVias = viaTable != null && viaTable.Rows.Count > 0;

                    // **4️⃣ Send Notification only if ETA <= configured msg time**
                    bool canSendNotificationForStop = hasVias
                        ? stop.linkNo != 0
                        : true;

                    if (stop.eta_msg >= stop.ETA_Default &&
                        stop.ETA_Default <= 10 &&
                        canSendNotificationForStop)
                    {
                        if (!stop.MsgSend)
                        {
                            General gen = new General();

                            string queryForUpcoming = $@"
        UPDATE bs_stop_master SET status = 1 
        WHERE sys_user_id = {user_id} AND route_id = {_RouteId} AND id = {stop.Id}";

                            gen.DML(queryForUpcoming);

                            List<Student> students = stop.Students;
                            string PhnNoLists = "";
                            StringBuilder student_ids = new StringBuilder();

                            foreach (Student student in students)
                            {
                                PhnNoLists += student.PhnNO + ",";
                                student_ids.Append(student.id.ToString() + ",");
                            }

                            string message = $"Dear parent, the bus for route {route_name} shall reach your stop within 10 min at {DateTime.Now.AddMinutes(10):HH:mm:ss}";

                            Thread t2 = new Thread(new ThreadStart(() => SendNotification(All_Nos + PhnNoLists, message,_RouteId)));
                            t2.Start();
                            Thread.Sleep(300);

                            General.update_stop_status(user_id, _RouteId, stop.Id);

                            General.WriteToLogFile(
                                route_name,
                                user_id,
                                hasVias
                                    ? "Message sent to stop " + stop.StopName + " using VIA flow."
                                    : "Message sent to stop " + stop.StopName + " using DIRECT fallback flow."
                            );

                            stop.MsgSend = true;
                            stop.MsgSendAt = DateTime.Now.TimeOfDay;
                        }
                    }
                }
                catch (Exception ex)
                {
                    General.WriteToLogFile(route_name, user_id, $"Error: {ex.Message}");
                }
            }

            this.startThread = true;
        }


        public string GetEta_here(string source, List<string> destinations, int counter)
        {
            try
            {
                string destinationParam = string.Join("|", destinations); // Convert list to API format
                string url = @"https://api.olamaps.io/routing/v1/distanceMatrix?origins={0}&destinations={1}&api_key=jFl3bgSWrcQGwgx1SDyepPS4sk2yAk8Cu9gj4hdz";

                string requesUri = string.Format(url, source, destinationParam);
                // Insert into Database 
                Thread tThread = new Thread(new ThreadStart(() => this.DML("insert into bs_Api_test values ('" + requesUri + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + counter + "," + _RouteId + ")")));
                tThread.Start();
                Thread.Sleep(30);
                General.WriteToLogFile1(_Route_Name, _UserID, "Route-wise ETA Fetching for all stops");

                using (WebClient wc = new WebClient())
                {
                    string downloaded_string = wc.DownloadString(requesUri);
                    hereTraffic traffic = JsonConvert.DeserializeObject<hereTraffic>(downloaded_string);

                    List<int> etaList = new List<int>();
                    foreach (var element in traffic.rows[0].elements)
                    {
                        int time = element.duration / 60; // Convert seconds to minutes
                        etaList.Add(time);
                    }

                    return JsonConvert.SerializeObject(etaList); // Return List<int> as JSON
                }
            }
            catch (Exception ex)
            {
                General.WriteToLogFile2(_Route_Name, _UserID, "Error in GetEta_here: " + ex.Message);
                return "[]"; // Return empty list if error
            }
        }


        //public string GetEta_here(string source, string des, string vias, int counter)//string lat, string lng)
        //{

        //    try
        //    {
        //        string Msgs = $"\nSource : {source},\tDestination:{des},\tVias: {vias},\tCounter: {counter},\tRoute Id :{_RouteId},\tRoute_name:{_Route_Name},\tUserId:{_UserID}";
        //        //string Msgsz = $"\nSource{source},Destination{des},Vias{vias},Counter{counter}, Route Id {_RouteId}, Route_name{_Route_Name}, UserId{_UserID}";
        //        General.WriteToLogFile10(_Route_Name, _UserID, Msgs);

        //        // string url = @"https://maps.googleapis.com/maps/api/directions/xml?key=AIzaSyCjdd0ctMsY1RQ40DfJRTXAigNR-Vu6Xlc&origin={0}&destination={1}&mode=driving&sensor=true&client=gme-nucleusmicrosystems&channel=fasttracksoft.us&alternatives=false&traffic_model=best_guess&departure_time=now&waypoints={2}";
        //        // string url = @"https://maps.googleapis.com/maps/api/directions/xml?key=AIzaSyDubQNaptgYtEEw8rNJMp0WFteKHU_PTm8&origin={0}&destination={1}&mode=driving&sensor=true&traffic_model=best_guess&departure_time=now&waypoints={2}";
        //        //string url = @"https://wse.ls.hereapi.com/2/findsequence.json?apiKey=w0o6WRp3JtOToGgN7sM7C6x5rDCti4AXEEBIELugXyk&start={0}&end={1}&{2}&improveFor=time&departure=now&mode=fastest;car;traffic:enabled;";
        //        //string url = @"https://wse.ls.hereapi.com/2/findsequence.json?apiKey=Mcyek67Q58w0VfZciJNnfeY6f54zg5dewC7mhAVrqOI&start={0}&end={1}&{2}&improveFor=time&departure=now&mode=fastest;car;traffic:enabled;";
        //        string url = @"https://api.olamaps.io/routing/v1/distanceMatrix?origins={0}&destinations={1}&api_key=jFl3bgSWrcQGwgx1SDyepPS4sk2yAk8Cu9gj4hdz";
        //        //string inputKey = "AIzaSyCjdd0ctMsY1RQ40DfJRTXAigNR-Vu6Xlc&";
        //        string Msg = $"\nSource : {source},\tDestination :{des},\tCounter:{counter}";

        //        // General.WriteToLogFile(_route_name, _user_id, "");
        //        //General.WriteToLogFile3(_Route_Name, _UserID, Msg);
        //        //string requesUri = string.Format(url, source, des, vias);
        //        string requesUri = string.Format(url, source, des);
        //        General.WriteToLogFile1(_Route_Name, _UserID, Msg);
        //        //  requesUri = GoogleSignedUrl.Sign(requesUri, inputKey);

        //        // Insert into Database 
        //        Thread tThread = new Thread(new ThreadStart(() => this.DML("insert into bs_Api_test values ('" + requesUri + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + counter + "," + _RouteId + ")")));
        //        tThread.Start();
        //        Thread.Sleep(30);
        //        using (WebClient wc = new WebClient())
        //        {
        //            string downloaded_string = wc.DownloadString(requesUri);

        //            hereTraffic traffic = JsonConvert.DeserializeObject<hereTraffic>(downloaded_string);
        //            int time = traffic.rows[0].elements[0].duration;
        //            time = time / 60;
        //          //  int time = traffic.results[0].timeBreakdown.driving;
        //            //Thread tThread = new Thread(new ThreadStart(() => this.DML("insert into bs_Api_test values ('" + requesUri + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + counter + "," + _RouteId + ")")));
        //            //tThread.Start();
        //            //Thread.Sleep(30);
        //            // Console.WriteLine(time);
        //            return time.ToString();
        //        }

        //    }

        //    catch (Exception ex) {
        //        string Msgs = $"\nSource : {source},\tDestination:{des},\tVias: {vias},\tCounter: {counter},\tRoute Id :{_RouteId},\tRoute_name:{_Route_Name},\tUserId:{_UserID}";
        //        //string Msgsz = $"\nSource{source},Destination{des},Vias{vias},Counter{counter}, Route Id {_RouteId}, Route_name{_Route_Name}, UserId{_UserID}";
        //        General.WriteToLogFile2(_Route_Name, _UserID, Msgs +"--------"+ ex.Message);
        //            return null; }
        //                        }

        public double GetDistance(string slat, string slng, string elat, string elon)
        {
            var sCoord = new GeoCoordinate(Convert.ToDouble(slat), Convert.ToDouble(slng));
            var eCoord = new GeoCoordinate(Convert.ToDouble(elat), Convert.ToDouble(elon));
            var dis = Math.Round(sCoord.GetDistanceTo(eCoord), 2);
            return dis;
        }


        public static string MidPoint(string slat, string slng, string elat, string elon)
        {

            double lat1 = (Math.PI / 180) * Convert.ToDouble(slat);
            double lng1 = (Math.PI / 180) * Convert.ToDouble(slng);
            double lat2 = (Math.PI / 180) * Convert.ToDouble(elat);
            double lng2 = (Math.PI / 180) * Convert.ToDouble(elon);

            double dlon = lng2 - lng1;

            double bx = Math.Cos(lat2) * Math.Cos(dlon);
            double by = Math.Cos(lat2) * Math.Sin(dlon);

            double lat3 = Math.Atan2(Math.Sin(lat1) + Math.Sin(lat2), Math.Sqrt((Math.Cos(lat1) + bx) * (Math.Cos(lat1) + bx) + by * by));
            double lng3 = lng1 + Math.Atan2(by, Math.Cos(lat1) + bx);

            lat3 = lat3 * (180 / Math.PI);
            lng3 = lng3 * (180 / Math.PI);

            return lat3.ToString() + "," + lng3.ToString();
        }



        //Edit by Tanya.................................because parents are getting large number of notifications.

        //public void SendNotification(string mobiles, string msg)
        //{
        //    // EtaPredict p = new EtaPredict();
        //    try
        //    {

        //        string[] mobileNOs = mobiles.Split(',');
        //        string address = "https://fcm.googleapis.com/fcm/send";
        //        DataTable dt = null;

        //        for (int i = 0; i < mobileNOs.Length - 1; i++)
        //        {
        //            try
        //            {
        //                using (WebClient wc = new WebClient())
        //                {
        //                    wc.Headers.Add("Content-Type", "application/json");
        //                    wc.Headers.Add("Authorization", "key=AAAAXlE6GGI:APA91bE77NMjJQ-2WhpzV3TVnqReCN1T54hqT0WCgk89_Qb-x4eQjb2qxTkg1c3hrOgWCEGyV-vzmcBKSzhSrfOksan86XaGY3sjqrx4MbnUW6iXjihKoF4TC5PZs5TUn02ya-8YSdRL");
        //                    dt = SelectQuery("select auid,iuid from bs_user_master where bs_user_name='" + mobileNOs[i] + "'");
        //                    if (dt.Rows.Count != 0)
        //                    {
        //                        // string uri = string.Format(address, msg, Convert.ToString(dt.Rows[0]["auid"]));

        //                        //  string s = "{\"to\":\"cxqNbSPj4U8:APA91bH9o-5C3jfofETp665qMKiy4JkdbmKpVX_b8tJkJheG9PRNLCy_OfiGrzHXSeisk45Ze91HVtbxQAMNTCaz7-J-qWn4HiyOH3MjcJvNFOuHpMfExO1fSNmOTtlxWXoIOE06XiqK\",\"notification\": {\"body\": \"Hello mobile\"}}";
        //                        //   request = WebRequest.Create(uri) as HttpWebRequest;
        //                        if (!string.IsNullOrEmpty(dt.Rows[0]["auid"].ToString()))
        //                        {
        //                            string s = wc.UploadString(address, "{\"to\":\"" + dt.Rows[0]["auid"].ToString() + "\",\"notification\": {\"body\": \"" + msg + "\"}}");
        //                            General.WriteToLogFile("result1", 000, s);
        //                        }
        //                        if (!string.IsNullOrEmpty(dt.Rows[0]["iuid"].ToString()))
        //                        {
        //                            string a = wc.UploadString(address, "{\"to\":\"" + dt.Rows[0]["iuid"].ToString() + "\",\"notification\": {\"body\": \"" + msg + "\"}}");
        //                            General.WriteToLogFile("result2", 000, a);
        //                        }
        //                        Thread t = new Thread(new ThreadStart(() => this.DML("insert into bs_notification_parent(parent_id,message,date_time,Route_id) values ((select id from bs_user_master where bs_user_name='" + mobileNOs[i] + "') ,'" + msg + "',GETDATE()," + _RouteId + ")")));  //technical
        //                        t.Start();
        //                        //shubham
        //                        Thread.Sleep(100);
        //                    }
        //                }
        //            }

        //            catch (Exception ex)
        //            {

        //                General.WriteToLogFile("Notification_Exp1", 000, ex.Message);

        //                SendNotificationLatestApi(mobileNOs[i], msg);


        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        General.WriteToLogFile("Notification_Exp2", 000, e.Message);
        //        //SendNotificationLatestApi(mobiles, msg);
        //    }
        //}

        //Edit by Gaurav Pandit.................................because parents are getting large number of notifications.

        //public void SendNotification(string mobiles, string msg)
        //{
        //    try
        //    {
        //        string[] mobileNOs = mobiles.Split(',');
        //        DataTable dt = null;
        //        Firebase firebase = new Firebase();

        //        for (int i = 0; i < mobileNOs.Length - 1; i++)
        //        {
        //            try
        //            {
        //                dt = SelectQuery($"SELECT auid, iuid FROM bs_user_master WHERE bs_user_name='{mobileNOs[i]}'");

        //                if (dt.Rows.Count != 0)
        //                {
        //                    string auid = dt.Rows[0]["auid"].ToString();
        //                    string iuid = dt.Rows[0]["iuid"].ToString();

        //                    if (!string.IsNullOrEmpty(auid))
        //                    {
        //                        Task.Run(() => firebase.FirebaseNotifications(auid, msg, mobileNOs[i]));
        //                        General.WriteToLogFile("Firebase Notification Sent to AUID", 000, auid);
        //                    }

        //                    if (!string.IsNullOrEmpty(iuid))
        //                    {
        //                        Task.Run(() => firebase.FirebaseNotifications(iuid, msg, mobileNOs[i]));
        //                        General.WriteToLogFile("Firebase Notification Sent to IUID", 000, iuid);
        //                    }

        //                    //  Fetch student_id from bs_student_master_backup using mobile number
        //                    string getStudentIdQuery = $"SELECT TOP 1 id FROM bs_student_master_backup WHERE mobile_no1 = '{mobileNOs[i]}'";
        //                    object studentIdObj = General.ExecuteScalar(getStudentIdQuery);
        //                    int studentId = studentIdObj != null ? Convert.ToInt32(studentIdObj) : 0;

        //                    //Thread t = new Thread(new ThreadStart(() =>
        //                    //    this.DML($"INSERT INTO bs_notification_parent(parent_id, message, date_time, Route_id) " +
        //                    //             $"VALUES ((SELECT id FROM bs_user_master WHERE bs_user_name='{mobileNOs[i]}'), " +
        //                    //             $"'{msg}', GETDATE(), {_RouteId})")
        //                    //));

        //                    //  Insert notification with student_id also
        //                    Thread t = new Thread(new ThreadStart(() =>
        //                        this.DML($"INSERT INTO bs_notification_parent(parent_id, message, date_time, Route_id, student_id) " +
        //                                 $"VALUES ((SELECT id FROM bs_user_master WHERE bs_user_name='{mobileNOs[i]}'), " +
        //                                 $"'{msg}', GETDATE(), {_RouteId}, {studentId})")
        //                    ));

        //                    t.Start();
        //                    Thread.Sleep(100);
        //                    //Thread.Sleep(50);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                General.WriteToLogFile("Notification_Exp1", 000, ex.Message);
        //                SendNotificationLatestApi(mobileNOs[i], msg);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        General.WriteToLogFile("Notification_Exp2", 000, e.Message);
        //    }
        //}

        public void SendNotification(string mobiles, string msg, int route_id)
        {
            try
            {
                string[] mobileNOs = mobiles.Split(',');
                DataTable dt = null;
                Firebase firebase = new Firebase();

                for (int i = 0; i < mobileNOs.Length - 1; i++)
                {
                    try
                    {
                        dt = SelectQuery($"SELECT auid, iuid FROM bs_user_master WHERE bs_user_name='{mobileNOs[i]}'");


                        if (dt.Rows.Count != 0)
                        {

                            //string auid = dt.Rows[0]["auid"].ToString();
                            //string iuid = dt.Rows[0]["iuid"].ToString();

                            //if (!string.IsNullOrEmpty(auid))
                            //{
                            //    Task.Run(() => firebase.FirebaseNotifications(auid, msg, mobileNOs[i]));
                            //    General.WriteToLogFile("Firebase Notification Sent to AUID", 000, auid);
                            //}

                            //if (!string.IsNullOrEmpty(iuid))
                            //{
                            //    Task.Run(() => firebase.FirebaseNotifications(iuid, msg, mobileNOs[i]));
                            //    General.WriteToLogFile("Firebase Notification Sent to IUID", 000, iuid);
                            //}
                            DataTable dtfcm = SelectQuery($@"
SELECT DISTINCT
    CAST(pl.fcm AS VARCHAR(MAX)) AS fcm
FROM bs_user_master um
INNER JOIN bs_parent_login_log pl
    ON pl.bs_user_id = um.id
WHERE um.bs_user_name = '{mobileNOs[i].Replace("'", "''")}'
  AND pl.fcm IS NOT NULL
  AND LTRIM(RTRIM(CAST(pl.fcm AS VARCHAR(MAX)))) <> ''
");

                            if (dtfcm != null && dtfcm.Rows.Count > 0)
                            {
                                foreach (DataRow row in dtfcm.Rows)
                                {
                                    string fcmToken = row["fcm"]?.ToString()?.Trim();

                                    if (!string.IsNullOrWhiteSpace(fcmToken))
                                    {
                                        string currentToken = fcmToken;
                                        string currentMobile = mobileNOs[i];

                                        Task.Run(() =>
                                            firebase.FirebaseNotifications(
                                                currentToken,
                                                msg,
                                                currentMobile
                                            )
                                        );

                                        General.WriteToLogFile(
                                            "Firebase Notification Sent to FCM",
                                            0,
                                            currentToken
                                        );
                                    }
                                }
                            }
                            //  Fetch student_id from bs_student_master_backup using mobile number
                            //string getStudentIdQuery = $"SELECT TOP 1 id FROM bs_student_master_backup WHERE mobile_no1 = '{mobileNOs[i]}'";
                            string getStudentIdQuery = $"SELECT Top 1 bsmb.id FROM bs_student_master_backup bsmb inner join bs_route_students brs on bsmb.id=brs.student_id wHERE mobile_no1 = '{mobileNOs[i]}' and brs.route_id ={route_id}";
                            object studentIdObj = General.ExecuteScalar(getStudentIdQuery);
                            int studentId = studentIdObj != null ? Convert.ToInt32(studentIdObj) : 0;

                            //Thread t = new Thread(new ThreadStart(() =>
                            //    this.DML($"INSERT INTO bs_notification_parent(parent_id, message, date_time, Route_id) " +
                            //             $"VALUES ((SELECT id FROM bs_user_master WHERE bs_user_name='{mobileNOs[i]}'), " +
                            //             $"'{msg}', GETDATE(), {_RouteId})")
                            //));

                            //  Insert notification with student_id also
                            Thread t = new Thread(new ThreadStart(() =>
                                this.DML($"INSERT INTO bs_notification_parent(parent_id, message, date_time, Route_id, student_id) " +
                                         $"VALUES ((SELECT id FROM bs_user_master WHERE bs_user_name='{mobileNOs[i]}'), " +
                                         $"'{msg}', GETDATE(), {_RouteId}, {studentId})")
                            ));

                            t.Start();
                            Thread.Sleep(100);
                            //Thread.Sleep(50);
                        }
                    }
                    catch (Exception ex)
                    {
                        General.WriteToLogFile("Notification_Exp1", 000, ex.Message);
                        SendNotificationLatestApi(mobileNOs[i], msg);
                    }
                }
            }
            catch (Exception e)
            {
                General.WriteToLogFile("Notification_Exp2", 000, e.Message);
            }
        }


        public void SendNotificationLatestApi(string mobile_no, string msg)
        {
            // EtaPredict p = new EtaPredict();
            try
            {


                string address = "https://fcm.googleapis.com/fcm/send";
                DataTable dt = null;
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("Content-Type", "application/json");
                        // wc.Headers.Add("Authorization", "key=AAAAXlE6GGI:APA91bE77NMjJQ-2WhpzV3TVnqReCN1T54hqT0WCgk89_Qb-x4eQjb2qxTkg1c3hrOgWCEGyV-vzmcBKSzhSrfOksan86XaGY3sjqrx4MbnUW6iXjihKoF4TC5PZs5TUn02ya-8YSdRL");
                        wc.Headers.Add("Authorization", "key=AAAAz8CrUj8:APA91bEyilcDNxGqjiXP.0FGUV4bR-DG4wxjGPIHYLDBBNtV21L0eZlk_KQBoNCtxBj7nCKLAQ0LWmzAVr5lBbjf2JqmEG4CSF5OTS-PjvGhlh1RX0zmmpfsEsQTe77UPFXtslJzqqxM8O");

                        dt = SelectQuery("select auid,iuid from bs_user_master where bs_user_name='" + mobile_no + "'");
                        if (dt.Rows.Count != 0)
                        {
                            // string uri = string.Format(address, msg, Convert.ToString(dt.Rows[0]["auid"]));

                            //  string s = "{\"to\":\"cxqNbSPj4U8:APA91bH9o-5C3jfofETp665qMKiy4JkdbmKpVX_b8tJkJheG9PRNLCy_OfiGrzHXSeisk45Ze91HVtbxQAMNTCaz7-J-qWn4HiyOH3MjcJvNFOuHpMfExO1fSNmOTtlxWXoIOE06XiqK\",\"notification\": {\"body\": \"Hello mobile\"}}";
                            //   request = WebRequest.Create(uri) as HttpWebRequest;
                            if (!string.IsNullOrEmpty(dt.Rows[0]["auid"].ToString()))
                            {
                                string s = wc.UploadString(address, "{\"to\":\"" + dt.Rows[0]["auid"].ToString() + "\",\"notification\": {\"body\": \"" + msg + "\"}}");
                                General.WriteToLogFile("result1", 000, s);
                            }
                            if (!string.IsNullOrEmpty(dt.Rows[0]["iuid"].ToString()))
                            {
                                wc.UploadString(address, "{\"to\":\"" + dt.Rows[0]["iuid"].ToString() + "\",\"notification\": {\"body\": \"" + msg + "\"}}");
                            }
                            Thread t = new Thread(new ThreadStart(() => this.DML("insert into bs_notification_parent(parent_id,message,date_time) values ((select id from bs_user_master where bs_user_name='" + mobile_no + "') ,'" + msg + "',GETDATE())")));
                            t.Start();
                            Thread.Sleep(100);
                        }
                    }
                }

                catch (Exception ex)
                {

                    General.WriteToLogFile("error", 000, ex.Message);

                }

            }
            catch (Exception e)
            {
                General.WriteToLogFile("Notification_Exp", 000, e.Message);
            }
        }

       
        public static void SendMessage(string msg, string mobile)
        {
            try
            {
                string html = "";
                string url = "http://smsby2.in/sendsms.php?username=atlanta&password=atlanta@123&sender=ATLNTA&mobile=" + mobile + "&message=" + msg + "&route=T";


                // string url = @"http://smsBy2.in/sendsms.php?username=abctrq&password=45719&message=" + msg + "&sender=ABCTRQ&numbers=" + mobile;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader rdr = new StreamReader(stream);
                html = rdr.ReadToEnd();
            }
            catch { }
        }

        public bool startEngine()
        {
            EngineStart = DateTime.Now;
            
                 int user_id = _UserID;
            DataTable dt1 = new DataTable();
            string schoolLoc = "";
            string busLoc = "";
            string start_time = "";
            string timeNow = "";


            //DataTable odata = SelectQuery("select school_lat,school_lng from users where Id=" + user_id);
            //if (odata != null)
            //{
            //    if (odata.Rows.Count > 0)
            //    {
            //        schoolLoc = odata.Rows[0]["school_lat"].ToString() + "," + odata.Rows[0]["school_lng"].ToString();

            //    }
            //}

            try
            {
                // 1️⃣ Try from Trackofy first
                DataTable odata = SelectQuery("select school_lat, school_lng from atltracking.dbo.tbl_users where id = " + user_id);

                // 2️⃣ Fallback to Abctrack (if null or no rows)
                if (odata == null || odata.Rows.Count == 0)
                {
                    odata = SelectQuery("select school_lat, school_lng from users where id = " + user_id);
                }

                // 3️⃣ Assign schoolLoc if valid data found
                if (odata != null && odata.Rows.Count > 0)
                {
                    schoolLoc = odata.Rows[0]["school_lat"].ToString() + "," + odata.Rows[0]["school_lng"].ToString();
                }
            }
            catch (Exception ex)
            {
                General.WriteToLogFile("SchoolLocErr", user_id, ex.Message);
            }

            DataTable dt = SelectQuery("select start_time_up from bs_route_master where Id=" + _RouteId);
            if (dt != null)
            {
                if (dt.Rows.Count > 0)
                {
                    start_time = dt.Rows[0]["start_time_up"].ToString();

                    while (true)
                    {
                        try
                        {
                            TimeSpan diff = DateTime.Now - EngineStart;
                            if (diff.TotalHours > 2.0)
                            {

                                this.DML("update bs_route_master set running=0 where id=" + _RouteId);
                                return false;
                            }
                            timeNow = DateTime.Now.ToString("HH:mm:ss");
                            int H1 = Convert.ToDateTime(start_time).Hour;
                            int H2 = Convert.ToDateTime(timeNow).Hour;
                            if (H1 > H2)
                            {
                                System.Threading.Thread.Sleep(60000);
                                continue;

                            }
                            else
                                break;
                        }
                        catch { break; }
                    }
                }
            }
            if (!string.IsNullOrEmpty(start_time))
            {
                if (Convert.ToDateTime(timeNow).Hour <= Convert.ToDateTime(start_time).Hour)
                {
                    try
                    {
                        int M1 = Convert.ToDateTime(start_time).Minute;
                        int M2 = Convert.ToDateTime(timeNow).Minute;
                        if (M2 < M1)
                        {
                            int diff = M1 - M2;
                            double millDiff = diff * 60 * 1000;
                            System.Threading.Thread.Sleep(Convert.ToInt32(millDiff));
                        }
                    }
                    catch { }
                }
            }
            //General.WriteToLogFile(_Route_Name, _UserID, "Bus Monitoring Started");
            while (true)
            {
                System.Threading.Thread.Sleep(5000);
                TimeSpan diff = DateTime.Now - EngineStart;
                if (diff.TotalHours > 2)
                {
                    // No thread
                    this.DML("update bs_route_master set running=0 where id=" + _RouteId);
                    return false;
                }
                if (dt1 != null)
                    dt1.Rows.Clear();

                //dt1 = SelectQuery("select gps_latitude,gps_longitude,gps_time,i2 from latest_telemetry where sys_service_id=" + _serviceId);
                //try
                //{
                //    DateTime gps_time = Convert.ToDateTime(dt1.Rows[0]["gps_time"]).AddMinutes(330);
                //    if ((DateTime.Now - gps_time).TotalMinutes > 3)
                //    {
                //        continue;

                //    }
                //    busLoc = dt1.Rows[0]["gps_latitude"].ToString() + "," + dt1.Rows[0]["gps_longitude"].ToString();
                //}
                //catch
                //{
                //    continue;
                //}

                try
                {
                    // 1️⃣ First try with Trackofy DB
                    dt1 = SelectQuery("select gps_latitude, gps_longitude, gps_time, i2 from atltracking.dbo.tbl_latest_telemetry where sys_service_id=" + _serviceId);

                    // 2️⃣ Fallback to Abctrack DB
                    if (dt1 == null || dt1.Rows.Count == 0)
                    {
                        dt1 = SelectQuery("select gps_latitude, gps_longitude, gps_time, i2 from latest_telemetry where sys_service_id=" + _serviceId);
                    }

                    // 3️⃣ Proceed if data is available
                    if (dt1 != null && dt1.Rows.Count > 0)
                    {
                        DateTime gps_time = Convert.ToDateTime(dt1.Rows[0]["gps_time"]).AddMinutes(330);
                        double gpsDelay = (DateTime.Now - gps_time).TotalMinutes;

                        if (gpsDelay > 3)
                        {
                            General.WriteToLogFile(
                                _Route_Name,
                                _UserID,
                                $"StartEngine waiting: GPS stale. GPS IST={gps_time:yyyy-MM-dd HH:mm:ss}, Delay={gpsDelay:N0} minutes"
                            );
                            continue;
                        }

                        busLoc = dt1.Rows[0]["gps_latitude"].ToString() + "," + dt1.Rows[0]["gps_longitude"].ToString();
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    General.WriteToLogFile("LatestTelemetryErr", _UserID, ex.Message);
                    continue;
                }
                try
                {
                    int igniton = Convert.ToInt32(dt1.Rows[0]["i2"]);
                    if (igniton == 0)
                    {
                        General.WriteToLogFile(
                            _Route_Name,
                            _UserID,
                            "StartEngine waiting: Ignition i2 is 0"
                        );
                        continue;
                    }
                    else
                    {

                        string slat = busLoc.Substring(0, busLoc.IndexOf(',')).Replace(',', ' ').Trim();
                        string slon = busLoc.Substring(busLoc.IndexOf(',') + 1, busLoc.Length - busLoc.IndexOf(',') - 1).Replace(',', ' ').Trim();
                        string elat = schoolLoc.Substring(0, schoolLoc.IndexOf(',')).Replace(',', ' ').Trim();
                        string elon = schoolLoc.Substring(schoolLoc.IndexOf(',') + 1, schoolLoc.Length - schoolLoc.IndexOf(',') - 1).Replace(',', ' ').Trim();
                        //   double dis = GetDistance("28.5662266666667", "77.1983866666667", elat, elon);
                        double dis = GetDistance(slat,slon, elat, elon);
                        string s = MidPoint(slat, slon, elat, elon);
                        General.WriteToLogFile(
    _Route_Name,
    _UserID,
    $"StartEngine Check => BusLoc={busLoc}, SchoolLoc={schoolLoc}, Distance={dis:N2}, Ignition={igniton}"
);

                        if (dis < 200)
                        {
                            General.WriteToLogFile(
                                _Route_Name,
                                _UserID,
                                "StartEngine waiting: Bus is within 200 meters of school"
                            );
                            continue;
                        }
                        else
                        {
                            General.WriteToLogFile(_Route_Name, _UserID, "Dis is " + dis + ". Engine Started condition passed.");
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    General.WriteToLogFile(_Route_Name, _UserID, e.Message);
                    return true;
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        public DataTable SelectQuery(string query)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(this.cs1))
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

                        General.WriteToLogFile(e.Message, AppDomain.CurrentDomain.BaseDirectory, "db.txt");
                        return null;
                    }
                    finally
                    {
                        conn.Close(); conn.Dispose();
                        SqlConnection.ClearPool(conn);

                    }

                }
            }
            catch (SqlException se)
            {
                General.WriteToLogFile(se.Message, AppDomain.CurrentDomain.BaseDirectory, "db.txt");
                return null;

            }
        }


        public void DML(string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(this.cs1))
                {

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                        //  connection.Open();
                        //  cmd.CommandTimeout = 1000;
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (SqlException se)
                        {
                            General.WriteToLogFile(se.Message, AppDomain.CurrentDomain.BaseDirectory, "db.txt");

                        }

                        finally { connection.Close(); connection.Dispose(); SqlConnection.ClearPool(connection); }
                        //  Console.WriteLine("data inserted");
                    }
                }
            }
            catch (SqlException se)
            {
                General.WriteToLogFile(se.Message, AppDomain.CurrentDomain.BaseDirectory, "db.txt");

            }
        }
    }
}



