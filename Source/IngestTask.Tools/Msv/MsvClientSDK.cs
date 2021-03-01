using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Xml;
using IngestTask.Dto;

namespace IngestTask.Tool.Msv
{
    public class MsvClientCtrlSDK
    {
        //private string m_msvUdpIp = "127.0.0.1";
        //private int m_msvUdpPort;
        ////private CClientTaskSDK SDK;
        //private TASK_PARAM m_taskParam;
        //private TASK_ALL_PARAM_NEW m_taskAllParam;
        //private string m_strCaptureParam = "";
        private CClientTaskSDKImp _clientSdk;
        public MsvClientCtrlSDK()
        {
            //m_msvUdpIp = strMsvIp;
            //m_msvUdpPort = msvPort;
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

        private bool IsTestDevice(string ip, int port)
        {
            if (port == 110)
            {
                return true;
            }
            return false;
        }

        public async Task<SDISignalStatus> QuerySDIFormatAsync(int nChPort, string strMsvIP, Sobey.Core.Log.ILogger logger)
        {
           
            try
            {
                //ret = _clientSdk.MSVQuerySDIFormat(strMsvIP, ref singleType, ref bIsBack, logger, nChPort);
                var ret = await _clientSdk.MSVQuerySDIFormatAsync(strMsvIP, logger, nChPort).ConfigureAwait(true);
                if (ret == null)
                {
                    logger.Error($"Cast Interface Function MSVQuerySDIFormat Error!(error {_clientSdk.MSVGetLastErrorString()})...........MsvUdpClientCtrlSDK::QuerySDIFormat");
                    return null;
                }
                
                logger.Info($"Cast Interface Function QuerySDIFormat!(vedioformat ={ret.VideoFormat} :width :{ret.nWidth})...........MsvUdpClientCtrlSDK::QuerySDIFormat");

                int pnSingleType = 255;
                bool bValidVideo = false;
                switch (ret.VideoFormat)
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
                    if (ret.nWidth > 0 && ret.nWidth <= 720)
                    {
                        pnSingleType = 0;
                    }
                    else if (ret.nWidth >= 3840)
                        pnSingleType = 2;
                    else if (ret.nWidth > 720)
                    {
                        pnSingleType = 1;
                    }
                    else
                    {
                        pnSingleType = 254;
                        bValidVideo = false;
                    }

                    ret.SignalType = pnSingleType;
                    return ret;
                }
                return null;
            }
            catch (System.Exception e)
            {
                logger.Error($"Cast Interface Function MSVQuerySDIFormat Exception! {e.Message})...........MsvUdpClientCtrlSDK::QuerySDIFormat");
               
                return null;
            }
        }

        public async Task<bool> RecordAsync(TASK_ALL_PARAM_NEW allparam, int nChPort, string strMsvIP, Sobey.Core.Log.ILogger logger)
        {
            MSV_RET ret;
            string strMutPath = "";
            string strPath = "";
            int nMsvRet = 0;
            try
            {
                var _xml = new XmlDocument();
                _xml.LoadXml(allparam.captureParam);
                XmlElement _root = _xml.DocumentElement;
                XmlNode pathNode = _root.SelectSingleNode("multiDest");
                if (pathNode != null)
                {
                    strPath = pathNode.InnerText;
                    strMutPath = string.Format("<multiDest><taskid>{0}</taskid>{1}</multiDest>", allparam.taskParam.ulID, strPath);
                    //ret = _clientSdk.MSVSetMulDestPath(strMsvIP, strMutPath, logger);
                    ret = await _clientSdk.MSVSetMulDestPathAsync(strMsvIP, nChPort, strMutPath, logger).ConfigureAwait(true);
                    if (ret == MSV_RET.MSV_NETERROR)
                    {
                        logger.Error($"MSVSetMulDestPath::taskName={allparam.taskParam.strName};Error:{_clientSdk.MSVGetLastErrorString()}!");
                    }
                }

                allparam.nCutLen = 10;
                logger.Info($"MsvSDK Record Prepare Cast MSVStartTaskNew Function ip={strMsvIP} port={nChPort} cutlen={allparam.nCutLen}");

                //ret = _clientSdk.MSVStartTaskNew(strMsvIP, m_taskAllParam, nChPort, logger);
                ret = await _clientSdk.MSVStartTaskNewAsync(strMsvIP, allparam, nChPort, logger).ConfigureAwait(true); ;
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
                return true;
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::Record, Exception:{e.Message}");
                return false;
            }
        }

