using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Dto
{
    public class CmParam
    {
        public string paramname { get; set; }
        public string paramvalue { get; set; }
        public string paramvaluedef { get; set; }
        public string paramdescription { get; set; }
    }

    public class DefaultParameter : CmParam
    {
        public string system { get; set; }
        public string tool { get; set; }
        //public string key { get; set; }
        //public bool autocreate { get; set; }
        //public string defaults { get; set; }
        //public string note { get; set; }
        //用户参数


    }

    public class CMUserInfo
    {
        public string createtime { get; set; }
        public bool disabled { get; set; }
        public string email { get; set; }
        public string id { get; set; }
        public string loginname { get; set; }
        public string nickname { get; set; }
    }

    public class ExtParam
    {
        public string accessPWD { get; set; }
        public string accessUser { get; set; }
        public string path { get; set; }
        public string pathType { get; set; }
#pragma warning disable IDE1006 // 命名样式
#pragma warning disable CA2227 // 集合属性应为只读
        public List<string> storageMarks { get; set; }
#pragma warning restore CA2227 // 集合属性应为只读
#pragma warning restore IDE1006 // 命名样式


        public long? storageSize { get; set; }
        public string storageType { get; set; }
        public long? usedSize { get; set; }
    }
}
