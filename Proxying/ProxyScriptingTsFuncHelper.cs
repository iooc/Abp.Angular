using Abp.Collections.Extensions;
using Abp.Extensions;
using Abp.Web.Api.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Future.Angular.Proxying
{
    /// <summary>
    /// 
    /// </summary>
    internal static class ProxyScriptingTsFuncHelper
    {
        /// <summary>
        /// 验证变量名字符
        /// </summary>
        private const string ValidTsVariableNameChars = "abcdefghijklmnopqrstuxwvyzABCDEFGHIJKLMNOPQRSTUXWVYZ0123456789_";
        /// <summary>
        /// 预留关键字
        /// </summary>
        private static readonly HashSet<string> ReservedWords = new HashSet<string> {
            "abstract",
            "else",
            "instanceof",
            "super",
            "boolean",
            "enum",
            "int",
            "switch",
            "break",
            "export",
            "interface",
            "synchronized",
            "byte",
            "extends",
            "let",
            "this",
            "case",
            "false",
            "long",
            "throw",
            "catch",
            "final",
            "native",
            "throws",
            "char",
            "finally",
            "new",
            "transient",
            "class",
            "float",
            "null",
            "true",
            "const",
            "for",
            "package",
            "try",
            "continue",
            "function",
            "private",
            "typeof",
            "debugger",
            "goto",
            "protected",
            "var",
            "default",
            "if",
            "public",
            "void",
            "delete",
            "implements",
            "return",
            "volatile",
            "do",
            "import",
            "short",
            "while",
            "double",
            "in",
            "static",
            "with"
        };
        /// <summary>
        /// 后端基本类型
        /// </summary>
        private static readonly string[] _basicTypes =
        {
            "guid", "string", "bool",
            "datetime", "int16", "int32", "int64", "single", "double", "decimal", "boolean", "void","byte"
        };
        /// <summary>
        /// 应忽略的类型
        /// </summary>
        private static readonly string[] _typesToIgnore =
        {
            "exception", "aggregateexception","module","object"
        };
        /// <summary>
        /// 是否基本类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsBasicType(Type type)
        {
            if (_basicTypes.Contains(NormalizeTsVariableName(type.Name).ToLowerInvariant()))
                return true;
            else
                return false;
        }
        /// <summary>
        /// 是否应忽略类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsIgnorantType(Type type)
        {
            if (_typesToIgnore.Contains(NormalizeTsVariableName(type.Name).ToLowerInvariant()))
                return true;
            else
                return false;
        }
        /// <summary>
        /// 获取类型的约定名称
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="newTypesToAdd">新类型的缓存列表</param>
        /// <returns></returns>
        public static string GetTypeContractName(Type type, List<Type> newTypesToAdd)
        {
            if (IsIgnorantType(type))
                return "any";

            if (type == typeof(Task))
            {
                return "void";
            }

            if (type.IsArray)
            {

                return GetTypeContractName(type.GetElementType(), newTypesToAdd) + "[]";
            }

            if (type.IsGenericType && (typeof(Task<>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
                                       typeof(TaskFactory<>).IsAssignableFrom(type.GetGenericTypeDefinition())))
            {
                return GetTypeContractName(type.GetGenericArguments()[0], newTypesToAdd);
            }

            if (type.IsGenericType && typeof(Nullable<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                return GetTypeContractName(type.GetGenericArguments()[0], newTypesToAdd);
            }

            if (type.IsGenericType && (
                typeof(List<>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
                typeof(ICollection<>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
                typeof(IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
                typeof(IReadOnlyList<>).IsAssignableFrom(type.GetGenericTypeDefinition())
                ))
            {
                return GetTypeContractName(type.GetGenericArguments()[0], newTypesToAdd) + "[]";
            }
            if (type.IsEnum)
            {
                return "number";
            }
            switch (type.Name.ToLowerInvariant())
            {
                case "guid":
                    return "string";
                case "datetime":
                    return "Date";
                case "int16":
                case "int32":
                case "int64":
                case "single":
                case "double":
                case "decimal":
                case "byte":
                    return "number";
                case "boolean":
                case "bool":
                    return "boolean";
                case "void":
                case "string":
                    return type.Name.ToLowerInvariant();
            }

            newTypesToAdd.Add(type);

            //return GenericSpecificName(type).ToCamelCase();
            // 俺这里类型不打算驼峰命名
            return GenericSpecificName(type);
        }
        /// <summary>
        /// 生成类型的指定名称
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static string GenericSpecificName(Type type)
        {
            //todo: update for Typescript's generic syntax once invented
            string name = type.Name;
            int index = name.IndexOf('`');
            name = index == -1 ? name : name.Substring(0, index);
            // 泛型类型
            if (type.IsGenericType &&
                type.GenericTypeArguments.Where(a => !IsBasicType(a) && !IsIgnorantType(a)).Count() != 0)
            {
                // 串联泛型参数的集合
                name += "Of" + string.Join("And", type.GenericTypeArguments.Where(a => 
                !IsBasicType(a) && !IsIgnorantType(a)).Select(GenericSpecificName));
            }
            return name;
        }


        /// <summary>
        /// 规范化TS变量名
        /// </summary>
        /// <param name="name">变量名</param>
        /// <param name="additionalChars">附加符号</param>
        /// <returns></returns>
        public static string NormalizeTsVariableName(string name, string additionalChars = "")
        {
            var validChars = ValidTsVariableNameChars + additionalChars;

            var sb = new StringBuilder(name);

            sb.Replace('-', '_');

            //删除变量名中的奇怪字符
            foreach (var c in name)
            {
                if (!validChars.Contains(c))
                {
                    sb.Replace(c.ToString(), "");
                }
            }

            if (sb.Length == 0)
            {
                return "_" + Guid.NewGuid().ToString("N").Left(8);
            }

            return sb.ToString();
        }
        /// <summary>
        /// 包装中括号或者点前缀
        /// </summary>
        /// <param name="name">变量名</param>
        /// <returns></returns>
        public static string WrapWithBracketsOrWithDotPrefix(string name)
        {
            if (!ReservedWords.Contains(name))
            {
                return "." + name;
            }

            return "['" + name + "']";
        }
        /// <summary>
        /// 获取Ts函数中的参数名
        /// </summary>
        /// <param name="parameterInfo">参数信息</param>
        /// <returns></returns>
        public static string GetParamNameInTsFunc(ParameterApiDescriptionModel parameterInfo)
        {
            return parameterInfo.Name == parameterInfo.NameOnMethod
                       ? NormalizeTsVariableName(parameterInfo.Name.ToCamelCase(), ".")
                       : NormalizeTsVariableName(parameterInfo.NameOnMethod.ToCamelCase()) + "." + NormalizeTsVariableName(parameterInfo.Name.ToCamelCase(), ".");
        }
        /// <summary>
        /// 创建 TS 对象字面
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        public static string CreateTsObjectLiteral(ParameterApiDescriptionModel[] parameters, int indent = 0)
        {
            var sb = new StringBuilder();

            sb.AppendLine("{");

            foreach (var prm in parameters)
            {
                sb.AppendLine($"{new string(' ', indent)}  '{prm.Name}': {GetParamNameInTsFunc(prm)}");
            }

            sb.Append(new string(' ', indent) + "}");

            return sb.ToString();
        }
        /// <summary>
        /// 生成TS 函数参数列表
        /// </summary>
        /// <param name="action">API函数描述模型</param>
        /// <param name="ajaxParametersName">附加参数名称</param>
        /// <returns></returns>
        public static string GenerateTsFuncParameterList(ActionApiDescriptionModel action, string ajaxParametersName = null)
        {
            List<Type> types = new List<Type>();
            // 参数名的集合
            var methodParamNames = action.Parameters.Select(p => NormalizeTsVariableName(p.Name) + ": " + GetTypeContractName(p.Type, types)).Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(ajaxParametersName))
                methodParamNames.Add(ajaxParametersName);
            // 与 JS 不同之处,保留冒号形成类型定义
            return methodParamNames.Select(prmName => NormalizeTsVariableName(prmName.ToCamelCase(),":")).JoinAsString(", ");
        }

        //public static string GenerateImportScript(string @class, string path)
        //{
        //    return "";
        //}
    }
}
