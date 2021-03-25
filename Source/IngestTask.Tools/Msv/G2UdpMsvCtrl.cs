using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Sobey.Core.Log;
using System.Reflection;

namespace IngestTask.Tool.Msv
{ 
    public class G2UdpMsvCtrl
    {
        public G2UdpMsvCtrl()
        {
        }
    }

    public partial class CClientTaskSDKImp
    {
        public async Task<string> SendMsvCommandAsync(
            ILogger logger, IPAddress ipaddress, string strmsvip,
            int nmsvchport, string strcmd)
        {
            string strret = "";

            if (logger == null || ipaddress.AddressFamily != AddressFamily.InterNetwork)
            {
                return strret;
            }

            string strtmplog = string.Format("{0}:{1}", strmsvip, nmsvchport);
            //logger.Info(strtmplog + ", strcmd:" + strcmd);

            using (UdpClient udpClient = new UdpClient(0))
            {
                try
                {
                    IPEndPoint remoteIpep = new IPEndPoint(ipaddress, nmsvchport); // 发送到的IP地址和端口号
                    string strContent = strcmd;//string.Format("<?xml version=\"1.0\"?><query_state><nChannel>{0}</nChannel></query_state>\0", 3100);
                    byte[] bytes = System.Text.Encoding.Unicode.GetBytes(strContent);
                    int u = bytes.Length + 8;
                    byte[] byteL = new byte[u];
                    byteL[3] = (byte)(u);
                    byteL[2] = (byte)(u >> 8);
                    byteL[1] = (byte)(u >> 16);
                    byteL[0] = (byte)(u >> 24);
                    bytes.CopyTo(byteL, 4);
                    udpClient.Client.Blocking = false;
                    await udpClient.SendAsync(byteL, byteL.Length, remoteIpep).ConfigureAwait(true);

                    int buffSizeCurrent = udpClient.Available;//取得缓冲区当前的数据的个数   

                    //logger.Info($"SendAsync one times {udpClient.Available}");

                    int ntimes = 4;
                    uint nNeedRecvLen = 0;
                    uint nRecvedLen = (uint)0;
                    byte[] bytedata = new byte[2048];
                    while (true)
                    {
                        if (buffSizeCurrent <= 1)
                        {
                            ntimes--;
                        }
                        else
                            ntimes = 3;

                        if (ntimes <= 0)//连续四次recevice还出错说明有问题网络
                        {
                            logger.Error($"SendMsvCommandAsync over times {ntimes}");
                            break;
                        }

                        if (buffSizeCurrent == 1)
                        {
                            await udpClient.SendAsync(byteL, byteL.Length, remoteIpep).ConfigureAwait(true);
                            buffSizeCurrent = udpClient.Available;//取得缓冲区当前的数据的个数  
                            logger.Info($"SendAsync two times {udpClient.Available}");

                            continue;
                        }

                        if (buffSizeCurrent > 1)     //有数据时候才读，不然会出异常哦
                        {
                            //byte[] data = await udpClient.ReceiveAsync(ref remoteIpep).ConfigureAwait(true);  
                            var backinfo = await udpClient.ReceiveAsync().ConfigureAwait(true);
                            if (nNeedRecvLen == 0)
                            {
                                backinfo.Buffer.CopyTo(bytedata, nRecvedLen);
                                nRecvedLen += (uint)backinfo.Buffer.Length;
                                if (nRecvedLen < 4)
                                {
                                    buffSizeCurrent = udpClient.Available;//取得缓冲区当前的数据的个数   
                                    continue;
                                }
                                else
                                {
                                    uint nlen = (uint)(bytedata[3] | bytedata[2] << 8 | bytedata[1] << 16 | bytedata[0] << 24);
                                    if (nlen > 2048)//无效值
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        nNeedRecvLen = nlen;
                                        if (nRecvedLen >= nNeedRecvLen)
                                        {
                                            break;
                                        }
                                    }

                                }
                            }
                            else
                            {
                                backinfo.Buffer.CopyTo(bytedata, nRecvedLen);
                                nRecvedLen += (uint)backinfo.Buffer.Length;
                                if (nRecvedLen >= nNeedRecvLen)
                                {
                                    break;
                                }
                            }
                        }

                        buffSizeCurrent = udpClient.Available;//取得缓冲区当前的数据的个数  

                        await Task.Delay(100).ConfigureAwait(true);
                        //System.Threading.Thread.Sleep(100);
                    }

                    if (nRecvedLen > 0)
                    {
                        byte[] datatmp = new byte[nRecvedLen - 4 - 1];
                        for (int j = 4; j < nRecvedLen - 1; j++)
                        {
                            datatmp[j - 4] = bytedata[j];
                        }

                        //bytedata[nRecvedLen - 1] = Convert.ToByte('\0');
                        strret = Encoding.Unicode.GetString(datatmp).TrimEnd('\0');

                        //logger.Info($"{strmsvip}:{nmsvchport} len: {nRecvedLen} recv: {strret}");
                        datatmp = null;
                    }
                    else
                    {
                        logger.Error("Recv: len error");
                    }
                    udpClient.Close();
                    bytedata = null;
                }
                catch (System.ObjectDisposedException ex)
                {
                    if (udpClient != null)
                    {
                        udpClient.Close();
                    }
                    logger.Error(strtmplog + "GetMsvUdpData Error!errorinfo=" + ex.ToString());
                }
                catch (System.ArgumentException ex)
                {
                    if (udpClient != null)
                    {
                        udpClient.Close();
                    }
                    logger.Error(strtmplog + "GetMsvUdpData Error!errorinfo=" + ex.ToString());
                }
                catch (System.InvalidOperationException ex)
                {
                    if (udpClient != null)
                    {
                        udpClient.Close();
                    }
                    logger.Error(strtmplog + "GetMsvUdpData Error!errorinfo=" + ex.ToString());
                }
                catch (SocketException ex)
                {
                    if (udpClient != null)
                    {
                        udpClient.Close();
                    }
                    logger.Error(strtmplog + "GetMsvUdpData Error!errorinfo=" + ex.ToString());
                }
                catch (System.Exception ex)
                {
                    if (udpClient != null)
                    {
                        udpClient.Close();
                    }
                    logger.Error(strtmplog + "GetMsvUdpData Error!errorinfo=" + ex.ToString());

                }
            }


            return strret;
        }
    }
}
