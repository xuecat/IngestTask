﻿using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Dto
{
    /// <summary>MSV设备状态信息</summary>
    public class MsvChannelState
    {
        /// <summary>通道Id</summary>
        public int ChannelId { get; set; }

        /// <summary>设备状态</summary>
        public Device_State DevState { get; set; } = Device_State.DISCONNECTTED;

        /// <summary>MSV模式</summary>
        public MSV_Mode MsvMode { get; set; } = MSV_Mode.LOCAL;

        /// <summary>vtrId</summary>
        public int VtrId { get; set; } = -1;

        /// <summary>当前用户Code</summary>
        public string UserCode { get; set; } = string.Empty;

        /// <summary>kamataki信息</summary>
        public string KamatakiInfo { get; set; } = string.Empty;

        /// <summary>上载模式</summary>
        public Upload_Mode UploadMode { get; set; } = Upload_Mode.NOUPLOAD;

        /// <summary>通道索引</summary>
        //public int ChannelIndex { get; set; }
    }
}
