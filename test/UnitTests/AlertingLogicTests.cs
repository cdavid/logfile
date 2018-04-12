using System;
using System.Collections.Generic;
using System.Text;
using LogReader.Implementations;
using LogReader.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace UnitTests
{
    public class FakeLogger : ILogger
    {
        public List<string> Logs = new List<string>();

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel > LogLevel.Information)
            {
                var logLine = formatter(state, exception);
                Logs.Add(logLine);
            }
        }
    }

    [TestClass]
    public class AlertingLogicTests
    {
        [TestMethod]
        public void TestAlerting()
        {
            var fakeLogger = new FakeLogger();
            var reportGenerator = new TenSecondTwoMinuteReportGenerator(fakeLogger);
            var now = DateTime.UtcNow;

            // Add 1 event at T-3 minutes and N-2 events in the past 5 seconds.
            reportGenerator.twoMinQueue.Enqueue(now.Subtract(TimeSpan.FromMinutes(3)));
            for (int i = 0; i < TenSecondTwoMinuteReportGenerator.ALERT_NUMBER - 2; i++)
            {
                reportGenerator.twoMinQueue.Enqueue(now.Subtract(TimeSpan.FromSeconds(5)));
            }
            // Generate the report for [T-5,T] minutes, should not trigger an alert as there are
            // N-1 elements in the queue.
            reportGenerator.GenerateAlertReport(
                now.Subtract(TimeSpan.FromMinutes(5)),
                now
                );

            Assert.IsTrue(!fakeLogger.Logs.Any(x => x.Contains("alert")));

            // Then add the N-th event
            reportGenerator.twoMinQueue.Enqueue(now.Subtract(TimeSpan.FromSeconds(5)));

            // Generate the report, should trigger an alert.
            reportGenerator.GenerateAlertReport(
                now.Subtract(TimeSpan.FromMinutes(5)),
                now
                );

            Assert.IsTrue(fakeLogger.Logs.Any(x => x.Contains("alert")));

            // Clear the queue and run the report again for [T-2,T] minutes,
            // should display "...recovered" as the T-3 minutes element was removed.
            fakeLogger.Logs.Clear();
            // Generate the report, should not trigger an alert.
            reportGenerator.GenerateAlertReport(
                now.Subtract(TimeSpan.FromMinutes(2)),
                now
                );

            Assert.IsTrue(fakeLogger.Logs.Any(x => x.Contains("recovered")));
        }
    }
}
