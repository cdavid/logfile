using System;
using System.Net;
using System.Text.RegularExpressions;
using LogReader.Interfaces;
using Shared;

namespace LogReader.Implementations
{
    /// <summary>
    /// A simple class that allows parsing strings to LogEntity-s in the
    /// Common Log Format: https://en.wikipedia.org/wiki/Common_Log_Format
    /// </summary>
    public class CommonLogParser : ILogParser
    {
        // Sample input:
        // 22.241.69.198 - - [4/Apr/2018:01:19:38 -0700] "GET /api/v1/Products HTTP/1.0" 400 256

        private readonly string _pattern;
        private readonly Regex _regularExpression;

        public CommonLogParser()
        {
            _pattern = "^([\\d.]+) (\\S+) (\\S+) \\[([\\w:/]+\\s[+\\-]\\d{4})\\] \"(.+?) (.+?) (.+?)/(.+?)\" (\\d{3}) (\\d+)"; //  \"([^\"]+)\" \"([^\"]+)\"
            _regularExpression = new Regex(_pattern);
        }

        public LogEntity Parse(string line, DateTime timeRead)
        {
            if (_regularExpression.IsMatch(line))
            {
                var matches = _regularExpression.Matches(line);

                string ipAddress = matches[0].Groups[1].Value;
                string clientIdentity = matches[0].Groups[2].Value;
                string userId = matches[0].Groups[3].Value;
                string timeString = matches[0].Groups[4].Value;
                DateTime time = DateTime.ParseExact(timeString, "d/MMM/yyyy:hh:mm:ss zz00", null);

                string httpMethod = matches[0].Groups[5].Value;
                string httpPath = matches[0].Groups[6].Value;
                string httpVersion = matches[0].Groups[8].Value; // 7 is HTTP in the regex
                HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), matches[0].Groups[9].Value);
                int size = int.Parse(matches[0].Groups[10].Value);

                return new LogEntity
                {
                    IpAddress = ipAddress,
                    ClientIdentity = clientIdentity,
                    UserId = userId,
                    Time = time.ToUniversalTime(),
                    HttpMethod = httpMethod,
                    HttpPath = httpPath,
                    HttpVersion = httpVersion,
                    StatusCode = statusCode,
                    ResponseSize = size,
                    TimeReadFromFile = timeRead
                };
            }
            else
            {
                throw new Exception("Unable to parse string");
            }
        }


    }
}
