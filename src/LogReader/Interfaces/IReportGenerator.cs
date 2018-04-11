using System;
using System.Threading;
using System.Threading.Tasks;
using Shared;

namespace LogReader.Interfaces
{
    /// <summary>
    /// An interface for a datastore that is able to generate reports
    /// regarding log events.
    /// </summary>
    public interface IReportGenerator
    {
        void AddLogItem(LogEntity logEntity);

        void GenerateSummaryReport(DateTime previousInterval, DateTime timeNow);

        void GenerateAlertReport(DateTime previousInterval, DateTime timeNow);

        void GenerateAllReports(DateTime previousInterval, DateTime timeNow);

        Task StartPeriodicReportingAsync(CancellationToken ct);
    }
}
