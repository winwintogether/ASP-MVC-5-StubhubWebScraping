﻿
namespace StubHubScraper.Web.Models
{
    public partial class EventChartModel
    {
        public int eventId { get; set; }
        public decimal average { get; set; }
        public int volume { get; set; }
        public string date { get; set; }
        public int totalTickets { get; set; }
        public decimal max { get; set; }
    }
}
