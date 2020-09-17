using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Xml;
using IngestTask.Tools.Dto;

namespace IngestTask.Tools.Msv
{
    public class MsvClientCtrlSDK
    {
        //private string m_msvUdpIp = "127.0.0.1";
        //private int m_msvUdpPort;
        //private CClientTaskSDK SDK;
        private TASK_PARAM m_taskParam;
        private TASK_ALL_PARAM_NEW m_taskAllParam;
        private string m_strCaptureParam = "";
        private XmlDocument _xml = new XmlDocument();
        private CClientTaskSDKImp _clientSdk;
        public MsvClientCtrlSDK()
        {
            //m_msvUdpIp = strMsvIp;
            //m_msvUdpPort = msvPort;
            m_taskParam = new TASK_PARAM();
            m_taskAllParam = new TASK_ALL_PARAM_NEW();
            _clientSdk = new CClientTaskSDKImp();
        }
        
        public void ClientParam2MSVTskParam(TaskParam tmptaskparam, ref TASK_PARAM pTaskparam)
        {
            if (tmptaskparam != null)
            {
                pTaskparam.ulID = tmptaskparam.taskID;
                pTaskparam.nInOutCount = tmptaskparam.inOutCount;
                pTaskparam.strName = tmptaskparam.taskName;
                if (pTaskparam.nInOutCount > 0)
                {
                    pTaskparam.dwInFrame = new ulong[100];
                    pTaskparam.dwOutFrame = new ulong[100];
                    pTaskparam.dwTotalFrame = new ulong[100];
                    for (int i = 0; i < pTaskparam.nInOutCount; i++)
                    {
                        pTaskparam.dwInFrame[i] = Convert.ToUInt64(tmptaskparam.inFrame[i]);
                        pTaskparam.dwOutFrame[i] = Convert.ToUInt64(tmptaskparam.outFrame[i]);
                        pTaskparam.dwTotalFrame[i] = Convert.ToUInt64(tmptaskparam.totalFrame[i]);
                    }

                    //pTaskparam.bUseTime = 0;
                    pTaskparam.bUseTime = false;
                }
                else
                {
                    // Add by chenzhi 2012-01-14
                    // TODO: 增加对计划任务采集的支持
                    //if (tmptaskparam.isPlanMode)
                    //{
                    //    pTaskparam.bUseTime = 2;
                    //}
                    //else
                    //{
                    //    pTaskparam.bUseTime = 1;
                    //}
                    pTaskparam.bUseTime = true;
                }

            }

        }
        public void MSVTskParam2ClientParam(TASK_PARAM pTaskparam, ref TaskParam tmptaskparam)
        {
            tmptaskparam.taskID = Convert.ToInt32(pTaskparam.ulID);
            tmptaskparam.inOutCount = pTaskparam.nInOutCount;
            tmptaskparam.cutLen = 0;
            tmptaskparam.isRetro = pTaskparam.bRetrospect;
            //byte[] y = new byte[4096];
            //y = pTaskparam.TaskName.Cast<byte>().ToArray();
            tmptaskparam.taskName = pTaskparam.strName;
            int nlen = 0;
            nlen = pTaskparam.dwInFrame.Length;
            tmptaskparam.inOutCount = nlen;
            tmptaskparam.tmBeg = pTaskparam.tmBeg;
            tmptaskparam.inFrame = new int[100];
            tmptaskparam.outFrame = new int[100];
            tmptaskparam.totalFrame = new int[100];
            for (int i = 0; i < 100; i++)
            {
                tmptaskparam.inFrame[i] = Convert.ToInt32(pTaskparam.dwInFrame[i]);
                tmptaskparam.outFrame[i] = Convert.ToInt32(pTaskparam.dwOutFrame[i]);
                tmptaskparam.totalFrame[i] = Convert.ToInt32(pTaskparam.dwTotalFrame[i]);
            }
            tmptaskparam.retroFrame = 0;
            tmptaskparam.isPlanMode = true;
            tmptaskparam.tmBeg = pTaskparam.tmBeg;
        }
        public bool QuerySDIFormat(int nChPort, string strMsvIP, out int pnSingleType, Sobey.Core.Log.ILogger logger)
        {
            SDISignalStatus singleType = new SDISignalStatus();
            bool bIsBack = false;
            pnSingleType = 1;
            MSV_RET ret;
            try
            {
                ret = _clientSdk.MSVQuerySDIFormat(strMsvIP, ref singleType, ref bIsBack, logger, nChPort);
                if (ret == MSV_RET.MSV_NETERROR)
                {
                    logger.Error($"Cast Interface Function MSVQuerySDIFormat Error!(error {_clientSdk.MSVGetLastErrorString()})...........MsvUdpClientCtrlSDK::QuerySDIFormat");
                    return false;
                }
                if (ret != MSV_RET.MSV_SUCCESS)
                {
                    logger.Error($"Cast Interface Function MSVQuerySDIFormat Error!(error {_clientSdk.MSVGetLastErrorString()})...........MsvUdpClientCtrlSDK::QuerySDIFormat");
                    return false;
                }

                logger.Info($"Cast Interface Function QuerySDIFormat!(vedioformat ={singleType.VideoFormat} :width :{singleType.nWidth})...........MsvUdpClientCtrlSDK::QuerySDIFormat");

                bool bValidVideo = false;
                switch (singleType.VideoFormat)
                {

                    case SignalFormat._invalid_vid_format:
                        pnSingleType = 254;
                        break;
                    //case 4095:
                    //	{
                    //		bValidVideo = TRUE;	
                    //	}
                    case SignalFormat._unknown_vid_format:
                        pnSingleType = 255;
                        break;
                    default:
                        bValidVideo = true;
                        break;
                }
                if (bValidVideo)
                {
                    if (singleType.nWidth > 0 && singleType.nWidth <= 720)
                    {
                        pnSingleType = 0;
                    }
                    else if (singleType.nWidth > 720)
                    {
                        pnSingleType = 1;
                    }
                    else
                    {
                        pnSingleType = 254;
                        bValidVideo = false;
                    }
                        

                }
                return bValidVideo;
            }
            catch (System.Exception e)
            {
                logger.Error($"Cast Interface Function MSVQuerySDIFormat Exception! {e.Message})...........MsvUdpClientCtrlSDK::QuerySDIFormat");
                pnSingleType = 254;
                return false;
            }
        }

