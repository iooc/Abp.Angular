using Abp.Dependency;
using Abp.Web.Api.ProxyScripting.Generators;
using System;
using System.Collections.Generic;
using System.Text;
using Abp.Web.Api.Modeling;
using Abp.Extensions;
using System.Threading.Tasks;

namespace Future.Angular.Proxying
{
    /// <summary>
    /// Angular TS 脚本代理生成器
    /// </summary>
    public class AngularProxyScriptGenerator : IProxyScriptGenerator, ITransientDependency
    {
        /// <summary>
        /// "angular".
        /// </summary>
        public const string Name = "angular";
        /// <summary>
        /// 存储已生成的类型备查，避免重复
        /// </summary>
        private static List<Type> localCache = new List<Type>();

        private static List<string> localClass = new List<string>();
        /// <summary>
        /// 从此函数开始创建脚本
        /// </summary>
        /// <param name="model">应用层API描述模型</param>
        /// <returns>已生成的脚本字符串</returns>
        public string CreateScript(ApplicationApiDescriptionModel model)
        {
            var script = new StringBuilder();

            script.AppendLine("/* 此 typescript 文件是用继承自 ABP 框架的 MVC 控制器自动生成的. */");
            script.AppendLine();
            script.AppendLine("import { Injectable } from \"@angular/core\";");
            script.AppendLine("import { HttpClient } from \"@angular/common/http\";");

            foreach (var module in model.Modules.Values)
            {
                script.AppendLine();
                AddModuleScript(script, module);
            }

            return script.ToString();
        }
        /// <summary>
        /// 为模块添加脚本
        /// </summary>
        /// <param name="script">脚本字符串构建器</param>
        /// <param name="module">模块API模型</param>
        private static void AddModuleScript(StringBuilder script, ModuleApiDescriptionModel module)
        {
            // 一个模块生成一个类
            script.AppendLine($"/* 模块 '{module.Name}' */");
            script.AppendLine($"export module {module.Name} {{");
            
            foreach (var controller in module.Controllers.Values)
            {
                // 服务中包含的所有模型
                script.AppendLine();
                AddModelScript(script, controller);
                // 服务中包含的所有服务
                AddControllerScript(script, module, controller);
            }

            script.AppendLine();
            script.AppendLine("}");
        }
        /// <summary>
        /// 为控制器添加脚本
        /// </summary>
        /// <param name="script">脚本字符串构建器</param>
        /// <param name="module">模块API模型引用</param>
        /// <param name="controller">控制器API模型</param>
        private static void AddControllerScript(
            StringBuilder script, 
            ModuleApiDescriptionModel module, 
            ControllerApiDescriptionModel controller)
        {
            script.AppendLine($"  /* 控制器 '{controller.Name}' */");
            script.AppendLine("  @Injectable()");
            script.AppendLine($"  export class {controller.Name} {{");
            script.AppendLine();
            script.AppendLine("    constructor(private http: HttpClient) { }");
            
            foreach (var action in controller.Actions.Values)
            {
                script.AppendLine();
                AddActionScript(script, module, controller, action);
            }

            script.AppendLine();
            script.AppendLine("  }");
        }
        /// <summary>
        /// 为API接口函数添加脚本
        /// </summary>
        /// <param name="script">脚本字符串构建器</param>
        /// <param name="module">模块描述模型</param>
        /// <param name="controller">控制器描述模型</param>
        /// <param name="action">API接口函数描述</param>
        private static void AddActionScript(StringBuilder script, ModuleApiDescriptionModel module, ControllerApiDescriptionModel controller, ActionApiDescriptionModel action)
        {
            //var parameterList = ProxyScriptingTsFuncHelper.GenerateTsFuncParameterList(action, "ajaxParams");
            var parameterList = ProxyScriptingTsFuncHelper.GenerateTsFuncParameterList(action);
            var actionName = action.Name.ToCamelCase();

            script.AppendLine($"    /* API 方法 '{action.Name}' */");
            script.AppendLine($"    {actionName}({parameterList}) {{");
            script.Append($"        return this.http");

            AddAjaxCallParameters(script, controller, action);

            script.AppendLine("    };");
        }
        private static void AddAjaxCallParameters(StringBuilder script, ControllerApiDescriptionModel controller, ActionApiDescriptionModel action)
        {
            //var httpMethod = action.HttpMethod?.ToUpperInvariant() ?? "POST";
            var httpMethod = action.HttpMethod?.ToLowerInvariant() ?? "post";
            var retureType = ProxyScriptingTsFuncHelper.GetTypeContractName(action.ReturnValue.Type, localCache);
            var url = ProxyScriptingHelper.GenerateUrlWithParameters(action);

            script.Append($".{httpMethod}<{retureType}>('/{url}'");

            var body = ProxyScriptingHelper.GenerateBody(action);
            if (!body.IsNullOrEmpty())
            {
                script.Append(",");
                script.Append(body);
            }
            else
            {
                var formData = ProxyScriptingHelper.GenerateFormPostData(action, 8);
                if (!formData.IsNullOrEmpty())
                {
                    script.Append(",");
                    script.Append(formData);
                }
            }
            script.AppendLine(")");
            script.AppendLine(".toPromise().then(res => { return res })");
            var error = "";
            switch (httpMethod)
            {
                case "get":
                    error = "获取";
                    break;
                case "post":
                    error = "新增";
                    break;
                case "put":
                    error = "修改";
                    break;
                case "delete":
                    error = "删除";
                    break;
            }
            script.AppendLine("        .catch(err => {");
            script.AppendLine($"            console.error('{error} {controller.Name} 出现错误：', err);");
            script.AppendLine("            return Promise.reject(err.message || err);");
            script.AppendLine("        });");
        }
        /// <summary>
        /// 添加模型脚本
        /// </summary>
        /// <param name="script">脚本字符串构建器</param>
        /// <param name="controller">控制器API描述模型</param>
        private static void AddModelScript(StringBuilder script, ControllerApiDescriptionModel controller)
        {
            foreach (var action in controller.Actions.Values)
            {
                // 返回模型 必须是异步
                var type = action.ReturnValue.Type;
                if (type.IsGenericType && !localCache.Contains(type.GenericTypeArguments[0]))
                {
                    AddClassScript(script, type.GenericTypeArguments[0]);
                }
                // 参数模型
                foreach (var param in action.Parameters)
                {
                    type = param.Type;
                    if (!ProxyScriptingTsFuncHelper.IsBasicType(type) &&
                        !ProxyScriptingTsFuncHelper.IsIgnorantType(type)&&
                        !localCache.Contains(type))
                    {
                        AddClassScript(script, type);
                    }
                }
            }
        }
        /// <summary>
        /// 模型定义脚本的递归实现
        /// </summary>
        /// <param name="script">脚本字符串构建器</param>
        /// <param name="type">用于生成前端类型定义的直接类型</param>
        private static void AddClassScript(StringBuilder script,Type type)
        {
            // 作为类时可以是泛型
            //if (type.IsGenericType)
            //    type = type.GenericTypeArguments[0];
            if (!localCache.Contains(type)&& !type.IsEnum)
            {
                var typeName = ProxyScriptingTsFuncHelper.NormalizeTsVariableName(
                            ProxyScriptingTsFuncHelper.GetTypeContractName(type, localCache));
                if (!localClass.Contains(typeName))
                {
                    localClass.Add(typeName);

                    var props = new StringBuilder();
                    // 模型成员
                    foreach (var prop in type.GetProperties())
                    {
                        // 作为属性时必须去泛型
                        var PropType = prop.PropertyType;
                        if (PropType.IsGenericType)
                            PropType = prop.PropertyType.GenericTypeArguments[0];

                        if (!ProxyScriptingTsFuncHelper.IsBasicType(PropType) &&
                            !ProxyScriptingTsFuncHelper.IsIgnorantType(PropType))
                            AddClassScript(script, PropType);

                        var propType = ProxyScriptingTsFuncHelper
                            .GetTypeContractName(prop.PropertyType, localCache);
                        if (propType == "TreeNodeDto[]")
                            propType = typeName + "[]";
                        if (propType == "Dictionary")
                            propType = "Array<{key:string,value:boolean}>";
                        props.AppendLine($"     {prop.Name.ToCamelCase()}: {propType};");
                    }
                    script.AppendLine($"  /* 模型 '{typeName}' */");
                    script.AppendLine($"  export class {typeName} {{");

                    script.Append(props.ToString());

                    script.AppendLine("  }");
                    script.AppendLine();
                }
            }
        }
    }
}
