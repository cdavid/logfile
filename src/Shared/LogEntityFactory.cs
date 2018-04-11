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
                "/v1/Customers", "/v1/Orders", "/v1/Invoices", "/v1/Users", "/v1/Products",
                "/v2/Customers", "/v2/Orders", "/v2/Invoices", "/v2/Users", "/v2/Products",
                "/v3/Customers", "/v3/Orders", "/v3/Invoices", "/v3/Users", "/v3/Products",
                "/v4/Customers", "/v4/Orders", "/v4/Invoices", "/v4/Users", "/v4/Products",
                "/v5/Customers", "/v5/Orders", "/v5/Invoices", "/v5/Users", "/v5/Products",
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
    }
}
