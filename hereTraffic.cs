using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIS_Engine
{
    class hereTraffic
    {
        //public List<hereResult> results { get; set; }
        //public List<object> errors { get; set; }
        //public string processingTimeDesc { get; set; }
        //public string responseCode { get; set; }
        //public object warnings { get; set; }
        //public object requestId { get; set; }

        public List<row> rows { get; set; }
        public string status { get; set; }

    }

    public class Waypoint
    {
        public string id { get; set; }
        public double lat { get; set; }
        public double lng { get; set; }
        public int sequence { get; set; }
        public DateTime? estimatedArrival { get; set; }
        public DateTime? estimatedDeparture { get; set; }
        public List<object> fulfilledConstraints { get; set; }
    }



    public class row
    {
        public List<elements> elements { get; set; }
    }
    public class elements
    {
        public int duration { get; set; }  // in seconds
        public int distance { get; set; }  // in meters
        public string polyline { get; set; }
        public string status { get; set; }
    }

    public class Durations
    {
       
    }


    public class Interconnection
    {
        public string fromWaypoint { get; set; }
        public string toWaypoint { get; set; }
        public double distance { get; set; }
        public double time { get; set; }
        public double rest { get; set; }
        public double waiting { get; set; }
    }

    public class TimeBreakdown
    {
        public int driving { get; set; }
        public int service { get; set; }
        public int rest { get; set; }
        public int waiting { get; set; }
    }

    public class hereResult
    {
        public List<Waypoint> waypoints { get; set; }
        public string distance { get; set; }
        public string time { get; set; }
        public List<Interconnection> interconnections { get; set; }
        public string description { get; set; }
        public TimeBreakdown timeBreakdown { get; set; }
    }

    
}