        public bool Record(int nChPort, string strMsvIP, out int nMsvRet, Sobey.Core.Log.ILogger logger)
        {
            MSV_RET ret;
            string strMutPath = "";
            string strPath = "";
            nMsvRet = 0;
            try
            {

                _xml.LoadXml(m_strCaptureParam);
                XmlElement _root = _xml.DocumentElement;
                XmlNode pathNode = _root.SelectSingleNode("multiDest");
                if (pathNode != null)
                {
                    strPath = pathNode.InnerText;
                    strMutPath = string.Format("<multiDest><taskid>{0}</taskid>{1}</multiDest>", m_taskParam.ulID, strPath);
                    ret = _clientSdk.MSVSetMulDestPath(strMsvIP, strMutPath, logger);
                    if (ret == MSV_RET.MSV_NETERROR)
                    {
                        logger.Error($"MSVSetMulDestPath::taskName={m_taskParam.strName};Error:{_clientSdk.MSVGetLastErrorString()}!");
                    }
                }

                m_taskAllParam.nCutLen = 10;
                logger.Info($"MsvSDK Record Prepare Cast MSVStartTaskNew Function ip={strMsvIP} port={nChPort} cutlen={m_taskAllParam.nCutLen}");

                ret = _clientSdk.MSVStartTaskNew(strMsvIP, m_taskAllParam, nChPort, logger);
                nMsvRet = Convert.ToInt32(ret);
                if (ret == MSV_RET.MSV_NETERROR)
                {
                    logger.Error("MsvSDK Record Failed(MSV_NETERROR)!...........MsvUdpClientCtrlSDK::Record");
                    return false;
                }
                if (ret != MSV_RET.MSV_SUCCESS)
                {
                    logger.Error($"MsvSDK Record Failed {ret} {_clientSdk.MSVGetLastErrorString()}");
                    return false;
                }
                else if (ret == MSV_RET.MSV_SUCCESS)
                    logger.Info("MsvSDK Record End!...........MsvUdpClientCtrlSDK::Record");
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::Record, Exception:{e.Message}");
                return false;
            }
            return true;
        }

