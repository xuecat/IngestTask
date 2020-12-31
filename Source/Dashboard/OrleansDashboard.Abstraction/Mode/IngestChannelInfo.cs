using System;
using System.Collections.Generic;
using System.Text;

namespace OrleansDashboard.Abstraction.Model
{
    public class IngestChannelInfo
    {
        public int Id { get; set; }
        /// <summary>通道id</summary>
        public int ChannelId { get; set; }
        /// <summary>通道名</summary>
        public string ChannelName { get; set; }
        /// <summary>设备类型 当前为0</summary>
        public int DeviceTypeId { get; set; }

        /// <summary>设备名称</summary>
        public string DeviceName { get; set; }

        /// <summary>IP</summary>
        public string Ip { get; set; }

        /// <summary>
        /// 通道序列号
        /// </summary>
        public int ChannelIndex { get; set; }

        /// <summary>序号</summary>
        public int OrderCode { get; set; }
    }
}