        public TASK_ALL_PARAM_NEW RecordReady(int nChPort, string strMsvIP, TaskParam pTaskparam, string strTaskName, string pCaptureparam, Sobey.Core.Log.ILogger logger)
        {
            if (pTaskparam == null)
            {
                logger.Error("RecordReady: pTaskparam is null!");
                return null;
            }
            TASK_PARAM param = new TASK_PARAM();
            TASK_PARAM taskParam = new TASK_PARAM();

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
                taskParam.bRetrospect = param.bRetrospect;
                taskParam.bUseTime = param.bUseTime;
                taskParam.channel = param.channel;
                taskParam.dwCutClipFrame = param.dwCutClipFrame;
                taskParam.nInOutCount = param.nInOutCount;
                taskParam.strName = param.strName;
                for (int i = 0; i < taskParam.nInOutCount; i++)
                {
                    taskParam.dwInFrame[i] = param.dwInFrame[i];
                    taskParam.dwOutFrame[i] = param.dwOutFrame[i];
                    taskParam.dwTotalFrame[i] = param.dwTotalFrame[i];
                }


                taskParam.nSignalID = param.nSignalID;
                taskParam.nTimeCode = param.nTimeCode;
                //m_taskparam.strDesc.Format(_T("%s"),pTaskparam->strDesc);
                //m_taskParam.strName = strTaskName;

                taskParam.tmBeg = DateTime.Now;
                taskParam.tmEnd = DateTime.Now;
                taskParam.ulID = param.ulID;
                taskParam.TaskMode = (TASK_MODE)param.TaskMode;
                taskParam.TaskState = (TASK_STATE)param.TaskState;

                //m_taskAllParam.captureParam = pCaptureparam;
                //m_taskAllParam.nCutLen = 10;
                //m_taskAllParam.taskParam = m_taskParam;
                //m_taskAllParam.taskParam.strName = strTaskName;
                logger.Info($"curent strTaskName: {strTaskName}");

                return new TASK_ALL_PARAM_NEW() { captureParam = pCaptureparam, nCutLen = 10, taskParam = taskParam};
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::RecordReady, Exception:{e.Message}");
                return null;
            }
        }

        public async Task<TASK_PARAM> QueryTaskInfoAsync(int nChPort, string strMsvIP, /*int taskid,*/ Sobey.Core.Log.ILogger logger)
        {
            logger.Info($"MsvSDK prepare QueryTaskState(ip={strMsvIP})");
            try
            {
                TASK_PARAM info = IsTestDevice(strMsvIP, nChPort) ?
                    await _clientSdk.Test_MSVQueryRuningTaskAsync(strMsvIP, nChPort, 0, logger).ConfigureAwait(true)
                    : await _clientSdk.MSVQueryRuningTaskAsync(strMsvIP,  nChPort, logger).ConfigureAwait(true);

                if (info == null)
                {
                    logger.Error($" MSVQueryRuningTask net Error {_clientSdk.MSVGetLastErrorString()}");
                    return null;
                }

                //string strbtime = info.tmBeg.ToString("yy-MM-dd hh:mm:ss");
                //string stretime = info.tmBeg.ToString("yy-MM-dd hh:mm:ss");
                //info.tmBeg = Convert.ToDateTime(strbtime);
                //info.tmEnd = Convert.ToDateTime(stretime);
                //MSVTskParam2ClientParam(info, ref tskparam);
                return info;
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::QueryTaskState, Exception:{e.Message}");
                return null;
            }
        }

