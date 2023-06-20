using OpenAI.ObjectModels.RequestModels;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatGptFunctionCallProcessor
{
    public static class FunctionDefinitionBuilderExtension
    {
        /// <summary>
        /// 通过类及方法名获取函数定义
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="functionname"></param>
        /// <returns></returns>
        public static IEnumerable<FunctionDefinition> GetDefinition<T>(this T instance)
        {
            if (instance != null)
            {
                var methods = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if(methods.Any())
                {
                    foreach (var method in methods)
                    {
                        var defined = new FunctionDefinition();
                        defined.Name = method.Name;
                        defined.Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
                        if(method.GetParameters().Any())
                        {
                            defined.Parameters = new FunctionParameters();
                            defined.Parameters.Properties = new Dictionary<string, FunctionParameterPropertyValue>();
                            var methodparams = method.GetParameters()[0];
                            foreach (var prop in methodparams.ParameterType.GetProperties())
                            {
                                var propval = new FunctionParameterPropertyValue()
                                {
                                    Description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description,
                                    Type = GetJsonType(prop.PropertyType),
                                    Enum = new List<string>()
                                };
                                if (prop.PropertyType.IsEnum)
                                {
                                    propval.Enum = Enum.GetNames(prop.PropertyType);
                                }
                                defined.Parameters.Properties.Add(prop.Name, propval);
                            }
                        }
                        yield return defined;
                    }
                    LocalProxyGenerator.LoadMethodDelegate(instance);
                }
            }
        }
        public static async Task<ChatMessage> CallFunction<T>(this T instance, string functionName, Dictionary<string, object> pairs)
        {
            var result = await LocalProxyGenerator.Excute(functionName, pairs);
            return new ChatMessage("function", JsonSerializer.Serialize(result), functionName);
        }
        static string GetJsonType(Type type)
        {
            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(int) || type == typeof(float) || type == typeof(decimal) || type == typeof(double) ||
                type == typeof(short) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong) ||
                type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte))
            {
                return "number";
            }

            if (type == typeof(bool))
            {
                return "boolean";
            }

            if (type == typeof(DateTime) || type == typeof(Guid))
            {
                return "string";
            }

            if (type == typeof(object))
            {
                return "object";
            }

            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();

                if (genericType == typeof(Nullable<>))
                {
                    return GetJsonType(type.GetGenericArguments()[0]);
                }

                if (genericType == typeof(List<>) || genericType == typeof(IEnumerable<>))
                {
                    return "array";
                }

                if (genericType == typeof(Dictionary<,>) && type.GetGenericArguments()[0] == typeof(string))
                {
                    return "object";
                }
            }
            if (typeof(IEnumerable<>).IsAssignableFrom(type) || type.IsArray)
            {
                return "array";
            }
            return "object";
        }
    }
}