using BasicWebServer.Server.HTTP;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;

namespace BasicWebServer.Server.Responses
{
    public  class ViewResponse : ContentResponse
    {
        private const char PathSeparator = '/';

        public ViewResponse(string viewName, string controllerName, object model = null)
            : base(string.Empty, ContentType.Html)
        {
            if (!viewName.Contains(PathSeparator))
            {
                viewName = controllerName + PathSeparator + viewName;
            }

            var viewPath = Path
                .GetFullPath($"./Views/{viewName.TrimStart(PathSeparator)}.cshtml");
            var viewContent = File.ReadAllText(viewPath);

            var (layoutPath, layoutExists) = FindLayout();

            if (layoutExists)
            {
                var layoutContent = File.ReadAllText(layoutPath);

                viewContent = layoutContent.Replace("{{RenderBody}}", viewContent);
            }

            if (model != null)
            {
                if (model is IEnumerable)
                {
                    viewContent = PopulateEnumerableModel(viewContent, model);
                }
                else 
                {
                    viewContent = PopulateModel(viewContent, model);
                }
            }

            Body = viewContent;
        }

        private string PopulateEnumerableModel(string viewContent, object model)
        {
            var result = new StringBuilder();

            var lines = viewContent
                .Split(Environment.NewLine)
                .Select(line => line.Trim());

            var inLoop = false;
            StringBuilder loopContent = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("{{foreach}}"))
                {
                    inLoop = true;

                    continue;
                }

                if (inLoop)
                {
                    if (line.StartsWith("{"))
                    {
                        loopContent = new StringBuilder();
                    }
                    else if (line.StartsWith("}"))
                    {
                        var loopTemplate = loopContent.ToString();

                        foreach (var item in (IEnumerable)model)
                        {
                            var loopResult = PopulateModel(loopTemplate, item);

                            result.AppendLine(loopResult);
                        }

                        inLoop = false;
                    }
                    else
                    {
                        loopContent.AppendLine(line);
                    }

                    continue;
                }

                result.AppendLine(line);
            }

            return result.ToString();
        }

        private string PopulateModel(string viewContent, object model)
        {
            var data = model
                .GetType()
                .GetProperties()
                .Select(p => new
                {
                    p.Name,
                    Value = p.GetValue(model)
                });

            foreach (var item in data)
            {
                const string openingBrackets = "{{";
                const string closingBrackets = "}}";

                viewContent = viewContent.Replace($"{openingBrackets}{item.Name}{closingBrackets}", item.Value.ToString());
            }

            return viewContent;
        }

        private (string, bool) FindLayout()
        {
            string layoutPath = null;
            bool exists = false;

            layoutPath = Path.GetFullPath("./Views/Layout.cshtml");

            if (File.Exists(layoutPath))
            {
                exists = true;
            }
            else
            {
                layoutPath = Path.GetFullPath("./Views/Shared/_Layout.cshtml");

                if (File.Exists(layoutPath))
                {
                    exists = true;
                }
            }

            return (layoutPath, exists);
        }
    }
}
