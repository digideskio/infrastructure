﻿using Infrastructure.Logging.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Logging.Services.Implementation
{
    public class LogReader : ILogReader
    {
        public LogReader(ILogContext context)
        {
            Context = context;
        }

        ILogContext Context { get; }

        public async Task<bool> HandleAsync(LogQuery e)
        {
            e.Entries = await (from entry in Context.Entries.Query()
                               where entry.LoggedAt > e.LoggedAfter
                               where entry.LoggedAt < e.LoggedBefore
                               orderby entry.LoggedAt descending
                               select new LogEntry
                               {
                                   UserId = entry.UserId,
                                   LoggedAt = entry.LoggedAt,
                                   Text = entry.Text
                               })
                   .Skip(e.PageSize * (e.Page - 1))
                   .Take(e.PageSize)
                   .ToArrayAsync();

            return true;
        }
    }
}
