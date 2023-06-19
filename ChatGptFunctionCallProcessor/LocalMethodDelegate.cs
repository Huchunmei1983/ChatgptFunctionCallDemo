using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChatGptFunctionCallProcessor
{
    public interface ILocalMethodDelegate
    {
        public Type ParmterType { get; set; }
        Task<object> Excute(object val);
    }
    public class LocalMethodDelegate<Tin, Tout>: ILocalMethodDelegate
    {
        private Func<Tin, Task<Tout>> localfunc;
        public Type ParmterType { get; set; }
        public LocalMethodDelegate(object instance, MethodInfo method)
        {
            ParmterType = typeof(Tin);
            localfunc = (Func<Tin, Task<Tout>>)method.CreateDelegate(typeof(Func<Tin, Task<Tout>>), instance);
        }
        public async Task<object> Excute(object val)
        {
            return await localfunc((Tin)val);
        }
    }
}
