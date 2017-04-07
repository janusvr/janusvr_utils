using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JanusVR
{
    public static class ReflectionUtil
    {
        public static Type GetType(string className, Assembly assembly = null)
        {
            Type t = null;
            if (assembly == null)
            {
                // search all loaded assemblies
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Assembly a = assemblies[i];
                    Type k = a.GetType(className);
                    if (k != null)
                    {
                        t = k;
                        break;
                    }
                }
            }
            else
            {
                t = assembly.GetType(className);
            }

            return t;
        }

        public static object GetStaticField(string className, string fieldName, Assembly assembly = null)
        {
            Type t = GetType(className, assembly);
            FieldInfo field = t.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return (object)field.GetValue(null);
        }

        public static void InvokeStaticMethod(string methodName, params object[] parameters)
        {
            
        }
    }
}
