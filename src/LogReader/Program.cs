﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LogReader.Implementations;
using LogReader.Interfaces;
using Shared;

namespace LogReader
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            //
            // Hi! Welcome to my assignment, I am Catalin David - nice to (virtually) meet you!
            //
            // Disclaimers:
            // * I understand that DataDog is a Python / Go / other non-.NET languages, so I appreciate taking
            //   the time to try to read through my code.
            // * I am making extensive use of async/await and Task/Task<T> throughout the code.
            //   These concepts are similar to Go sub-routines (the runtime takes care of creating the necessary
            //   threads and scheduling these items on those threads), Promises (in JavaScript).
            //   I found this doc to be rather informative: https://docs.microsoft.com/en-us/dotnet/csharp/async
            // * There are 3rd party libraries for .NET that implement time sliding windows, but I have considered
            //   that the intent of this assignment is not to test my knowledge of .NET libraries, but to actually
            //   implement something by myself, so I have decide to not use such libraries.
            //   That being said, if I were to write production code, I would probably not be using the current
            //   implementation. Instead, I would be using RX (Reactive eXtensions): https://github.com/Reactive-Extensions/Rx.NET
            //   This library makes it easy to operate using the Observable pattern and a time sliding window with
            //   events accummulated can be implemented using Observable.Window (to define the time window),
            //   Observable.Interval (to trigger the reporting) and an IFileReader (under Interfaces folder) that
            //   would generate the observable events. See an example of observable here: http://rxwiki.wikidot.com/101samples#toc38
            //
            //
            // Now that we are done with the disclaimers, please allow me to introduce you to my solution.
            //
            // General notes:
            // * I am testing this using the LogWriter project - this outputs data to a well-known file (~\datadog\file.log)
            //   in the common log format.
            // * I did not implement a proper logger and rely on Console.WriteLine for now (though I try to hide it behind a Log() method).
            //   such that when we implement logging, the required change is constrained.
            // * I try to use interfaces in most places as that makes it easy to unit test different components and mock out everything else.
            // * In production, we would use Dependency Injection (DI) to construct the objects and their dependencies, but I decided to not
            //   use that here (here we instantiate the objects manually).
            //
            // The structure of this project is as follows:
            // * we have a reader flow (Task) that run continuously - this reads the file and generates the necessary data.
            // * we have a reporting flow (Task) that runs every Interval and generates the report as output.
            //
            // THE READER FLOW
            // * we have an IFileReader with the implementation FileReader - this tries to read as fast as possible strings from
            //   the log file and pump the strings to an asynchronous FIFO queue (BufferBlock).
            // * .NET provides a library called TPL Dataflow (TPL = Task Parallel Library) that provides building "blocks"
            //   for data flows (these seem very similar to Go channels). In particular, I am leveraging here a BufferBlock
            //   which is an async FIFO queue. This allows us to have a producer / consumer model where the FileReader is the producer
            //   that can go back to reading without caring if the log line is parsed.
            //   This document explains the blocks that I am using:
            //   https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library#predefined-dataflow-block-types
            // * Next, we have another Dataflow block: the TransformBlock. This block takes the output of the FileReader and parses it
            //   to a C# object that represents the log event. This block is chained to the BufferBlock.
            // * Next, we have an Action block. This is a builtin block that is chained to the TransformBlock above.
            //   This block executes a method on a block - in our case, it adds the LogEvent to the IReportGenerator.
            //
            // THE REPORTING FLOW
            // * Every interval, we call into the IReportGenerator to do the following:
            //   1) generate a summary report for the last interval (10 seconds)
            //   2) generate an alerting report for the last 2 minutes.

            CancellationTokenSource cts = new CancellationTokenSource();

            ILogParser logParser = new CommonLogParser();

            var bufferBlock = new BufferBlock<Tuple<string, DateTime>>();

            var transformBlock = new TransformBlock<Tuple<string, DateTime>, LogEntity>(input =>
            {
                return logParser.Parse(input.Item1, input.Item2);
            });
            bufferBlock.LinkTo(transformBlock); // we chain the buffer block to the transform block

            var actionBlock = new ActionBlock<LogEntity>(input =>
            {

            });
            transformBlock.LinkTo(actionBlock); // we chain the transform block to the action block

            IFileReader fr = new FileReader(LogSettings.FILE_PATH, bufferBlock);


            await fr.StartReadAsync(cts.Token);
        }

        private static Tuple<LogEntity, DateTime> TransformMethod(Tuple<string, DateTime> input)
        {
            throw new NotImplementedException();
        }
    }
}
