using System;
using System.Collections.Generic;
using System.Text;
using Shared;

namespace LogReader.Interfaces
{
    public interface ILogParser
    {
        LogEntity Parse(string line, DateTime timeRead);
    }
}
