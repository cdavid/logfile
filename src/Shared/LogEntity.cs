using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Shared
{
    public class LogEntity
    {
        public string IpAddress { get; set; }
        public string ClientIdentity { get; set; }
        public string UserId { get; set; }
        public DateTime Time { get; set; }
        public string HttpMethod { get; set; } /* we want to allow for custom HTTP method names here, so we leave it as string */
        public string HttpPath { get; set; }
        public string HttpVersion { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public int ResponseSize { get; set; }

        // We also want to keep track of when the log item was read from the file
        public DateTime TimeReadFromFile { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} [{3}] \"{4} {5} HTTP/{6}\" {7} {8}",
                IpAddress,
                ClientIdentity,
                UserId,
                Time.ToString("d/MMM/yyyy:hh:mm:ss zz00"),
                HttpMethod,
                HttpPath,
                HttpVersion,
                (int)StatusCode,
                ResponseSize
                );
        }
    }
}
