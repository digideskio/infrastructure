﻿using Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Logging.Services
{
    interface ILogReader : IHandler<LogQuery>
    {
    }
}
