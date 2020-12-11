using IngestTask.Tool;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IngestTask.Tools
{
    public static class AutoRetry
    {
        //internal static ILogger Logger = LoggerManager.GetLogger("AutoRetry");

        public async static Task<TResponse> RunAsync<TResponse>(Func<Task<TResponse>> proc, int retryCount = 3, int delay = 1000)
            where TResponse : ResponseMessage
        {
            TResponse r = null;
            for (int i = 1; i <= retryCount; i++)
            {
                bool isOk = true;
                try
                {
                    r = await proc().ConfigureAwait(true);
                    isOk = r.Code == ResponseCodeDefines.SuccessCode;
                }
                catch (System.Exception )
                {
                    //Logger.Error("execute proc error....{0}\r\n{1}", r == null ? "" : (r.Message ?? ""), e.ToString());
                    isOk = false;
                    if (i == retryCount)
                    {
                        throw;
                    }
                }
                if (!isOk)
                {
                    if (delay > 0)
                    {
                        await Task.Delay((int)delay).ConfigureAwait(true);
                    }
                }
                else
                {
                    break;
                }
            }
            return r;
        }

        public async static Task<TResponse> RunAsync<TResponse>(Func<string,Task<TResponse>> proc,string param, int retryCount = 3, int delay = 1000)
            where TResponse : ResponseMessage
        {
            TResponse r = null;
            for (int i = 1; i <= retryCount; i++)
            {
                bool isOk = true;
                try
                {
                    r = await proc(param).ConfigureAwait(true);
                    isOk = r.Code == ResponseCodeDefines.SuccessCode;
                }
                catch (System.Exception )
                {
                    //Logger.Error("execute proc error....{0}\r\n{1}", r == null ? "" : (r.Message ?? ""), e.ToString());
                    isOk = false;
                    if (i == retryCount)
                    {
                        throw;
                    }
                }
                if (!isOk)
                {
                    if (delay > 0)
                    {
                        await Task.Delay((int)delay).ConfigureAwait(true);
                    }
                }
                else
                {
                    break;
                }
            }
            return r;
        }

        public async static Task<bool> BoolRunAsync(Func<Task<bool>> proc, int retryCount = 5, int delay = 5000)
        {
            bool r = false;
            for (int i = 1; i <= retryCount; i++)
            {
                bool isOk = true;
                try
                {
                    r = await proc().ConfigureAwait(true);
                    isOk = r;
                }
                catch (System.Exception )
                {
                    //Logger.Error("execute proc error....{0}\r\n{1}", r, e.ToString());
                    isOk = false;
                    if (i == retryCount)
                    {
                        throw;
                    }
                }
                if (!isOk)
                {
                    if (delay > 0)
                    {
                        await Task.Delay((int)delay).ConfigureAwait(true);
                    }
                }
                else
                {
                    break;
                }
            }
            return r;
        }


        public async static Task RunAsync(Func<Task> proc, int retryCount = 5, int delay = 5000)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    await proc().ConfigureAwait(true);
                    break;
                }
                catch (System.Exception )
                {
                    //Logger.Error("execute proc error....\r\n{0}", e.ToString());
                    if (i == retryCount)
                    {
                        throw;
                    }
                }

            }
        }

        public static void RunSync(Action proc, int retryCount = 3, int delay = 1000, bool throwError = true)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    proc();
                    break;
                }
                catch (System.Exception )
                {
                    //Logger.Error("execute proc error....\r\n{0}", e.ToString());
                    if (i == retryCount)
                    {
                        if (throwError)
                            throw;
                        else
                            break;
                    }
                }

            }
        }

        public static async Task<TResult> RunSyncAsync<TResult>(Func<Task<TResult>> proc, Func<TResult,bool> judge, int retryCount = 3, int delay = 1000, bool throwError = true)
        {
            TResult r = default(TResult);
            for (int i = 0; i < retryCount; i++)
            {
                bool isOk = true;
                try
                {
                    r = await proc().ConfigureAwait(true);
                    isOk = judge(r);
                    break;
                }
                catch (System.Exception)
                {
                    isOk = false;
                    //Logger.Error("execute proc error....\r\n{0}", e.ToString());
                    if (i == retryCount)
                    {
                        if (throwError)
                            throw;
                        else
                            break;
                    }
                }

                if (!isOk)
                {
                    if (delay > 0)
                    {
                        await Task.Delay((int)delay).ConfigureAwait(true);
                    }
                }
                else
                {
                    break;
                }
            }
            return r;
        }

        public static TResult RunSync<TResult>(Func<TResult> proc, int retryCount = 3, int delay = 1000, bool throwError = true)
        {
            TResult r = default(TResult);
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    r = proc();
                    break;
                }
                catch (System.Exception )
                {
                    //Logger.Error("execute proc error....\r\n{0}", e.ToString());
                    if (i == retryCount)
                    {
                        if (throwError)
                            throw;
                        else
                            break;
                    }
                }
            }
            return r;
        }


        public static async Task<TResult> RunAsync<TParam,TResult>(Func<TParam, Task<TResult>> proc, TParam param, int retryCount = 3, int delay = 1000)
        {
            TResult r = default(TResult);
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    r = await proc(param).ConfigureAwait(true);
                }
                catch (System.Exception )
                {
                    if (i == retryCount)
                    {
                        throw ;
                    }
                }
                if (r == null)
                {
                    if (delay > 0)
                    {
                        await Task.Delay((int)delay).ConfigureAwait(true);
                    }
                }
                else
                {
                    break;
                }
            }
            return r;
        }


    }
}
