using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test._ScriptExtensions
{
    public static class TypeExtension
    { 
        public static bool IsNullable(this Type type)
        {
            Type underlyingType = Nullable.GetUnderlyingType(type); 
            return underlyingType == null;
        } 
    }
}
