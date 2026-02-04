using System;
using System.Collections.Generic;

namespace one_db.Models
{
    public class RosterPeriod
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int TotalWeeks { get; set; }
        public int WorkWeeks { get; set; }
        
        public static List<RosterPeriod> GetPredefinedPeriods()
        {
            return new List<RosterPeriod>
            {
                new RosterPeriod { Id = "10_2", Name = "10 minggu : 2 minggu", TotalWeeks = 10, WorkWeeks = 2 },
                new RosterPeriod { Id = "6_2", Name = "6 minggu : 2 minggu", TotalWeeks = 6, WorkWeeks = 2 },
                new RosterPeriod { Id = "3_1", Name = "3 minggu : 1 minggu", TotalWeeks = 3, WorkWeeks = 1 },
                new RosterPeriod { Id = "20_10", Name = "20 hari : 10 hari", TotalWeeks = 0, WorkWeeks = 0 } // Special case for days
            };
        }
    }
}