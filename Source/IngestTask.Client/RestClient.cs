﻿using IngestTask.Dto;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Client
{
    public class RestClient : IDisposable
    {
        
        private HttpClient _httpClient = null;
        //const string TASKAPI20 = "api/v2/task";
        //const string TASKAPI21 = "api/v2.1/task";
        const string TASKAPI30 = "api/v3/task";
        //const string MATRIXAPI20 = "api/v2/matrix";
        //const string USERAPI20 = "api/v2/user";
        //const string DEVICEAPI21 = "api/v2.1/device";
        //const string DEVICEAPI20 = "api/v2/device";

        private string _ingestDbUrl;
        private string _cmServerUrl;
        private bool _disposed;
        public RestClient(string dburl, string cmurl)
        {
            _disposed = false;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Connection.Clear();
            _httpClient.DefaultRequestHeaders.ConnectionClose = false;
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("sobeyhive-http-system", "INGESTSERVER");
            _httpClient.DefaultRequestHeaders.Add("sobeyhive-http-site", "S1");
            _httpClient.DefaultRequestHeaders.Add("sobeyhive-http-tool", "INGESTSERVER");
            _ingestDbUrl = dburl;
            _cmServerUrl = cmurl;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RestClient()
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
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
            _disposed = true;
        }

        public static Dictionary<string, string> GetTokenHeader(string usertoken)
        {
            return new Dictionary<string, string>() { 
                {"sobeyhive-http-token", usertoken }
            };
        }

        
        public static Dictionary<string, string> GetIngestHeader()
        {
            return new Dictionary<string, string>() {
                {"sobeyhive-ingest-signature", Base64SQL.ToBase64String($"ingest_server;{DateTime.Now}")},
            };
        }

        public async Task<TResponse> PostAsync<TResponse>(string url, object body, string method = "POST", NameValueCollection queryString = null)
            where TResponse : class, new()
        {
            TResponse response = null;
            try
            {
                string json = JsonHelper.ToJson(body);
                HttpClient client = _httpClient;
                if (queryString == null)
                {
                    queryString = new NameValueCollection();
                }
                if (String.IsNullOrEmpty(method))
                {
                    method = "POST";
                }
                url = CreateUrl(url, queryString);
                //Logger.Debug("请求：{0} {1}", method, url);
                byte[] strData = Encoding.UTF8.GetBytes(json);
                MemoryStream ms = new MemoryStream(strData);
                using (StreamContent sc = new StreamContent(ms))
                {
                    sc.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

                    //foreach (var item in _httpClient.DefaultRequestHeaders)
                    //{
                    //    Logger.Error("header :  " + item.Key + ":" + item.Value.FirstOrDefault());
                    //    foreach(var test in item.Value)
                    //    {
                    //        Logger.Error("test :  " + test);
                    //    }
                    //}

                    var res = await client.PostAsync(url, sc).ConfigureAwait(true);
                    byte[] rData = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
                    string rJson = Encoding.UTF8.GetString(rData);
                    response = JsonHelper.ToObject<TResponse>(rJson);
                    return response;
                }
                   
            }
            catch (System.Exception )
            {
                TResponse r = new TResponse();
                throw;
            }
        }

        public async Task<string> PostAsync(string url, string body, string method = "POST", NameValueCollection queryString = null)
        {
            string response = null;
            try
            {
                string json = body;
                HttpClient client = _httpClient;
                if (queryString == null)
                {
                    queryString = new NameValueCollection();
                }
                if (String.IsNullOrEmpty(method))
                {
                    method = "POST";
                }
                url = CreateUrl(url, queryString);
                //Logger.Debug("请求：{0} {1}", method, url);
                byte[] strData = Encoding.UTF8.GetBytes(json);
                MemoryStream ms = new MemoryStream(strData);
                using (StreamContent sc = new StreamContent(ms))
                {
                    sc.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    var res = await client.PostAsync(url, sc).ConfigureAwait(true);
                    if (res.Content == null || res.Content.Headers.ContentLength == 0)
                    {
                        response = "";
                    }
                    else
                    {
                        byte[] rData = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
                        string rJson = Encoding.UTF8.GetString(rData);
                        //Logger.Debug("应答：\r\n{0}", rJson);
                        response = rJson;
                    }
                }
                    
            }
            catch (System.Exception )
            {
                response = "ERROR";
            }
            return response;
        }

        public async Task<TResponse> PutAsync<TResponse>(string url, object body, Dictionary<string, string> header, NameValueCollection queryString = null)
        {
            TResponse response = default(TResponse);
            try
            {
                string json = JsonHelper.ToJson(body);
                HttpClient client = _httpClient;
                if (queryString == null)
                {
                    queryString = new NameValueCollection();
                }

                url = CreateUrl(url, queryString);
                //Logger.Debug("请求：{0} {1}", method, url);
                byte[] strData = Encoding.UTF8.GetBytes(json);
                MemoryStream ms = new MemoryStream(strData);
                using (StreamContent sc = new StreamContent(ms))
                {
                    sc.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    if (header != null)
                    {
                        foreach (var item in header)
                        {
                            sc.Headers.Add(item.Key, item.Value);
                        }
                    }

                    var res = await client.PutAsync(url, sc).ConfigureAwait(true);
                    byte[] rData = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
                    string rJson = Encoding.UTF8.GetString(rData);
                    //Logger.Debug("应答：\r\n{0}", rJson);
                    response = JsonHelper.ToObject<TResponse>(rJson);
                    return response;
                }

            }
            catch (System.Exception )
            {
                throw;
            }
        }

        public async Task<TResponse> DeleteAsync<TResponse>(string url, Dictionary<string, string> header, NameValueCollection queryString = null)
            where TResponse : class, new()
        {
            TResponse response = default(TResponse);
            try
            {
                HttpClient client = _httpClient;
                if (queryString == null)
                {
                    queryString = new NameValueCollection();
                }

                url = CreateUrl(url, queryString);
                //Logger.Debug("请求：{0} {1}", method, url);

                using (var requestMessage = new HttpRequestMessage(HttpMethod.Delete, url))
                {
                    if (header != null)
                    {
                        foreach (var item in header)
                        {
                            requestMessage.Headers.Add(item.Key, item.Value);
                        }
                    }
                    var backinfo = await client.SendAsync(requestMessage).ConfigureAwait(true);
                    var rJson = await backinfo.Content.ReadAsStringAsync().ConfigureAwait(true);
                    response = JsonHelper.ToObject<TResponse>(rJson);
                }

            }
            catch (System.Exception )
            {
                TResponse r = new TResponse();
                return r;
            }
            return response;
        }

        public async Task<TResponse> GetAsync<TResponse>(string url, NameValueCollection queryString, Dictionary<string, string> header)
                    where TResponse : class, new()
        {
            TResponse response = null;
            try
            {
                HttpClient client = _httpClient;
                if (queryString != null)
                {
                    url = CreateUrl(url, queryString);
                }
                
                //Logger.Debug("请求：{0} {1}", "GET", url);
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (header != null)
                    {
                        foreach (var item in header)
                        {
                            requestMessage.Headers.Add(item.Key, item.Value);
                        }
                    }
                    var backinfo = await client.SendAsync(requestMessage).ConfigureAwait(true);
                    var rJson = await backinfo.Content.ReadAsStringAsync().ConfigureAwait(true);
                    response = JsonHelper.ToObject<TResponse>(rJson);
                }
                
            }
            catch (System.Exception )
            {
                TResponse r = new TResponse();
                return r;
            }
            return response;
        }

        //public async Task<TResponse> GetAsync<TResponse>(string url, NameValueCollection queryString)
        //            where TResponse : class, new()
        //{
        //    TResponse response = null;
        //    try
        //    {
        //        HttpClient client = _httpClient;
        //        if (queryString != null)
        //        {
        //            url = CreateUrl(url, queryString);
        //        }
        //        
        //        //Logger.Debug("请求：{0} {1}", "GET", url);
        //        byte[] rData = await client.GetByteArrayAsync(url).ConfigureAwait(true);
        //        string rJson = Encoding.UTF8.GetString(rData);
        //        Logger.Info("url response：\r\n{0} {1}", url, rJson);
        //        response = JsonHelper.ToObject<TResponse>(rJson);
        //    }
        //    catch (System.Exception )
        //    {
        //        TResponse r = new TResponse();
        //        //Logger.Error("请求异常：\r\n{0}", e.ToString());
        //        return r;
        //    }
        //    return response;
        //}
        public async Task<TResponse> PostAsync<TResponse>(string url, object body, Dictionary<string, string> header, string method = null, NameValueCollection queryString = null)
        {
            TResponse response = default(TResponse);
            try
            {
                string json = JsonHelper.ToJson(body);
                HttpClient client = _httpClient;
                if (queryString == null)
                {
                    queryString = new NameValueCollection();
                }

                url = CreateUrl(url, queryString);
                if (String.IsNullOrEmpty(method))
                {
                    method = "POST";
                }
                //Logger.Debug("请求：{0} {1}", method, url);
                byte[] strData = Encoding.UTF8.GetBytes(json);
                MemoryStream ms = new MemoryStream(strData);
                using (StreamContent sc = new StreamContent(ms))
                {
                    sc.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    if (header != null)
                    {
                        foreach (var item in header)
                        {
                            sc.Headers.Add(item.Key, item.Value);
                        }
                    }
                    
                    var res = await client.PostAsync(url, sc).ConfigureAwait(true);
                    byte[] rData = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
                    string rJson = Encoding.UTF8.GetString(rData);
                    //Logger.Debug("应答：\r\n{0}", rJson);
                    response = JsonHelper.ToObject<TResponse>(rJson);
                    return response;
                }
                    
            }
            catch (System.Exception )
            {
                //Logger.Error("请求异常：\r\n{0}", e.ToString());
                throw;
            }
        }

        //public async Task<string> PostAsync(string url, object body, string method, NameValueCollection queryString)
        //{
        //    string response = null;
        //    try
        //    {
        //        string json = JsonHelper.ToJson(body);
        //        HttpClient client = _httpClient;
        //        if (queryString == null)
        //        {
        //            queryString = new NameValueCollection();
        //        }

        //        url = CreateUrl(url, queryString);
        //        if (String.IsNullOrEmpty(method))
        //        {
        //            method = "POST";
        //        }
        //        //Logger.Debug("请求：{0} {1}", method, url);
        //        byte[] strData = Encoding.UTF8.GetBytes(json);
        //        MemoryStream ms = new MemoryStream(strData);
        //        using (StreamContent sc = new StreamContent(ms))
        //        {
        //            sc.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
        //            var res = await client.PostAsync(url, sc).ConfigureAwait(true);
        //            byte[] rData = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
        //            string rJson = Encoding.UTF8.GetString(rData);
        //            //Logger.Debug("应答：\r\n{0}", rJson);
        //            response = rJson;
        //            return response;
        //        }
                    
        //    }
        //    catch (System.Exception )
        //    {
        //        //Logger.Error("请求异常：\r\n{0}", e.ToString());
        //        throw;
        //    }
        //}

        public async Task<TResult> PostWithTokenAsync<TResult>(string url, object body, string token, string userId = null, string method = "Post")
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            string apiUrl = $"{url}";
            HttpMethod hm = new HttpMethod(method);

            using (var request = new HttpRequestMessage(hm, apiUrl))
            {
                if (!String.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                }
                if (!String.IsNullOrEmpty(userId))
                {
                    request.Headers.Add("User", userId);
                }

                string json = "";
                if (body != null)
                {
                    json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
                }
                request.Content = new StringContent(json);
                request.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(true);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("验证失败");
                }
                try
                {
                    response.EnsureSuccessStatusCode();
                    string str = await response.Content.ReadAsStringAsync().ConfigureAwait(true);

                    //sw.Stop();
                    //if (sw.ElapsedMilliseconds >= 1000)
                    //{
                    //slowLogger.Warn("请求时间超过一秒：POST {0} {1}", apiUrl, sw.ElapsedMilliseconds);
                    //}

                    return Newtonsoft.Json.JsonConvert.DeserializeObject<TResult>(str);
                }
                catch (Exception )
                {
                    //logger.Error("Post 失败：{0}\r\n{1}", url, e.ToString());
                    string str = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                    //logger.Error(str);
                    throw;
                }
            }
                
        }

        public async Task<TResult> SubmitFormAsync<TResult>(string url, Dictionary<string, string> formData, string method = "Post")
        {
            HttpMethod hm = new HttpMethod(method);
            using (var request = new HttpRequestMessage(hm, url))
            {
                request.Content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(true);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException("验证失败");
                }
                response.EnsureSuccessStatusCode();
                string str = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TResult>(str);
            }
                
        }


        public static string CreateUrl(string url, NameValueCollection qs)
        {
            if (qs != null && qs.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                List<string> kl = qs.AllKeys.ToList();
                foreach (string k in kl)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("&");
                    }
                    sb.Append(k).Append("=");
                    if (!String.IsNullOrEmpty(qs[k]))
                    {

                        sb.Append(System.Net.WebUtility.UrlEncode(qs[k]));
                    }
                }

                if (url != null)
                {
                    if (url.Contains("?"))
                    {
                        url = url + "&" + sb.ToString();
                    }
                    else
                    {
                        url = url + "?" + sb.ToString();
                    }
                }
                
            }

            return url;

        }

        #region Global
        //public async Task<List<UserLoginInfo>> GetAllUserLoginInfosAsync()
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<List<UserLoginInfo>>>(() =>
        //    {
        //        return GetAsync<ResponseMessage<List<UserLoginInfo>>>(
        //            $"{_ingestDbUrl}/{USERAPI20}/userlogininfo/all", null, GetIngestHeader()
        //            );
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return null;
        //}
        #endregion

        #region Task

        public async Task<List<TaskContent>> GetNeedSyncTaskListAsync()
        {
            var back = await GetAsync<ResponseMessage<List<TaskContent>>>(
                    //$"{_ingestDbUrl}/{TASKAPI20}/needsync"
                    $"{_ingestDbUrl}/{TASKAPI30}/sync/ready"
                    , null, GetIngestHeader()
                    ).ConfigureAwait(true);

            if (back != null)
            {
                return back.Ext;
            }
            return null;
        }
        public async Task<DispatchTask> GetTaskDBAsync(int taskid)
        {
            var back = await GetAsync<ResponseMessage<DispatchTask>>(
                    $"{_ingestDbUrl}/{TASKAPI30}/db/{taskid}", null, GetIngestHeader()
                    ).ConfigureAwait(true);

            if (back != null)
            {
                return back.Ext;
            }
            return null;
        }

        //public async Task<TaskContent> GetChannelCapturingTaskInfoAsync(int channelid)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<TaskContent>>(() => {
        //        NameValueCollection v = new NameValueCollection();
        //        v.Add("newest", "1");

        //        return GetAsync<ResponseMessage<TaskContent>>(
        //            $"{_ingestDbUrl}/{TASKAPI20}/capturing/{channelid}", v, GetIngestHeader()
        //            );
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return null;
        //}

        //public async Task<TaskSource> GetTaskSourceByTaskIdAsync(int taskid)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<TaskSource>>(() =>
        //    {
        //        return GetAsync<ResponseMessage<TaskSource>>(
        //            $"{_ingestDbUrl}/{TASKAPI20}/tasksource/{taskid}",
        //            null, GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return TaskSource.emUnknowTask;
        //}

        //public async Task<TaskFullInfo> GetTaskFullInfoAsync(int taskid)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<TaskFullInfo>>(() =>
        //    {
        //        return GetAsync<ResponseMessage<TaskFullInfo>>(
        //            $"{_ingestDbUrl}/{TASKAPI21}/taskinfo/{taskid}/full",
        //            null, GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return null;
        //}

        //public async Task<bool> CompleteSynTasksAsync(int taskid, taskState tkstate, dispatchState dpstate, syncState systate)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage>(() =>
        //    {
        //        CompleteSyncTask task = new CompleteSyncTask()
        //        {
        //            DispatchState = (int)dpstate,
        //            IsFinish = false,
        //            Perodic2Next = false,
        //            SynState = (int)systate,
        //            TaskID = taskid,
        //            TaskState = (int)tkstate
        //        };

        //        return PutAsync<ResponseMessage>(
        //            $"{_ingestDbUrl}/{TASKAPI20}/completesync",
        //            task, GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null && back.IsSuccess())
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        //public async Task<int> DeleteTaskAsync(int taskid)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage>(() =>
        //    {
        //        return DeleteAsync<ResponseMessage>(
        //            $"{_ingestDbUrl}/{TASKAPI20}/delete/{taskid}",
        //            GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null && back.IsSuccess())
        //    {
        //        return taskid;
        //    }
        //    return 0;
        //}

        //public async Task<int> SetTaskStateAsync(int taskid, taskState tkstate)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<int>>(() =>
        //    {
        //        NameValueCollection v = new NameValueCollection();
        //        v.Add("state", ((int)tkstate).ToString());

        //        return PutAsync<ResponseMessage<int>>(
        //            $"{_ingestDbUrl}/{TASKAPI20}/state/{taskid}",null,
        //            GetIngestHeader(), v);
        //    }).ConfigureAwait(true);

        //    if (back != null && back.IsSuccess())
        //    {
        //        return taskid;
        //    }
        //    return 0;
        //}

        //public async Task<TaskContent> CreatePeriodcTaskAsync(int taskid)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<TaskContent>>(() =>
        //    {

        //        return PostAsync<ResponseMessage<TaskContent>>(
        //            $"{_ingestDbUrl}/{TASKAPI20}/periodic/createtask/{taskid}", null,
        //            GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null && back.IsSuccess())
        //    {
        //        return back.Ext;
        //    }
        //    return null;
        //}

        //public async Task<int> AddReScheduleTaskAsync(int oldtaskid)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<TaskContent>>(() =>
        //    {

        //        return PostAsync<ResponseMessage<TaskContent>>(
        //            $"{_ingestDbUrl}/{TASKAPI21}/schedule/{oldtaskid}", null,
        //            GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null && back.IsSuccess())
        //    {
        //        return back.Ext.TaskId;
        //    }
        //    return 0;
        //}
        #endregion

        #region Matrix
        //public async Task<bool> SwitchMatrixAsync(int inport, int outport)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<bool>>(() =>
        //    {
        //        var query = new NameValueCollection();
        //        query.Add("inport", inport.ToString());
        //        query.Add("outport", outport.ToString());
        //        return GetAsync<ResponseMessage<bool>>(
        //            $"{_ingestDbUrl}/{MATRIXAPI20}/switch/",
        //            query,
        //            GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null && back.IsSuccess())
        //    {
        //        return back.Ext;
        //    }
        //    return false;
        //}

        //public async Task<bool> SwitchMatrixSignalChannelAsync(int signalid, int channelid)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<bool>>(() =>
        //    {
        //        var query = new NameValueCollection();
        //        query.Add("signal", signalid.ToString());
        //        query.Add("channel", channelid.ToString());
        //        return GetAsync<ResponseMessage<bool>>(
        //            $"{_ingestDbUrl}/{MATRIXAPI20}/switchsignalchannel/",
        //            query,
        //            GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null && back.IsSuccess())
        //    {
        //        return back.Ext;
        //    }
        //    return false;
        //}

        //public async Task<bool> SwitchMatrixChannelRtmpAsync(int channelid, string url)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<bool>>(() =>
        //    {
        //        var query = new NameValueCollection();

        //        query.Add("channelid", channelid.ToString());
        //        query.Add("url", url);
        //        return GetAsync<ResponseMessage<bool>>(
        //            $"{_ingestDbUrl}/{MATRIXAPI20}/switchchannelrtmpurl/",
        //            query,
        //            GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null && back.IsSuccess())
        //    {
        //        return back.Ext;
        //    }
        //    return false;
        //}

        //public async Task<bool> SwitchMatrixRtmpAsync(int outport, string url)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<bool>>(() =>
        //    {
        //        var query = new NameValueCollection();

        //        query.Add("outport", outport.ToString());
        //        query.Add("url", url);
        //        return GetAsync<ResponseMessage<bool>>(
        //            $"{_ingestDbUrl}/{MATRIXAPI20}/switchrtmpurl/",
        //            query,
        //            GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null && back.IsSuccess())
        //    {
        //        return back.Ext;
        //    }
        //    return false;
        //}
        //#endregion

        //#region Device

        //public async Task<List<CaptureChannelInfo>> GetAllCaptureChannelAsync()
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<List<CaptureChannelInfo>>>(() =>
        //    {
        //        return GetAsync<ResponseMessage<List<CaptureChannelInfo>>>(
        //            $"{_ingestDbUrl}/{DEVICEAPI20}/capturechannel/all", null, GetIngestHeader()
        //            );
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return null;
        //}

        //public async Task<List<SignalSrc>> GetAllSignalSrcAsync()
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<List<SignalSrc>>>(() =>
        //    {
        //        return GetAsync<ResponseMessage<List<SignalSrc>>>(
        //            $"{_ingestDbUrl}/{DEVICEAPI20}/signalsrc/all", null, GetIngestHeader()
        //            );
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return null;
        //}

        //public async Task<List<MsvChannelState>> GetAllChannelStateAsync()
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<List<MsvChannelState>>>(() =>
        //    {
        //        return GetAsync<ResponseMessage<List<MsvChannelState>>>(
        //            $"{_ingestDbUrl}/{DEVICEAPI20}/channelstate/all", null, GetIngestHeader()
        //            );
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return null;
        //}

        //public async Task<List<ProgrammeInfo>> GetAllProgrammeAsync()
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<List<ProgrammeInfo>>>(() =>
        //    {
        //        return GetAsync<ResponseMessage<List<ProgrammeInfo>>>(
        //            $"{_ingestDbUrl}/{DEVICEAPI20}/programme/all", null, GetIngestHeader()
        //            );
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return null;
        //}
        //public async Task<bool> UpdateMSVChannelStateAsync(int id, MSV_Mode mode, Device_State state)
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<bool>>(() =>
        //    {
        //        return PostAsync<ResponseMessage<bool>>(
        //            $"{_ingestDbUrl}/{DEVICEAPI20}/channelstate/{id}", 
        //            new { DevState = state, MSVMode = mode }, GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return false;
        //}

        //public async Task<List<DeviceInfo>> GetAllDeviceInfoAsync()
        //{
        //    var back = await AutoRetry.RunAsync<ResponseMessage<List<DeviceInfo>>>(() =>
        //    {
        //        return GetAsync<ResponseMessage<List<DeviceInfo>>>(
        //            $"{_ingestDbUrl}/{DEVICEAPI21}/allocdevice", null, GetIngestHeader());
        //    }).ConfigureAwait(true);

        //    if (back != null)
        //    {
        //        return back.Ext;
        //    }
        //    return null;
        //}


        #endregion

    }
}
