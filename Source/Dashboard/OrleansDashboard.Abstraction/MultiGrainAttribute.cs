using System;
using System.Collections.Generic;
using System.Text;

namespace OrleansDashboard.Abstraction
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public sealed class NoProfilingAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public sealed class MultiGrainAttribute : Attribute
    {
        private static List<string> _RecordClass = new List<string>();
        public MultiGrainAttribute(string name)
        {
            if (_RecordClass.FindIndex(x => x == name) < 0)
            {
                _RecordClass.Add(name);
            }

        }

        public static bool IsRecordClass(string name)
        {
            if (_RecordClass.FindIndex(x => x == name) >= 0)
            {
                return true;
            }
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public sealed class TraceGrainAttribute : Attribute
    {
        private static Dictionary<string, TaskTraceEnum> _RecordClass = new Dictionary<string, TaskTraceEnum>();
        public TraceGrainAttribute(string name, TaskTraceEnum type)
        {
            if (!_RecordClass.ContainsKey(name))
            {
                _RecordClass.Add(name, type);
            }

        }

        public static bool IsRecordClass(string key)
        {
            if (_RecordClass.ContainsKey(key))
            {
                return true;
            }
            return false;
        }

        public static TaskTraceEnum GetRecordTrace(string key)
        {
            TaskTraceEnum value = TaskTraceEnum.No;
            if (_RecordClass.TryGetValue(key, out value))
            {
                return value;
            }
            return value;
        }
    }
}
