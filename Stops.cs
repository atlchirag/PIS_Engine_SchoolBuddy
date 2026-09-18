using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIS_Engine
{
    public class Student
    {
        public int id { get; set; }
        public int StopId { get; set; }
        public string PhnNO { get; set; }
    }
    class GeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
    class Stop
    {
        public int Id { get; set; }
        public string StopName { get; set; }
        public GeoLocation GeoLocation {get;set;}
        public int StopOrder { get; set; }
        public string PlaceId { get; set; }
        public int ETA_Default { get; set; }
        public TimeSpan ETA_Traffic_BestGuess { get; set; }
        public TimeSpan ETA_Traffic_Pessimistic { get; set; }
        public TimeSpan ETA_Traffic_Optimistic { get; set; }
        public bool IsReached { get; set; }
        public int eta_msg { get; set; }
        public double Distance { get; set; }
        public int TripId { get; set; }
        public string Distance_KM { get; set; }
        public string ETA_Text { get; set; }
        public List<GeoLocation> WayPoints { get; set; }
        public Student student { get; set; }
        public List<Student> Students { get; set; }
        public bool MsgSend { get; set; }
        public int linkNo { get; set; }
        public TimeSpan MsgSendAt { get; set; }
        public bool RemMsgSend { get; set; }

        // Absolute clock time the bus is predicted to reach this stop, fixed by the one and only
        // distance-matrix call of the trip. Every later cycle compares this against DateTime.Now
        // instead of asking the API again.
        public DateTime PredictedArrival { get; set; }
        public bool EtaFetched { get; set; }
      //  public string RFIf { get; set; }
    }
}
