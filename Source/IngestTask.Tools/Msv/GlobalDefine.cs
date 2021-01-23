using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IngestTask.Tool.Msv
{
    //SDI信号源的详细信息
    public class SDISignalDetails
    {
        public int nSDIFormat { get; set; }
		public int nDFMode { get; set; }
		public int nWidth { get; set; }
		public int nHeight { get; set; }
		public float fFrameRate { get; set; }
		public bool bIsBlack { get; set; }
		public SDISignalDetails()
        {
            nSDIFormat = -1;
            nDFMode = 2;
            nWidth = 0;
            nHeight = 0;
            fFrameRate = 0.0F;
            bIsBlack = false;
        }

    }
    public class TaskParam
	{
		public int taskID { get; set; }                          //任务ID
		public int cutLen { get; set; }                          //任务分段长度
		public int inOutCount { get; set; }                      //上载时多出和入点计数
#pragma warning disable CA1819 // 属性不应返回数组
		public int[] inFrame { get; set; } = new int[100];       //多出入点开始贞数组
        public int[] outFrame { get; set; } = new int[100];      //多出入点结束贞数组
        public int[] totalFrame { get; set; } = new int[100];    //任务总帧数数组(防止出点找不到的情况)
#pragma warning restore CA1819 // 属性不应返回数组

        public string taskName { get; set; }                 //任务名称
		public bool isRetro { get; set; }                        //是否是回溯任务
		public int retroFrame { get; set; }                      //回溯的帧数
																 // TODO: 为MSV的计划任务模式传递任务开始时间
		public DateTime tmBeg { get; set; }                      //任务采集开始时间
		public bool isPlanMode { get; set; }                 //是否为计划任务

		public TaskParam()
		{
			taskID = 0;
			cutLen = 0;
			inOutCount = 0;
			isRetro = false;
			retroFrame = 0;
			tmBeg = DateTime.Now; // 当前时间
			isPlanMode = false;             // 不是计划任务模式，默认值
		}
		
	}
	//public struct _MSVC_TASK_PARAM
	//{
	//	public int bRetrospect;
	//	public int bUseTime;
	//	public int channel;
	//	public int dwCutClipFrame;
	//	public int[] dwInFrame;
	//	public int[] dwOutFrame;
	//	public int[] dwTotalFrame;
	//	public int nInOutCount;
	//	public int nSignalID;
	//	public int nTimeCode;
	//	public int TaskMode;
	//	public sbyte[] TaskName;
	//	public int TaskState;
	//	public int ulID;
	//}

	public class _ErrorInfo
	{
        public string errStr { get; set; }
        public string getErrorInfo()
        {
            return errStr;
        }
	}

	public static class G2CommonFun
	{
		public static string ObjectToJson(object obj)
		{
			return JsonConvert.SerializeObject(obj);
		}

		public static object JsonToObject(string strJson,Type type)
		{
			return JsonConvert.DeserializeObject(strJson,type);
		}
	}



}