        //capture 刚发采集命令发完要完全确认状态
        public async Task<Device_State> QueryDeviceStateAsync(int nChPort, string strMsvIP, bool capture, Sobey.Core.Log.ILogger logger)
        {
            try
            {

                MSV_STATE state = await _clientSdk.MSVQueryStateAsync(strMsvIP, nChPort, logger).ConfigureAwait(true);
                if (state == null)
                {
                    logger.Error($"MSVQueryState MSV_NETERROR, {strMsvIP}:{nChPort}, error: {_clientSdk.MSVGetLastErrorString()}");
                    return Device_State.DISCONNECTTED;
                }
               
                logger.Info($"MSVQueryState End, state:{state.msv_capture_state}......");

                if (capture)
                {
                    if (state.msv_capture_state == CAPTURE_STATE.CS_CAPTURE || state.msv_capture_state == CAPTURE_STATE.CS_CAPTURE_ACTIVE)
                    {
                        return Device_State.WORKING;
                    }
                    else
                        return Device_State.CONNECTED;
                }
                else
                {
                    if (state.msv_capture_state == CAPTURE_STATE.CS_PAUSE || state.msv_capture_state == CAPTURE_STATE.CS_CAPTURE)
                        return Device_State.WORKING;
                    else
                        return Device_State.CONNECTED;
                }
                
                
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

        public async Task<long> StopAsync(int nChPort, string strMsvIP, int taskID, Sobey.Core.Log.ILogger logger)
        {
            try
            {
                long nRetTaskId = await _clientSdk.MSVStopTaskAsync(strMsvIP, taskID, nChPort, logger).ConfigureAwait(true);
                if (nRetTaskId <= 0)
                {
                    logger.Error($"MSVStopTask failed, error:{_clientSdk.MSVGetLastErrorString()} ");
                    return 0;
                }
                logger.Info($"MSVStopTask end,MsvUdpClientCtrlSDK::Stop nRetTaskId::{nRetTaskId} ");
                return nRetTaskId;
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::Stop, Exception:{e.Message} ");
                return 0;
            }
        }

        public async Task<TaskParam> TraceAsync(int nChPort, string strMsvIP, TaskParam pTaskparam, string strTaskName, string pCaptureparam, Sobey.Core.Log.ILogger logger)
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
                //ret = _clientSdk.MSVStartRetrospectTask(strMsvIP, task, nChPort, logger);
                ret = await _clientSdk.MSVStartRetrospectTaskAsync(strMsvIP, task, nChPort, logger).ConfigureAwait(true);
                if (ret != MSV_RET.MSV_SUCCESS)
                {
                    logger.Error($"MSVStartRetrospectTask falied, error:{_clientSdk.MSVGetLastErrorString()} ");
                    return pTaskparam;
                }
                logger.Info($"MSVStartRetrospectTask end......MsvUdpClientCtrlSDK Trace");
            }
            catch (Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::Trace falied, Exception:{e.Message} ");
                return null;
            }
            return null;
        }

        public async Task<SDISignalDetails> QuerySignalStatusAsync(int nChPort, string strMsvIP, Sobey.Core.Log.ILogger logger)
        {
            SDISignalDetails _SDISignalDetails = new SDISignalDetails();
            bool bIsBlack = false;
            try
            {
                //ret = _clientSdk.MSVQuerySDIFormat(strMsvIP, ref status, ref bIsBlack, logger, nChPort);
                var retback = await _clientSdk.MSVQuerySDIFormatAsync(strMsvIP, logger, nChPort).ConfigureAwait(true);

                if (retback == null)
                {
                    logger.Error($"Cast Interface Function QuerySignalStatus Error!(error{_clientSdk.MSVGetLastErrorString()}");
                    return null;
                }
                //处理数据
                if (retback.VideoFormat != SignalFormat._invalid_vid_format && retback.VideoFormat != SignalFormat._unknown_vid_format && retback.nWidth > 0)
                {
                    if (retback.nWidth <= 720)
                        _SDISignalDetails.nSDIFormat = 0;
                    else
                        _SDISignalDetails.nSDIFormat = 1;
                }

                _SDISignalDetails.nWidth = retback.nWidth;
                _SDISignalDetails.nHeight = retback.nHeight;
                _SDISignalDetails.nDFMode = Convert.ToInt32(retback.TCMode);
                _SDISignalDetails.fFrameRate = retback.fFrameRate;
                _SDISignalDetails.bIsBlack = Convert.ToBoolean(bIsBlack);
                return _SDISignalDetails;
            }
            catch (System.Exception e)
            {
                logger.Error($"MsvUdpClientCtrlSDK::QuerySignalStatus Exception:{e.Message}");

                return null;
            }
        }
    }
}