        public bool RecordReady(int nChPort, string strMsvIP, ref TaskParam pTaskparam, string strTaskName, string pCaptureparam, Sobey.Core.Log.ILogger logger)
        {
            if (pTaskparam == null)
            {
                logger.Error("RecordReady: pTaskparam is null!");
                return false;
            }
            TASK_PARAM param = new TASK_PARAM();
            try
            {
                logger.Info($"MsvSDK Record Ready!,taskID:{pTaskparam.taskID},pCaptureparam:{pCaptureparam} ");
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(pCaptureparam);
                XmlNode root = doc.DocumentElement;
                //string curDate = DateTime.Now.ToString("yyyy-MM-dd") + '\\';
                string curDate = DateTime.Now.ToString("yyyy-MM-dd");
                if (root != null)
                {
                    //XmlNode _capTure = root.SelectSingleNode("CAPTUREPARAM"); 

                    XmlNode _fileName0 = root.SelectSingleNode("path0FileName");
                    if (_fileName0 != null)
                    {
                        string fileName0 = _fileName0.InnerText;
                        //fileName0 += curDate;
                        //                         int nIndex = fileName0.LastIndexOf('\\');
                        //                         if (nIndex == fileName0.Length - 1)
                        //                             fileName0 = fileName0.Substring(0, nIndex);
                        //                         int nPos = fileName0.LastIndexOf('\\');
                        //                         fileName0 = fileName0.Insert(nPos + 1, curDate);
                        //fileName0 = fileName0.Substring(0, nPos + 1);
                        //fileName0 = fileName0 + curDate;
                        fileName0 = fileName0.Replace("yyyy-mm-dd", curDate);
                        _fileName0.InnerText = fileName0;
                        
                    }
                    else
                    {
                        logger.Error("Not find fileName0:");
                    }
                    XmlNode _fileName1 = root.SelectSingleNode("path1FileName");
                    if (_fileName1 != null)
                    {
                        string fileName1 = _fileName1.InnerText;
                        //fileName1 += curDate;
                        //                         int nIndex = fileName1.LastIndexOf('\\');
                        //                         if (nIndex == fileName1.Length - 1)
                        //                             fileName1 = fileName1.Substring(0, nIndex);
                        //                         int nPos = fileName1.LastIndexOf('\\');
                        //                         fileName1 = fileName1.Insert(nPos + 1, curDate);
                        //fileName1 = fileName1.Substring(0, nPos + 1);
                        //fileName1 = fileName1 + curDate;
                        fileName1 = fileName1.Replace("yyyy-mm-dd", curDate);
                        _fileName1.InnerText = fileName1;
                    }
                    else
                    {
                        logger.Error("Not find fileName1:");
                    }
                }
                else
                {
                    logger.Error("root is null");
                }
                pCaptureparam = doc.InnerXml;
                logger.Info($"MsvSDK Record Ready!, taskID:{pTaskparam.taskID}, lastCapture:{pCaptureparam}...........RecordReady");
                ClientParam2MSVTskParam(pTaskparam, ref param);
                m_taskParam.bRetrospect = param.bRetrospect;
                m_taskParam.bUseTime = param.bUseTime;
                m_taskParam.channel = param.channel;
                m_taskParam.dwCutClipFrame = param.dwCutClipFrame;
                m_taskParam.nInOutCount = param.nInOutCount;
                m_taskParam.strName = param.strName;
                for (int i = 0; i < m_taskParam.nInOutCount; i++)
                {
                    m_taskParam.dwInFrame[i] = param.dwInFrame[i];
                    m_taskParam.dwOutFrame[i] = param.dwOutFrame[i];
                    m_taskParam.dwTotalFrame[i] = param.dwTotalFrame[i];
                }


                m_taskParam.nSignalID = param.nSignalID;
                m_taskParam.nTimeCode = param.nTimeCode;
                //m_taskparam.strDesc.Format(_T("%s"),pTaskparam->strDesc);
                //m_taskParam.strName = strTaskName;

                m_taskParam.tmBeg = DateTime.Now;
                m_taskParam.tmEnd = DateTime.Now;
                m_taskParam.ulID = param.ulID;
                m_taskParam.TaskMode = (TASK_MODE)param.TaskMode;
                m_taskParam.TaskState = (TASK_STATE)param.TaskState;


                m_taskAllParam.captureParam = pCaptureparam;
                m_taskAllParam.nCutLen = 10;
                m_taskAllParam.taskParam = m_taskParam;
                //m_taskAllParam.taskParam.strName = strTaskName;
                m_strCaptureParam = "";
                m_strCaptureParam = pCaptureparam;
                logger.Info($"curent strTaskName: {strTaskName}");
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::RecordReady, Exception:{e.Message}");
                return false;
            }
            return true;
        }

