using System;
using System.Collections.Generic;
using BasicWebServer.Server.HTTP;
using BasicWebServer.Server.Common;
using BasicWebServer.Server.Responses;

namespace BasicWebServer.Server.Routing
{
    public class RoutingTable : IRoutingTable
    {
        private readonly Dictionary<Method, Dictionary<string, Func<Request, Response>>> routes;

        public RoutingTable() => this.routes = new()
        {
            [Method.Get] = new(StringComparer.InvariantCultureIgnoreCase),
            [Method.Post] = new(StringComparer.InvariantCultureIgnoreCase),
            [Method.Put] = new(StringComparer.InvariantCultureIgnoreCase),
            [Method.Delete] = new(StringComparer.InvariantCultureIgnoreCase)
        };


        public IRoutingTable Map(
            Method method, 
            string path, 
            Func<Request, Response> responseFunction)
        {
            Guard.AgainstNull(path, nameof(path));
            Guard.AgainstNull(responseFunction, nameof(responseFunction));

            switch (method)
            {
                case Method.Get:
                    return MapGet(path, responseFunction);
                case Method.Post:
                    return MapPost(path, responseFunction); 
                case Method.Put:
                case Method.Delete:
                default:
                    throw new ArgumentOutOfRangeException($"The method {nameof(method)} is not supported!");
            }
        }

        private IRoutingTable MapGet(
            string path,
            Func<Request, Response> responseFunction)
        {
            Guard.AgainstDuplicatedKey(routes[Method.Get], path, "RoutingTable.Get");
            routes[Method.Get][path] = responseFunction;

            return this;
        }

        private IRoutingTable MapPost(
            string path,
            Func<Request, Response> responseFunction)
        {
            Guard.AgainstDuplicatedKey(routes[Method.Post], path, "RoutingTable.Post");
            routes[Method.Post][path] = responseFunction;

            return this;
        }

        public Response MatchRequest(Request request)
        {
            var requestMethod = request.Method;
            var requestUrl = request.Url;

            if (!this.routes.ContainsKey(requestMethod)
                || !this.routes[requestMethod].ContainsKey(requestUrl))
            {
                return new NotFoundResponse();
            }

            var responseFunction = this.routes[requestMethod][requestUrl];

            return responseFunction(request);
        }
    }
}
