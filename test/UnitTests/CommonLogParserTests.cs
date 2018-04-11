using System;
using LogReader.Implementations;
using LogReader.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared;

namespace UnitTests
{
    [TestClass]
    public class CommonLogParserTests
    {
        [TestMethod]
        public void SimpleTest()
        {
            LogEntityFactory logEntityFactory = new LogEntityFactory();
            LogEntity logEntity = logEntityFactory.GetRandomLogEntity();
            ILogParser logParser = new CommonLogParser();
            DateTime now = DateTime.UtcNow;

            try
            {
                var response = logParser.Parse(logEntity.ToString(), now);

                Assert.AreEqual(logEntity.IpAddress, response.IpAddress, "ip address");
                Assert.AreEqual(logEntity.UserId, response.UserId, "user id");
                Assert.AreEqual(logEntity.ClientIdentity, response.ClientIdentity, "client identity");
                Assert.AreEqual(logEntity.Time.ToShortTimeString(), response.Time.ToShortTimeString(), "time");
                Assert.AreEqual(logEntity.HttpMethod, response.HttpMethod, "http method");
                Assert.AreEqual(logEntity.HttpPath, response.HttpPath, "http path");
                Assert.AreEqual(logEntity.HttpVersion, response.HttpVersion, "http version");
                Assert.AreEqual(logEntity.StatusCode, response.StatusCode, "status code");
                Assert.AreEqual(logEntity.ResponseSize, response.ResponseSize, "size");
                Assert.AreEqual(now, response.TimeReadFromFile, "read from file");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
    }
}
