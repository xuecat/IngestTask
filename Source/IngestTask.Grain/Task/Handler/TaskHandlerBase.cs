

namespace IngestTask.Grain
{
    using IngestTask.Abstraction.Grains;
    using IngestTask.Dto;
    using IngestTask.Tool;
    using IngestTask.Tool.Msv;
    using Sobey.Core.Log;
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    public class TaskHandlerBase : ITaskHandler
    {
        protected ILogger Logger { get; set; }
        protected RestClient restClient { get; set; }

        protected MsvClientCtrlSDK msvClient { get; set; }
        public TaskHandlerBase(RestClient rest, MsvClientCtrlSDK msv)
        {
            Logger = LoggerManager.GetLogger("TaskHandlerBase");
            restClient = rest;
            msvClient = msv;
        }

        /*
         * 判断是否执行这个handle每个继承都必须要写这个
         */
        //static public bool IsHandler(TaskFullInfo task)

        public virtual ValueTask<int> HandleTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }

        public virtual ValueTask<int> StartTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }

        public virtual ValueTask<int> StopTaskAsync(TaskFullInfo task, ChannelInfo channel)
        {
            throw new NotImplementedException();
        }

        public virtual async ValueTask<bool> UnlockTaskAsync(int taskid, taskState tkstate, dispatchState dpstate, syncState systate)
        {
            if (await restClient.CompleteSynTasksAsync(taskid, tkstate, dpstate, systate))
            {
                Logger.Info($"taskbase UnlockTaskAsync success {taskid} {tkstate} {dpstate} {systate}");
            }
            else
            {
                Logger.Info($"taskbase UnlockTaskAsync failed {taskid} {tkstate} {dpstate} {systate}");
            }

            if (dpstate == dispatchState.dpsRedispatch
                || dpstate == dispatchState.dpsDispatchFailed)
            {
                //同步planing的状态为 PlanState.emPlanFailed
                //但是现在代码没有可以先不用写
                //SetPlanSourceListState(PluginsMgr.PlanState.emPlanFailed)
            }

            return false;
        }

        public virtual async Task<bool> HandleTieupTaskAsync(TaskContent info)
        {
            await UnlockTaskAsync(info.TaskId, taskState.tsNo, dispatchState.tsNo, syncState.ssSync);
            var lsttask = await restClient.GetChannelCapturingTaskInfoAsync(info.ChannelId);
            if (lsttask != null && lsttask.TaskType == TaskType.TT_MANUTASK)
            {
                await restClient.DeleteTaskAsync(info.TaskId).ConfigureAwait(true);

                //同步planing的状态为 PlanState.emPlanDeleted
                //但是现在代码没有可以先不用写
                //SetPlanSourceListState(PluginsMgr.PlanState.emPlanDeleted)
            }
            return false;
        }

        public string FormatCaptureParamPath(string strPath)
        {
            strPath = strPath.Trim();

            String strTime = DateTime.Now.ToString("YY-mm-dd");
            strPath = strPath.Replace("%YY-MM-DD%", strTime);

            //将非法字符替换为下划线
            strPath = strPath.Replace("\\\\", "\\");
            strPath = strPath.Replace(@"\\", @"\");
            if (strPath[0] == '\\')
            {
                strPath = "\\" + strPath;
            }
            
            //strPath = strPath.Replace("*", "_");
            //strPath = strPath.Replace("\"", "_");
            //strPath = strPath.Replace("/", "_");
            //strPath = strPath.Replace("<", "_");
            //strPath = strPath.Replace(">", "_");
            //strPath = strPath.Replace("|", "_");
            //strPath = strPath.Replace("?", "_");

            //最后处理冒号
            //int len = strPath.Length;
            
            //for (int i = 2; i < len; i++)
            //{
            //    if (strPath[i] == ':')
            //        strPath = strPath.Replace(strPath[i], '_');
            //}
            return strPath;
        }

        public TaskParam CreateTaskParam(TaskContent contentinfo)
        {
            var param = new TaskParam();
            param.taskID = contentinfo.TaskId;
            param.taskName = contentinfo.TaskName;
            param.tmBeg = DateTimeFormat.DateTimeFromString(contentinfo.Begin);
            return param;
        }

        public virtual int IsNeedRedispatchask(TaskFullInfo taskinfo)
        {
            throw new NotImplementedException();
        }

        public virtual async ValueTask<string> GetCaptureParmAsync(TaskFullInfo taskinfo, ChannelInfo channel)
        {
            string captureparam = taskinfo.CaptureMeta;
            var typeinfo = await msvClient.QuerySDIFormatAsync(channel.ChannelIndex, channel.Ip, Logger);
            if (typeinfo.VideoFormat == SignalFormat._unknown_vid_format)
            {
                //查询的制式出现了问题，重新再来一遍
                await Task.Delay(500);
                typeinfo = await msvClient.QuerySDIFormatAsync(channel.ChannelIndex, channel.Ip, Logger);
            }

            if (typeinfo != null && typeinfo.SignalType < 254)
            {
                /*oss路径过滤*/
                if (captureparam.IndexOf("&amp;") > 0 || captureparam.IndexOf("&lt;") > 0)
                {
                    captureparam = captureparam.Replace("&amp;", "&");
                    captureparam = captureparam.Replace("&lt;", "<");
                    captureparam = captureparam.Replace("&gt;", ">");
                }
                if (captureparam.IndexOf("&lt;") > 0)
                {
                    captureparam = captureparam.Replace("&amp;", "&");
                    captureparam = captureparam.Replace("&lt;", "<");
                    captureparam = captureparam.Replace("&gt;", ">");
                }

                //captureparam = captureparam.Replace("&", "&amp;");

                XElement capturenode = null;
                var root = XDocument.Parse(captureparam);
                if (root != null)
                {
                    var capturemeta = root.Element("CaptureMetaAll");
                    switch (typeinfo.SignalType)
                    {
                        case 0:
                            {
                                var node = capturemeta.Element("SDCaptureMeta");
                                if (node != null)
                                {
                                    capturenode = node.FirstNode as XElement;
                                }
                            }
                            break;
                        case 1:
                            {
                                var node = capturemeta.Element("HDCaptureMeta");
                                if (node != null)
                                {
                                    capturenode = node.FirstNode as XElement;
                                }
                            }
                            break;
                        case 2:
                            {
                                var node = capturemeta.Element("UHDCaptureMeta");
                                if (node != null)
                                {
                                    capturenode = node.FirstNode as XElement;
                                }
                            }
                            break;
                        default:
                            break;
                    }

                    if (capturenode != null)
                    {
                        CTimeCode timecode = new CTimeCode();
                        timecode.setDBFrameRate(typeinfo.fFrameRate);
                        timecode.SetDFMode(typeinfo.TCMode == TimeCodeMode.DF ? 1 : 0);
                        timecode.SetVS(timecode.Rate2VideoStandard(typeinfo.fFrameRate));

                        long bmpframe = timecode.GetFrameByTimeCode(taskinfo.ContentMeta.PresetStamp);

                        Logger.Info($"GetCaptureParm {bmpframe}");
                        var pic = capturenode.Element("FirPicNum");
                        if (pic != null)
                        {
                            pic.Value = bmpframe.ToString();
                        }
                        else
                        {
                            capturenode.Add(new XElement("FirPicNum", bmpframe.ToString()));
                        }

                        //第三码路径没说过要用，暂时算了
                        var pathfile = capturenode.Element("path1FileName");
                        if (pathfile != null)
                        {
                            pathfile.Value = FormatCaptureParamPath(pathfile.Value);
                        }
                        pathfile = capturenode.Element("path0FileName");
                        if (pathfile != null)
                        {
                            pathfile.Value = FormatCaptureParamPath(pathfile.Value);
                        }

                        //if (m_bReplaceHighCaptureParams && (nTaskType == 7))
                        //{
                        //    XmlNode bPath0 = CAPTUREPARAM.SelectSingleNode("bPath0");
                        //    if (bPath0 != null)
                        //    {
                        //        bPath0.InnerText = "0";
                        //    }
                        //}

                        if (taskinfo.ContentMeta.AudioChannels >= 0)
                        {
                            pic = capturenode.Element("iPureAudioValue");
                            if (pic != null)
                            {
                                pic.Value = taskinfo.ContentMeta.AudioChannels.ToString();
                            }
                        }
                        if (taskinfo.ContentMeta.AudioChannelAttribute > 0)
                        {
                            pic = capturenode.Element("nAudioChannelAttribute");
                            if (pic != null)
                            {
                                pic.Value = taskinfo.ContentMeta.AudioChannelAttribute.ToString();
                            }
                        }

                        if (taskinfo.ContentMeta.ASRmask >= 0)
                        {
                            pic = capturenode.Element("ASR_mask");
                            if (pic != null)
                            {
                                pic.Value = taskinfo.ContentMeta.ASRmask.ToString();
                            }
                        }

                        string backinfo = capturenode.ToString();
                        //backinfo = backinfo.Replace("&amp;", "&");
                        return backinfo;
                    }
                    else
                        Logger.Error($"load captureparam error {captureparam} {typeinfo.SignalType}");
                }

            }
            else
                Logger.Error($"StartTaskAsync QuerySDIFormatAsync error {typeinfo?.SignalType}");
            return string.Empty;
        }

        
    }
}
