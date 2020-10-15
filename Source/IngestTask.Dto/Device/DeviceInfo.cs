using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Dto
{
    /// <summary>采集设备详情</summary>
    public class DeviceInfo
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

    public class ChannelInfo : DeviceInfo
    {
        public bool NeedStopFlag { get; set; }
        public Device_State CurrentDevState { get; set; } = Device_State.DISCONNECTTED;
        public Device_State LastDevState { get; set; } = Device_State.DISCONNECTTED;

       
        /// <summary>MSV模式</summary>
        public MSV_Mode LastMsvMode { get; set; } = MSV_Mode.LOCAL;
        /// <summary>vtrId</summary>
        public int VtrId { get; set; } = -1;

        /// <summary>当前用户Code</summary>
        public string UserCode { get; set; } = string.Empty;

        /// <summary>kamataki信息</summary>
        public string KamatakiInfo { get; set; } = string.Empty;

        /// <summary>上载模式</summary>
        public Upload_Mode UploadMode { get; set; } = Upload_Mode.NOUPLOAD;

    }
}

