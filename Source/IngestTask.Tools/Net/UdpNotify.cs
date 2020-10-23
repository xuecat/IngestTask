using IngestTask.Tool;
using Sobey.Core.Log;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Tools.Net
{
    public class UdpNotify : IDisposable
    {
        private readonly ILogger Logger = LoggerManager.GetLogger("UdpNotify");
        private readonly RestClient _restClient ;
        private readonly SocketAsyncEventArgs _soketArgs;
        private  bool _disposed;
        public UdpNotify (RestClient rest)
        {
            _disposed = false;
            _restClient = rest;
            _soketArgs = new SocketAsyncEventArgs();
            //_soketArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
        }
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        { }

        public async Task SendMsgToClientAsync(int level, string info)
        {
            if (_restClient != null)
            {
                var lstlogininfo = await _restClient.GetAllUserLoginInfosAsync().ConfigureAwait(true);

                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                Byte[] bySend = Encoding.Unicode.GetBytes(GetErrorMsg(level, info));

                foreach (var item in lstlogininfo)
                {
                    

                    _soketArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(item.Ip), item.Port);
                    bySend.CopyTo(_soketArgs.Buffer, 0);
                    _soketArgs.SetBuffer(0, bySend.Length);
                    
                    if (!sock.SendToAsync(_soketArgs))
                    {
                        Logger.Info($"udpnotify error {item.Ip}");
                    }
                }

                sock.Close();
                sock = null;
            }
        }

        public string GetErrorMsg(int level, string info)
        {
            string msg = string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><ErrorInfoNotify><ErrorLevel>{0:d}</ErrorLevel><ErrorString>{1}</ErrorString><ErroSource>5</ErroSource></ErrorInfoNotify>"
                , level, info);

            return msg;
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UdpNotify()
        {
            //必须为false
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return; 
            if (disposing)
            {
                
            }
            if (_soketArgs != null)
            {
                _soketArgs.Dispose();
            }
            _disposed = true;
        }
    }
}
