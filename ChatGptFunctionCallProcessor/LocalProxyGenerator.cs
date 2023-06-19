using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChatGptFunctionCallProcessor
{
    internal class LocalProxyGenerator
    {
        private static readonly ConcurrentDictionary<string, ILocalMethodDelegate> InstanceDictionary = new ConcurrentDictionary<string, ILocalMethodDelegate>();
        /// <summary>
        /// 执行本地方法
        /// </summary>
        /// <param name="pathname"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static async Task<object> Excute(string methodName, Dictionary<string, object> dictionary)
        {
            if (InstanceDictionary.TryGetValue(methodName.ToLower(), out ILocalMethodDelegate methodDelegate))
            {
                return await methodDelegate.Excute(JsonSerializer.Deserialize(JsonSerializer.Serialize(dictionary), methodDelegate.ParmterType));
            }
            return null;
        }
        /// <summary>
        /// 缓存本地方法类型信息
        /// </summary>
        /// <param name="pathname"></param>
        /// <returns></returns>
        public static void LoadMethodDelegate<T>(T instance)
        {
            foreach (var method in typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var delegateObj = CreateMethodDelegate(instance, method);
                InstanceDictionary.TryAdd(method.Name.ToLower(), delegateObj);
            }
        }
        /// <summary>
        /// 创建本地方法委托
        /// </summary>
        /// <returns></returns>
        static ILocalMethodDelegate CreateMethodDelegate<T>(T instance, MethodInfo method)
        {
            return (ILocalMethodDelegate)Activator.CreateInstance(typeof(LocalMethodDelegate<,>).MakeGenericType(method.GetParameters()[0].ParameterType, method.ReturnType.GetGenericArguments().FirstOrDefault()), instance, method);
        }
    }
}
