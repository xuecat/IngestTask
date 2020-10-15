using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Dto
{
    /// <summary>信号源详情</summary>
    public class SignalSrc
    {
        /// <summary>Id</summary>
        public int Id { get; set; }

        /// <summary>名称</summary>
        public string Name { get; set; }

        /// <summary>详情描述</summary>
        public string Desc { get; set; }

        /// <summary>信号源类型Id（高标清）</summary>
        public int TypeId { get; set; }

        /// <summary>图像类型;0、为素材原始比例，1、图像为4:3的方式，2:图像为16:9的方式</summary>
        public int ImageType { get; set; }

        /// <summary>表示是否是纯音频信号源</summary>
        public int PureAudio { get; set; }
    }
}
