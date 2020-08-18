using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Tools.Dto
{
    /// <summary>采集设备详情</summary>
    public class CaptureDeviceInfo
    {
        /// <summary>Id</summary>
        public int Id { get; set; }

        /// <summary>设备类型 当前为0</summary>
        public int DeviceTypeId { get; set; }

        /// <summary>设备名称</summary>
        public string DeviceName { get; set; }

        /// <summary>IP</summary>
        public string Ip { get; set; }

        /// <summary>序号</summary>
        public int OrderCode { get; set; }
    }
}

