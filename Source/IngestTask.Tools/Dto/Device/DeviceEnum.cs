using System;
using System.Collections.Generic;
using System.Text;

namespace IngestTask.Tools.Dto
{
    /// <summary> 信号来源 </summary>
    public enum emSignalSource
    {
        /// <summary>卫星</summary>
        emSatlitlleSource = 0,

        /// <summary>总控矩阵</summary>
        emCtrlMatrixSource = 1,

        /// <summary>视频服务器</summary>
        emVideoServerSource = 2,

        /// <summary>VTR</summary>
        emVtrSource = 3,

        /// <summary>MSV</summary>
        emMSVSource = 4,

        /// <summary>蓝光</summary>
        emXDCAM = 5,

        /// <summary>IPTS流</summary>
        emIPTS = 6,

        /// <summary>流媒体</summary>
        emStreamMedia = 7
    }

    /// <summary> 设备类型 </summary>
    public enum emDeviceType
    {
        /// <summary>MSV</summary>
        emDeviceMSV = 0,

        /// <summary>VTR</summary>
        emDeviceVTR = 1,

        /// <summary>蓝光</summary>
        emDeviceXDCAM = 2
    }

    /// <summary> 采集通道类型 </summary>
    public enum CaptureChannelType
    {
        /// <summary> 默认通道时msv通道，为啥有这个是因为前端乱存数据msv存0，所以为了兼容msv和默认是一样 </summary>
        emDefualtChannel = 0,
        /// <summary> MSV 采集通道 </summary>
        emMsvChannel = 1,

        /// <summary> IPTS 虚拟的通道 </summary>
        emIPTSChannel,

        /// <summary> 流媒体通道 </summary>
        emStreamChannel,
        emIPSChannel                //IPS2110 
    }

    /// <summary>通道状态</summary>
    public enum Channel_State
    {
        CS_Null = 0,
        CS_Idle,
        CS_Ready,
        CS_Capturing,
        CS_Error,
    }

    /// <summary>通道类型</summary>
    public enum Channel_Type
    {
        CT_SDI = 0,
        CT_TS,
        CT_Stream,
    }

    /// <summary> 任务备份属性 </summary>
    public enum emBackupFlag
    {
        /// <summary> 不允许备份 </summary>
		emNoAllowBackUp = 0,

        /// <summary> 允许备份 </summary>
		emAllowBackUp = 1,

        /// <summary> 只允许作备份 </summary>
		emBackupOnly = 2
    }

    /// <summary>设备状态</summary>
    public enum Device_State
    {
        /// <summary>没有连接</summary>
        DISCONNECTTED,

        /// <summary>已经连接</summary>
        CONNECTED,

        /// <summary>正在采集</summary>
        WORKING
    }

    /// <summary>MSV模式</summary>
    public enum MSV_Mode
    {
        /// <summary>本地</summary>
        LOCAL,

        /// <summary>网络</summary>
        NETWORK
    }

    /// <summary>上载模式</summary>
    public enum Upload_Mode
    {
        /// <summary></summary>
        NOUPLOAD,

        /// <summary></summary>
        CANUPLOAD,

        /// <summary>上载独占</summary>
        ONLYUPLOAD
    }
    /// <summary>程序类型</summary>
    public enum ProgrammeType
    {
        PT_Null = -1,
        PT_SDI,
        PT_IPTS,
        PT_StreamMedia
    }

    /// <summary>图像类型</summary>
    public enum ImageType
    {
        IT_Null = -1,
        IT_Original = 0,
        IT_SD_4_3 = 1,
        IT_SD_16_9 = 2,
        IT_HD_16_9 = 4
    }
}
