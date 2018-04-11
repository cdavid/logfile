using System;
using System.Collections.Generic;
using System.Text;
using LogReader.Interfaces;

namespace LogReader.Implementations
{

    /// <summary>
    /// A class that accepts events and can then generate summary-s and alerts
    /// at 10 second / 2 minute intervals
    /// </summary>
    public class TenSecondTwoMinuteReportGenerator : IReportGenerator
    {
        public void AddLogItem()
        {
            throw new NotImplementedException();
        }

        public void GenerateAlertReport()
        {
            throw new NotImplementedException();
        }

        public void GenerateSummaryReport()
        {
            throw new NotImplementedException();
        }
    }
}