        public bool QueryTaskState(int nChPort, string strMsvIP, out TaskParam tskparam, Sobey.Core.Log.ILogger logger)
        {
            MSV_RET ret;
            TASK_PARAM info = new TASK_PARAM();
            tskparam = new TaskParam();
            logger.Info($"MsvSDK prepare QueryTaskState(ip={strMsvIP})");
            try
            {
                ret = _clientSdk.MSVQueryRuningTask(strMsvIP, ref info, nChPort, logger);

                if (ret == MSV_RET.MSV_NETERROR)
                {
                    logger.Error($" MSVQueryRuningTask net Error {_clientSdk.MSVGetLastErrorString()}");
                    return false;
                }
                if (ret != MSV_RET.MSV_SUCCESS)
                {
                    logger.Error($" MSVQueryRuningTask Error {_clientSdk.MSVGetLastErrorString()}");
                    return false;
                }
                //string strbtime = info.tmBeg.ToString("yy-MM-dd hh:mm:ss");
                //string stretime = info.tmBeg.ToString("yy-MM-dd hh:mm:ss");
                //info.tmBeg = Convert.ToDateTime(strbtime);
                //info.tmEnd = Convert.ToDateTime(stretime);
                MSVTskParam2ClientParam(info, ref tskparam);
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::QueryTaskState, Exception:{e.Message}");
                return false;
            }
            return true;
        }
        public Device_State QueryDeviceState(int nChPort, string strMsvIP, Sobey.Core.Log.ILogger logger)
        {
            MSV_STATE state = new MSV_STATE();
            MSV_RET ret;
            try
            {
                
                ret = _clientSdk.MSVQueryState(strMsvIP, ref state, nChPort, logger);
                if (ret == MSV_RET.MSV_NETERROR)
                {
                    logger.Error($"MSVQueryState MSV_NETERROR, {strMsvIP}:{nChPort}, error: {_clientSdk.MSVGetLastErrorString()}");
                    return Device_State.DISCONNECTTED;
                }
                if (ret != MSV_RET.MSV_SUCCESS)
                {
                    logger.Error($"MSVQueryState failed, {strMsvIP}:{nChPort}, error: {_clientSdk.MSVGetLastErrorString()}");
                    return Device_State.DISCONNECTTED;
                }
                logger.Info($"MSVQueryState End, state:{state.msv_capture_state}......");
                if (state.msv_capture_state == CAPTURE_STATE.CS_PAUSE || state.msv_capture_state == CAPTURE_STATE.CS_CAPTURE)
                    return Device_State.WORKING;
                else
                    return Device_State.CONNECTED;
                
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::QueryState, Exception {e.Message}");
                return Device_State.DISCONNECTTED;
            }
        }

