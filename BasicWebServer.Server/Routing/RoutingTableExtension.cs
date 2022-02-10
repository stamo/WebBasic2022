using BasicWebServer.Server.Attributes;
using BasicWebServer.Server.Controllers;
using BasicWebServer.Server.HTTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BasicWebServer.Server.Routing
{
    public static class RoutingTableExtension
    {
        public static IRoutingTable MapGet<TController>(
            this IRoutingTable routingTable,
            string path,
            Func<TController, Response> controllerFunction) where TController : Controller
        => routingTable.Map(
            Method.Get, 
            path, 
            request => controllerFunction(CreateController<TController>(request)));

        public static IRoutingTable MapPost<TController>(
            this IRoutingTable routingTable,
            string path,
            Func<TController, Response> controllerFunction) where TController : Controller
        => routingTable.Map(
            Method.Post,
            path,
            request => controllerFunction(CreateController<TController>(request)));

        public static IRoutingTable MapControllers(this IRoutingTable routingTable)
        {
            IEnumerable<MethodInfo> controllerActions = GetControllerActions();

            foreach (var controllerAction in controllerActions)
            {
                string controllerName = controllerAction
                    .DeclaringType
                    .Name
                    .Replace(nameof(Controller), string.Empty);

                string actionName = controllerAction.Name;
                string path = $"/{controllerName}/{actionName}";

                var responseFunction = GetResponseFunction(controllerAction);

                Method httpMethod = Method.Get;
                var actionMethodAttribute = controllerAction
                    .GetCustomAttribute<HttpMethodAttribute>();

                if (actionMethodAttribute != null)
                {
                    httpMethod = actionMethodAttribute.HttpMethod;
                }

                routingTable.Map(httpMethod, path, responseFunction);

                MapDefaultRoutes(
                    routingTable,
                    httpMethod,
                    controllerName,
                    actionName,
                    responseFunction);
            }

            return routingTable;
        }

        public static IRoutingTable MapStaticFiles(this IRoutingTable routingTable, string folder = "wwwroot")
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var staticFilesFolder = Path.Combine(currentDirectory, folder);

            if (!Directory.Exists(staticFilesFolder))
            {
                return routingTable;
            }

            var staticFiles = Directory.GetFiles(
                staticFilesFolder,
                "*.*",
                SearchOption.AllDirectories);

            foreach (var file in staticFiles)
            {
                var relativePath = Path.GetRelativePath(staticFilesFolder, file);

                var urlPath = "/" + relativePath.Replace("\\", "/");

                routingTable.Map(Method.Get, urlPath, request =>
                {
                    var content = File.ReadAllBytes(file);
                    var fileExtension = Path.GetExtension(file).Trim('.');
                    var fileName = Path.GetFileName(file);
                    var contentType = ContentType.GetByFileExtension(fileExtension);

                    return new Response(StatusCode.OK)
                    {
                        FileContent = content
                    };
                });
            }

            return routingTable;
        }

        private static Func<Request, Response> GetResponseFunction(MethodInfo controllerAction)
        {
            return request =>
            {
                if (!UserIsAuthorized(controllerAction, request.Session))
                {
                    return new Response(StatusCode.Unauthorized);
                }

                var controllerInstance = CreateController(controllerAction.DeclaringType, request);
                var parameterValues = GetParameterValues(controllerAction, request);

                return (Response)controllerAction.Invoke(controllerInstance, parameterValues);
            };
        }

        private static object[] GetParameterValues(MethodInfo controllerAction, Request request)
        {
            var actionParameters = controllerAction
                .GetParameters()
                .Select(p => new
                {
                    p.Name,
                    p.ParameterType
                })
                .ToArray();

            var parameterValues = new object[actionParameters.Length];

            for (int i = 0; i < actionParameters.Length; i++)
            {
                var parameter = actionParameters[i];

                if (parameter.ParameterType.IsPrimitive || 
                    parameter.ParameterType == typeof(string))
                {
                    try
                    {
                        string parameterValue = request.GetValue(parameter.Name);
                        parameterValues[i] = Convert.ChangeType(parameterValue, parameter.ParameterType);
                    }
                    catch (Exception)
                    {}
                }
                else
                {
                    var parameterValue = Activator.CreateInstance(parameter.ParameterType);
                    var parameterProperties = parameter.ParameterType.GetProperties();

                    foreach (var property in parameterProperties)
                    {
                        try
                        {
                            var propertyValue = request.GetValue(property.Name);
                            property.SetValue(
                                parameterValue,
                                Convert.ChangeType(propertyValue, property.PropertyType));
                        }
                        catch (Exception)
                        {}
                    }

                    parameterValues[i] = parameterValue;
                }
            }

            return parameterValues;
        }

        private static IEnumerable<MethodInfo> GetControllerActions()
            => Assembly
            .GetEntryAssembly()
            .GetExportedTypes()
            .Where(t => t.IsAbstract == false)
            .Where(t => t.IsAssignableTo(typeof(Controller)))
            .Where(t => t.Name.EndsWith(nameof(Controller)))
            .SelectMany(t => t
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.ReturnType.IsAssignableTo(typeof(Response)))
            ).ToList();

        private static TController CreateController<TController>(Request request)
            => (TController)Activator.CreateInstance(typeof(TController), new[] { request });

        private static Controller CreateController(Type controllerType, Request request)
        {
            var controller = (Controller)Request.ServiceCollection.CreateInstance(controllerType);

            controllerType
                .GetProperty("Request", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, request);

            return controller;
        }

        private static string GetValue(this Request request, string name)
            => request.Query.GetValueOrDefault(name) ??
            request.Form.GetValueOrDefault(name);

        private static void MapDefaultRoutes(
            IRoutingTable routingTable,
            Method httpMethod,
            string controllerName,
            string actionName,
            Func<Request, Response> responseFunction)
        {
            const string defaultActionName = "Index";
            const string defaultControllerName = "Home";

            if (actionName == defaultActionName)
            {
                routingTable.Map(httpMethod, $"/{controllerName}", responseFunction);

                if (controllerName == defaultControllerName)
                {
                    routingTable.Map(httpMethod, "/", responseFunction);
                }
            }
        }

        private static bool UserIsAuthorized(
            MethodInfo controllerAction,
            Session session)
        {
            var authorizationRequired = controllerAction
                .DeclaringType
                .GetCustomAttribute<AuthorizeAttribute>()
                ?? controllerAction
                .GetCustomAttribute<AuthorizeAttribute>();

            if (authorizationRequired != null)
            {
                var userIsAuthorized = session.ContainsKey(Session.SessionUserKey)
                    && session[Session.SessionUserKey] != null;

                if (!userIsAuthorized)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
