using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using LogReader.Interfaces;
using Shared;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LogReader.Implementations
{

    /// <summary>
    /// A class that accepts events and can then generate summary-s and alerts
    /// at 10 second / 2 minute intervals.
    /// 
    /// Internally, the functionality is split into two items:
    /// * we do a sliding time window for the 10 second report
    /// * we do another sliding time window for the 2 minute alert
    /// 
    /// To generate the sliding time windows, we make use of a concurrent queue (one for each).
    /// Thus, the file-reading thread can keep adding items while we generate the reports.
    /// </summary>
    public class TenSecondTwoMinuteReportGenerator : IReportGenerator
    {
        const int tenSecondInterval = 10;
        const int twoMinuteInterval = 2;
        const int ALERT_NUMBER = 10;
        const string Padding = "                       ";

        private readonly ConcurrentQueue<DateTime> twoMinQueue;
        private readonly ConcurrentQueue<LogEntity> tenSecondQueue;
        private readonly ILogger _logger;

        public TenSecondTwoMinuteReportGenerator()
        {
            tenSecondQueue = new ConcurrentQueue<LogEntity>();
            twoMinQueue = new ConcurrentQueue<DateTime>();
            _logger = Program.LoggerFactory.CreateLogger<TenSecondTwoMinuteReportGenerator>();
        }

        /// <summary>
        /// Adds a LogEntity to the report.
        /// </summary>
        /// <param name="logEntity"></param>
        public void AddLogItem(LogEntity logEntity)
        {
            tenSecondQueue.Enqueue(logEntity);
            twoMinQueue.Enqueue(logEntity.Time);
        }

        private int previousTwoMinuteElementCount = 0;

        /// <summary>
        /// This report looks back at all the events in the queue in the past
        /// twoMinuteInterval, counts them and triggers an alert if necessary.
        /// </summary>
        public void GenerateAlertReport(DateTime previousInterval, DateTime timeMax)
        {
            // In the twoMinuteQueue, we keep track of just the timestamps of the events,
            // as we are only interested in the count of events in a particular time range.
            // 
            // Normally, this would mean that:
            // * we remove elements that are before the time range,
            // * we count the elements inside the time range and 
            // * we stop when we encounter an element after the time range.
            //
            // But, this is inefficient: each 10s interval, we count the elements for
            // the 110s over and over again, while in reality we are only interested in three things:
            // * how many elements were there the last time we counted.
            // * how many elements are in the list before previousInterval (and that we remove)
            // * how many new elements are in the latest 10 second interval

            bool shouldContinue = true;
            int count = 0;
            int elementsRemoved = 0;

            // First we cound and remove elements before previousInterval
            while (shouldContinue)
            {
                if (twoMinQueue.TryPeek(out DateTime eventTime))
                {
                    // old events we remove
                    if (eventTime < previousInterval)
                    {
                        twoMinQueue.TryDequeue(out DateTime _);
                        elementsRemoved++;
                    }
                    else
                    {
                        // we got to the events in the current interval
                        shouldContinue = false;
                    }
                }
                else
                {
                    shouldContinue = false;
                }
            }

            // We compute where we need to jump so we are at timeMax - 10 seconds
            var elementsToSkip = previousTwoMinuteElementCount - elementsRemoved;
            if (elementsToSkip < 0)
            {
                elementsToSkip = 0;
            }

            // We compute how many elements are in the interval [timeMax - 10s, timeMax]
            int newElements = 0;
            foreach (var logItemTimestamp in twoMinQueue.Skip(elementsToSkip))
            {
                if (logItemTimestamp < timeMax)
                {
                    newElements++;
                }
                else
                {
                    break;
                }
            }

            count = elementsToSkip + newElements;

            _logger.LogTrace($"Queue={twoMinQueue.Count} prevCount={previousTwoMinuteElementCount}\r\n" +
                $"removed={elementsRemoved} new={newElements}\r\n" +
                $"prevInterval={previousInterval}, current={timeMax}\r\n");

            previousTwoMinuteElementCount = count;

            if (count > ALERT_NUMBER)
            {
                _logger.LogCritical(
                    "===============================\r\n" +
                    "| ALERT ALERT ALERT!!!        |\r\n" +
                    "| Events in past 2 mins: {0,5}|\r\n" +
                    "===============================\r\n",
                    count
                    );
            }

        }

        /// <summary>
        /// This report looks at all the events in the queue in the past tenSecondInterval
        /// and produces a report regarding the requests in the past 10 seconds.
        /// </summary>
        /// <param name="previousInterval"></param>
        /// <param name="timeMax"></param>
        public void GenerateSummaryReport(DateTime previousInterval, DateTime timeMax)
        {
            // In the tenSecondQueue, we keep track of entire LogEntity objects and every
            // 10 seconds, we generate a summary report from that (as you can see here and above, 10s
            // is in fact arbitrary).

            bool shouldContinue = true;
            int parsedItems = 0;

            Dictionary<string, int> paths = new Dictionary<string, int>();
            Dictionary<HttpStatusCode, int> statusCodes = new Dictionary<HttpStatusCode, int>();

            while (shouldContinue)
            {
                // if there are elements in the queue
                if (tenSecondQueue.TryPeek(out LogEntity logItem))
                {
                    // There are 3 possible cases here (we assume that previousInterval < timeMax always):
                    //
                    // 0. logItem.Time < previousInterval
                    //    These are old events, outside of our current interval.
                    //    If we are in this scenario, we can check the difference between when the event Time and when 
                    //    the event was actually read (TimeReadFromFile). The difference between those times is the time
                    //    the service took to service the request. There might be an issue here, so we alert.
                    //    Steps: Dequeue the element, if the time difference > 10s, print a warning.
                    //
                    // 1. previousInterval < logItem.Time < timeMax
                    //    These are the elements that we need to generate the report from.
                    //    Steps: Dequeue, add to the report
                    //
                    // 2. timeMax < logItem.Time
                    //    We have reached the end of the interval. Stop.

                    if (logItem.Time < previousInterval)
                    {
                        if (tenSecondQueue.TryDequeue(out LogEntity longRunningItem))
                        {
                            if (longRunningItem.TimeReadFromFile - longRunningItem.Time > TimeSpan.FromSeconds(tenSecondInterval))
                            {
                                _logger.LogWarning($"Log item has Time {longRunningItem.Time}, but was processed at {longRunningItem.TimeReadFromFile}");
                            }
                        }
                        else
                        {
                            _logger.LogCritical("Something has gone terribly wrong, the element was there just now, we peeked at it??");
                        }
                    }
                    else
                    {
                        if (logItem.Time < timeMax)
                        {
                            parsedItems++;
                            // Elements we are interested in.
                            if (tenSecondQueue.TryDequeue(out LogEntity currentItem))
                            {
                                // Here we can do any parsing logic that we want.
                                // we parse the paths
                                var segments = currentItem.HttpPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                                if (segments.Length > 0)
                                {
                                    paths.TryIncrementOrAdd(segments[0]);
                                }
                                else
                                {
                                    paths.TryIncrementOrAdd("/");
                                }

                                // Status codes
                                if ((int)currentItem.StatusCode >= 400 && (int)currentItem.StatusCode < 600)
                                {
                                    statusCodes.TryIncrementOrAdd(currentItem.StatusCode);
                                }
                            }
                            else
                            {
                                _logger.LogCritical("Something has gone terribly wrong, the element was there just now, we peeked at it??");
                            }
                        }
                        else
                        {
                            // We reached the timeMax limit, stop.
                            shouldContinue = false;
                        }
                    }
                }
                else
                {
                    // If there are no more elements in the queue, stop
                    shouldContinue = false;
                }
            }

            _logger.LogInformation($"Read {parsedItems} log lines in the interval {previousInterval} to {timeMax}");

            if (paths.Count > 0)
            {
                string toOutput = string.Empty;
                toOutput += 
                    "Requests by Path:      " +
                    "=======================\r\n" +
                    Padding + "| Item | Path | Count |\r\n" +
                    Padding + "=======================\r\n"
                    ;

                var sortedPaths = from entry in paths
                                  orderby entry.Value descending
                                  select entry;

                for (int i = 0; i < Math.Min(paths.Count, 3); i++)
                {
                    var item = sortedPaths.ElementAt(i);
                    toOutput += string.Format(Padding + "| {0,4} | {1,4} | {2,5} |\r\n", i, item.Key, item.Value);
                }
                toOutput += Padding + "=======================";

                _logger.LogInformation(toOutput);
            }

            if (statusCodes.Count > 0)
            {
                string toOutput = string.Empty;
                toOutput +=
                    "BAD STATUS CODES:      " +
                    "=======================\r\n" +
                    Padding + "| Item | Code | Count |\r\n" +
                    Padding + "=======================\r\n"
                    ;

                var sortedCodes = from entry in statusCodes
                                  orderby entry.Value descending
                                  select entry;

                for (int i = 0; i < statusCodes.Count; i++)
                {
                    var item = sortedCodes.ElementAt(i);
                    toOutput += string.Format(Padding + "| {0,4} | {1,4} | {2,5} |\r\n", i, (int)item.Key, item.Value);
                }
                toOutput += Padding + "=======================";

                _logger.LogInformation(toOutput);
            }
            else
            {
                _logger.LogInformation("No bad status codes in this interval...");
            }
        }

        public void GenerateAllReports(DateTime previousInterval, DateTime timeMax)
        {
            // For the summary report we use the time in previousInterval and timeMax, but for the alert report
            // we need to use T-2 minutes and now
            GenerateSummaryReport(previousInterval, timeMax);
            GenerateAlertReport(timeMax.Subtract(TimeSpan.FromMinutes(twoMinuteInterval)), timeMax);
        }

        /// <summary>
        /// Start a Task that will generate the reports every 10 seconds.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task StartPeriodicReportingAsync(CancellationToken ct)
        {
            DateTime previousTimestamp = DateTime.UtcNow;
            TimeSpan operationDelay = TimeSpan.FromSeconds(0);
            while (!ct.IsCancellationRequested)
            {
                // Practically, to be 99.9999% correct, we will sleep (10s - how long it took to process the previous batch).
                // This makes it such that the intervals are as close as possible to 10 seconds.
                // This also tries to mitigate some of the scenarios in which the processing would take a long time
                // (e.g.: multiple seconds) - in those cases we try to be accurate and count exactly the 10 seconds that
                // are required. Also, this can be instrumented with some Diagnostics events + monitoring to see if the algorithm
                // actually performs well or not (and in case it does not, why?)

                var timeToSleep = TimeSpan.FromSeconds(tenSecondInterval) - operationDelay;

                // In case we ever take 10+ seconds to process the events, we don't want to sleep at all, let's keep going...
                if (timeToSleep < TimeSpan.FromSeconds(0))
                {
                    timeToSleep = TimeSpan.FromSeconds(0);
                    // this is safe to do and takes no time and does no thread switching during the Task.Delay below.
                    // according to the source code: http://source.dot.net/#System.Private.CoreLib/src/System/Threading/Tasks/Task.cs,5fb80297e082b8d6 
                    // Task.Delay checks if the delay is 0 and then continues the execution.
                }

                await Task.Delay(timeToSleep, ct).ConfigureAwait(false);
                DateTime timeNow = DateTime.UtcNow;

                // We do the reporting
                GenerateAllReports(previousTimestamp, timeNow);

                // Set up so we know how far back to look next time and how long to sleep.
                previousTimestamp = timeNow;
                operationDelay = DateTime.UtcNow - timeNow;
            }
        }
    }
}