        public bool GetLastErrorInfo(int nChPort, string strMsvIP, out _ErrorInfo pErrorInfo)
        {
            pErrorInfo = new _ErrorInfo();
            try
            {
               
                pErrorInfo.errStr = _clientSdk.MSVGetLastErrorString();
            }
            catch (Exception )
            {
                return false;
            }
            return true;
        }

        public bool Stop(int nChPort, string strMsvIP, int taskID, out int nMsvStopRet, Sobey.Core.Log.ILogger logger)
        {
            MSV_RET ret;
            nMsvStopRet = 0;
            try
            {
                long nRetTaskId = -1;
                ret = _clientSdk.MSVStopTask(strMsvIP, ref nRetTaskId, taskID, nChPort, logger);
                nMsvStopRet = Convert.ToInt32(ret);
                if (ret != MSV_RET.MSV_SUCCESS)
                {
                    logger.Error($"MSVStopTask failed, error:{_clientSdk.MSVGetLastErrorString()} ");
                    return false;
                }
                logger.Info($"MSVStopTask end,MsvUdpClientCtrlSDK::Stop nRetTaskId::{nRetTaskId} ");
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::Stop, Exception:{e.Message} ");
                return false;
            }
            return true;
        }

        public bool Trace(int nChPort, string strMsvIP, ref TaskParam pTaskparam, string strTaskName, string pCaptureparam, Sobey.Core.Log.ILogger logger)
        {
            MSV_RET ret;
            try
            {
                TASK_PARAM tmptaskparam = new TASK_PARAM();
                ClientParam2MSVTskParam(pTaskparam, ref tmptaskparam);
                TASK_ALL_PARAM_NEW task = new TASK_ALL_PARAM_NEW();
                task.taskParam.ulID = tmptaskparam.ulID;
                task.taskParam.strName = strTaskName;
                task.nCutLen = 10;
                task.captureParam = pCaptureparam;
                ret = _clientSdk.MSVStartRetrospectTask(strMsvIP, task, nChPort, logger);
                if (ret != MSV_RET.MSV_SUCCESS)
                {
                    logger.Error($"MSVStartRetrospectTask falied, error:{_clientSdk.MSVGetLastErrorString()} ");
                    return false;
                }
                logger.Info($"MSVStartRetrospectTask end......MsvUdpClientCtrlSDK Trace");
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::Trace falied, Exception:{e.Message} ");
                return false;
            }
            return true;
        }

        public bool QuerySignalStatus(int nChPort, string strMsvIP, out SDISignalDetails _SDISignalDetails, Sobey.Core.Log.ILogger logger)
        {
            MSV_RET ret;
            SDISignalStatus status = new SDISignalStatus();
            _SDISignalDetails = new SDISignalDetails();
            bool bIsBlack = false;
            try
            {
                ret = _clientSdk.MSVQuerySDIFormat(strMsvIP, ref status, ref bIsBlack, logger, nChPort);

                if (ret != MSV_RET.MSV_SUCCESS)
                {
                    logger.Error($"Cast Interface Function QuerySignalStatus Error!(error{_clientSdk.MSVGetLastErrorString()}");
                    return false;
                }
                //处理数据
                if (status.VideoFormat != SignalFormat._invalid_vid_format && status.VideoFormat != SignalFormat._unknown_vid_format && status.nWidth > 0)
                {
                    if (status.nWidth <= 720)
                        _SDISignalDetails.nSDIFormat = 0;
                    else
                        _SDISignalDetails.nSDIFormat = 1;
                }

                _SDISignalDetails.nWidth = status.nWidth;
                _SDISignalDetails.nHeight = status.nHeight;
                _SDISignalDetails.nDFMode = Convert.ToInt32(status.TCMode);
                _SDISignalDetails.fFrameRate = status.fFrameRate;
                _SDISignalDetails.bIsBlack = Convert.ToBoolean(bIsBlack);
            }
            catch (System.Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::QuerySignalStatus Exception:{e.Message}");

                return false;
            }
            return true;
        }
    }
}
