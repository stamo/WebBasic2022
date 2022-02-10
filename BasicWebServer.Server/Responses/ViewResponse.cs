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
                viewContent = EvaluateConditions(viewContent, model);

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

        private string EvaluateConditions(string viewContent, object model)
        {
            var result = new StringBuilder();

            var lines = viewContent
                .Split(Environment.NewLine)
                .Select(line => line.Trim());

            var inCondition = false;
            var waitingForElse = false;
            var inElse = false;
            string conditionPropertyName = string.Empty;

            StringBuilder ifContent = null;
            StringBuilder elseContent = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("{{if("))
                {
                    int start = line.IndexOf('(') + 1;
                    int end = line.IndexOf(')');
                    conditionPropertyName = line.Substring(start, end - start)?.Trim();
                    inCondition = true;
                    inElse = false;
                    waitingForElse = false;

                    continue;
                }

                if (inCondition)
                {
                    if (waitingForElse && line.StartsWith("{{else}}"))
                    {
                        inElse = true;
                        inCondition = false;
                        waitingForElse = false;

                        continue;
                    }
                    else if (waitingForElse)
                    {
                        inElse = false;
                        inCondition = false;
                        waitingForElse = false;

                        string conditionResult = GetConditionContent(ifContent, elseContent, model, conditionPropertyName);
                        
                        if (!string.IsNullOrWhiteSpace(conditionResult))
                        {
                            result.AppendLine(conditionResult);
                        }

                        result.AppendLine(line);

                        continue;
                    }

                    if (line.StartsWith("{"))
                    {
                        ifContent = new StringBuilder();
                    }
                    else if (line.StartsWith("}"))
                    {
                        waitingForElse = true;
                    }
                    else 
                    {
                        ifContent.AppendLine(line);
                    }

                    continue;
                }

                if (inElse)
                {
                    if (line.StartsWith("{"))
                    {
                        elseContent = new StringBuilder();
                    }
                    else if (line.StartsWith("}"))
                    {
                        inElse = false;
                        string conditionResult = GetConditionContent(ifContent, elseContent, model, conditionPropertyName);

                        if (!string.IsNullOrWhiteSpace(conditionResult))
                        {
                            result.AppendLine(conditionResult);
                        }
                    }
                    else
                    {
                        elseContent.AppendLine(line);
                    }

                    continue;
                }

                result.AppendLine(line);
            }

            return result.ToString();
        }

        private string GetConditionContent(StringBuilder ifContent, StringBuilder elseContent, object model, string conditionPropertyName)
        {
            var prop = model
                .GetType()
                .GetProperty(conditionPropertyName);

            if (prop != null)
            {
                bool? conditionResult = prop.GetValue(model) as bool?;

                if (conditionResult == true && ifContent != null)
                {
                    return ifContent.ToString();
                }
                else if (conditionResult == false && elseContent != null)
                {
                    return elseContent.ToString();
                }
            }

            return null;
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
                if (item.Value is IEnumerable && item.Value is not string)
                {
                    viewContent = PopulateEnumerableModel(viewContent, item.Value);

                    continue;
                }

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
