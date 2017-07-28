﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using StubHubScraper.Web.Filters;
using StubHubScraper.Web.Models;

using StubHubScraper.Data.Domain;
using StubHubScraper.Services;


namespace StubHubScraper.Web.Controllers
{
    //[Authorize]
    public class ChartDataController : ApiController
    {
        private readonly IQuickSearchService _quickSearchService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger _logger;
        public ChartDataController(IQuickSearchService quickSearchService,
            ILogger logger,
            IAuthenticationService authenticationService)
        {
            this._quickSearchService = quickSearchService;
            this._authenticationService = authenticationService;
            this._logger = logger;
        }
      
        [Queryable]
        public IQueryable<ChartModel> GetEventChartData(int quickId)
        {
              var user = _authenticationService.GetAuthenticatedUser();

              var list=new List<ChartModel>();
              var max = 0.0M;
              try
              {
                  var chartdatas = _quickSearchService.GetTicketsChartDatas(user.Id, quickId);
                  var dateList = chartdatas.Select(x => x.DateSold.Date).Distinct().OrderBy(x => x.Date).ToList();

                decimal total_price = 0;
                int total_sales = 0;
                int total_tickets = 0;

                foreach (var date in dateList)
                  {
                      //var before = date.AddDays(-1);
                      //var after = date.AddDays(1);
                      var before_string = date.ToString("MM/dd/yyyy") + " 00:00:00";
                      var after_string = date.ToString("MM/dd/yyyy") + " 23:59:59";

                      var before = Convert.ToDateTime(before_string);
                      var after = Convert.ToDateTime(after_string);

                      var tickets = chartdatas.Where(x => x.DateSold > before && x.DateSold < after);
                      decimal average = 0; int sales = 0;

                    total_price += tickets.Sum(x => x.Qty * x.Price);
                    total_sales += tickets.Sum(x => x.Qty);

                    //average = Math.Round(tickets.Sum(x => x.Qty * x.Price) / tickets.Sum(x => x.Qty), 2);

                    average = Math.Round(total_price / total_sales, 2);

                    sales = tickets.Count();
                    total_tickets += sales;
                    
                    if (max < average) max = average;

                      list.Add(new ChartModel { average = average, volume = sales, date = date.ToString(), totaltickets = total_tickets });
                  }
                  if (chartdatas.Count > 0)
                  {
                      max = Math.Ceiling(max / 10) * 10;
                      var c = list.FirstOrDefault();
                      c.max = max;
                  }
              }
              catch (Exception ex) {
                  _logger.InsertLog(new Log { UserId = user.Id, LogLevelId = 40, Message = ex.Message, CreatedOnUtc = DateTime.Now });
              }          
              return list.AsQueryable();
        }
    }
}