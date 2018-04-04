using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Shared
{
    public class LogEntityFactory
    {
        private readonly Random _random;
        private readonly List<string> _users;
        private readonly List<string> _httpMethods;
        private readonly List<string> _knownPaths;

        public LogEntityFactory()
        {
            // TODO: allow configuration of IP addresses, HttpPaths etc.
            _random = new Random();
            _users = new List<string>
            {
                @"page\jimmy", @"plant\robert", @"jones\john", @"bonham\john",
                @"mercury\freddie", @"may\brian", @"taylor\roger", @"deacon\john"
            };
            _httpMethods = new List<string> { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
            _knownPaths = new List<string>
            {
                "/api/v1/Customers", "/api/v1/Orders", "/api/v1/Invoices", "/api/v1/Users", "/api/v1/Products",
                "/api/v2/Customers", "/api/v2/Orders", "/api/v2/Invoices", "/api/v2/Users", "/api/v2/Products",
            };
        }

        public LogEntity GetRandomLogEntity()
        {
            // TODO: make it a REAL IP address (e.g.: without the possibility of hitting 255.255.255.255 or 0.0.0.0
            // and with the real constraints in the RFC.
            string ipAddress = $"{_random.Next(256)}.{_random.Next(256)}.{_random.Next(256)}.{_random.Next(256)}";

            // We assume here that 50% of users are authenticated, 50% are not. Out of those authenticated, we pick a
            // random name from the list of users.
            string user = _random.Next(100) > 50 ?
                _users[_random.Next(_users.Count - 1)] :
                "-";

            // TODO: model this to be a REAL distribution (for most websites, GET requests are most common).
            string httpMethod = _httpMethods[_random.Next(_httpMethods.Count - 1)];

            // Here we assume that we have a web API that results in the following:
            // * 75% 200
            // * 20% 401
            // * 1% 404 (web crawlers?)
            // * 1% 400
            // * 1% 302
            // * 1% 500 (our monitoring is broken which is why I am building the other tool that is supposed to parse :) )
            HttpStatusCode code;
            int value = _random.Next(100);
            if (value < 75)
            {
                code = HttpStatusCode.OK;
            }
            else if (value < 95)
            {
                code = HttpStatusCode.Unauthorized;
            }
            else if (value < 96)
            {
                code = HttpStatusCode.NotFound;
            }
            else if (value < 97)
            {
                code = HttpStatusCode.BadRequest;
            }
            else if (value < 98)
            {
                code = HttpStatusCode.Redirect;
            }
            else
            {
                code = HttpStatusCode.InternalServerError;
            }

            // We generate traffic according to the following pattern:
            // * 90% of traffic goes to any of the endpoints
            // * 10% goes to 404s
            var path = _random.Next(100) < 90 ?
                _knownPaths[_random.Next(_knownPaths.Count - 1)] :
                "/xyzSPAMxyz";
                

            return new LogEntity
            {
                IpAddress = ipAddress,
                ClientIdentity = "-",
                UserId = user,
                Time = DateTime.Now,
                HttpMethod = httpMethod,
                HttpPath = path,
                HttpVersion = "1.0",
                StatusCode = code,
                ResponseSize = 256
            };
        }

        public LogEntity ParseLogEntity(string input)
        {
            // TODO: parse a log line to an entity
            return null;
        }
    }
}
