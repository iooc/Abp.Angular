using Abp;
using Abp.Collections.Extensions;
using Abp.Extensions;
using Abp.Web.Api.Modeling;
using Abp.Web.Api.ProxyScripting.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Future.Angular.Proxying
{
    /// <summary>
    /// 脚本生成代理帮助类
    /// </summary>
    internal static class ProxyScriptingHelper
    {
        public const string DefaultHttpVerb = "POST";
        /// <summary>
        /// 生成带参数的 url
        /// </summary>
        /// <param name="action">API 描述模型</param>
        /// <returns></returns>
        public static string GenerateUrlWithParameters(ActionApiDescriptionModel action)
        {
            //TODO: Can be optimized using StringBuilder?
            var url = ReplacePathVariables(action.Url, action.Parameters);
            url = AddQueryStringParameters(url, action.Parameters);
            return url;
        }
        /// <summary>
        /// 生成消息头
        /// </summary>
        /// <param name="action">API 描述模型</param>
        /// <param name="indent"></param>
        /// <returns></returns>
        public static string GenerateHeaders(ActionApiDescriptionModel action, int indent = 0)
        {
            var parameters = action
                .Parameters
                .Where(p => p.BindingSourceId == ParameterBindingSources.Header)
                .ToArray();

            if (!parameters.Any())
            {
                return null;
            }

            return ProxyScriptingTsFuncHelper.CreateTsObjectLiteral(parameters, indent);
        }
        /// <summary>
        /// 生成传输主体
        /// </summary>
        /// <param name="action">API 描述模型</param>
        /// <returns></returns>
        public static string GenerateBody(ActionApiDescriptionModel action)
        {
            var parameters = action
                .Parameters
                .Where(p => p.BindingSourceId == ParameterBindingSources.Body)
                .ToArray();

            if (parameters.Length <= 0)
            {
                return null;
            }

            if (parameters.Length > 1)
            {
                throw new AbpException(
                    $"Only one complex type allowed as argument to a controller action that's binding source is 'Body'. But {action.Name} ({action.Url}) contains more than one!"
                    );
            }

            return ProxyScriptingTsFuncHelper.GetParamNameInTsFunc(parameters[0]);
        }
        /// <summary>
        /// 生成表单POST数据
        /// </summary>
        /// <param name="action">API 描述模型</param>
        /// <param name="indent"></param>
        /// <returns></returns>
        public static string GenerateFormPostData(ActionApiDescriptionModel action, int indent = 0)
        {
            var parameters = action
                .Parameters
                .Where(p => p.BindingSourceId == ParameterBindingSources.Form)
                .ToArray();

            if (!parameters.Any())
            {
                return null;
            }

            return ProxyScriptingTsFuncHelper.CreateTsObjectLiteral(parameters, indent);
        }
        /// <summary>
        /// 替换地址变量
        /// </summary>
        /// <param name="url">url 地址</param>
        /// <param name="actionParameters">API 参数模型集合</param>
        /// <returns></returns>
        private static string ReplacePathVariables(string url, IList<ParameterApiDescriptionModel> actionParameters)
        {
            var pathParameters = actionParameters
                .Where(p => p.BindingSourceId == ParameterBindingSources.Path)
                .ToArray();

            if (!pathParameters.Any())
            {
                return url;
            }

            foreach (var pathParameter in pathParameters)
            {
                url = url.Replace($"{{{pathParameter.Name}}}", $"' + {ProxyScriptingTsFuncHelper.GetParamNameInTsFunc(pathParameter)} + '");
            }

            return url;
        }
        /// <summary>
        /// 添加地址栏查询参数
        /// </summary>
        /// <param name="url">url 地址</param>
        /// <param name="actionParameters">API 参数模型集合</param>
        /// <returns></returns>
        private static string AddQueryStringParameters(string url, IList<ParameterApiDescriptionModel> actionParameters)
        {
            var queryStringParameters = actionParameters
                .Where(p => p.BindingSourceId.IsIn(ParameterBindingSources.ModelBinding, ParameterBindingSources.Query))
                .ToArray();

            if (!queryStringParameters.Any())
            {
                return url;
            }

            //var qsBuilderParams = queryStringParameters
            //    .Select(p => $"{{ name: '{p.Name.ToCamelCase()}', value: {ProxyScriptingTsFuncHelper.GetParamNameInTsFunc(p)} }}")
            //    .JoinAsString(", ");
            var qsBuilderParams = queryStringParameters
                .Select(p => $"{p.Name.ToCamelCase()}='+{p.Name.ToCamelCase()}")
                .JoinAsString("+'&");

            return url + "?" + qsBuilderParams+"+'";
        }
        /// <summary>
        /// 从函数名中获取常规HTTP谓词
        /// </summary>
        /// <param name="methodName">函数名</param>
        /// <returns></returns>
        public static string GetConventionalVerbForMethodName(string methodName)
        {
            if (methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
            {
                return "GET";
            }

            if (methodName.StartsWith("Put", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase))
            {
                return "PUT";
            }

            if (methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Remove", StringComparison.OrdinalIgnoreCase))
            {
                return "DELETE";
            }

            if (methodName.StartsWith("Patch", StringComparison.OrdinalIgnoreCase))
            {
                return "PATCH";
            }

            if (methodName.StartsWith("Post", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Insert", StringComparison.OrdinalIgnoreCase))
            {
                return "POST";
            }

            //Default
            return DefaultHttpVerb;
        }
    }
}
