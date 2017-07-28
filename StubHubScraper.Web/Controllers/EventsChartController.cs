using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using StubHubScraper.Web.Filters;
using StubHubScraper.Web.Models;
using StubHubScraper.Data.Domain;
using StubHubScraper.Services;

namespace StubHubScraper.Web.Controllers
{
    //[Authorize]
    public class EventsChartController : ApiController
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IManualScrapingService _manualScrapingService;
        private readonly ILogger _logger;
        public EventsChartController(
            IAuthenticationService authenticationService,
            ILogger logger,
            IManualScrapingService manualScrapingService)
        {
            this._authenticationService = authenticationService;
            this._manualScrapingService = manualScrapingService;
            this._logger = logger;
        }

        [Queryable]
        public IQueryable<EventChartModel> GetEvents(int searchId,int eventId,string title,string venue,
            string startDate,string endDate,string zone,string sectionForm,string sectionTo,
            int LastWeekSalesOnly, int HidePastEvents, int ShowArchivedSearches)
        {
            var user = _authenticationService.GetAuthenticatedUser();
            var charts = new List<EventChartModel>();
          
            var max = 0.0M;
            try
            {
                var soldTickets = _manualScrapingService.SearchTickets(user.Id, searchId, eventId, title, venue, startDate, endDate, zone, sectionForm, sectionTo,
                    LastWeekSalesOnly, HidePastEvents, ShowArchivedSearches);
   
                var eventList = soldTickets.Select(x => x.EventId).Distinct().ToList();

                foreach (var event_Id in eventList){

                   var dateList = soldTickets.Where(x => x.EventId == event_Id).Select(x => x.DateSold.Date).Distinct().OrderBy(x => x.Date).ToList();

                    decimal total_price = 0;
                    int total_sales = 0;
   
                   foreach (var date in dateList)
                    {
                        var before_string = date.ToString("MM/dd/yyyy")+" 00:00:00";
                        var after_string = date.ToString("MM/dd/yyyy") + " 23:59:59";

                        var before = Convert.ToDateTime(before_string);
                        var after = Convert.ToDateTime(after_string);

                        var tickets = soldTickets.Where(x => x.EventId == event_Id && x.DateSold > before && x.DateSold < after);

                        decimal average = 0; int sales = 0;

                        sales = tickets.Count();


                        if (tickets.Count() != 0)
                        {
                            total_price += tickets.Sum(x => x.Qty * x.Price);
                            total_sales += tickets.Sum(x => x.Qty);

                            //average = Math.Round(tickets.Sum(x => x.Qty * x.Price) / tickets.Sum(x => x.Qty), 2);
                            average = Math.Round(total_price/total_sales, 2);
                        }

                       

                        if (max < average) max = average;
                        
                        charts.Add(new EventChartModel { eventId=event_Id, average = average, volume = sales, date = date.ToString() });
                    }
                    if (charts.Count > 0)
                    {
                        max = Math.Ceiling(max / 10) * 10;
                        var c = charts.FirstOrDefault();
                        c.max = max;
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.InsertLog(new Log { UserId = user.Id, LogLevelId = 40, Message = ex.Message, CreatedOnUtc = DateTime.Now });
            }
            return charts.AsQueryable();
        }

    }

}