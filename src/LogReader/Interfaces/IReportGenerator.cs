using System;
using System.Collections.Generic;
using System.Text;

namespace LogReader.Interfaces
{
    /// <summary>
    /// An interface for a datastore that is able to generate reports
    /// regarding log events.
    /// </summary>
    public interface IReportGenerator
    {
        void AddLogItem();

        void GenerateSummaryReport();

        void GenerateAlertReport();
    }
}
