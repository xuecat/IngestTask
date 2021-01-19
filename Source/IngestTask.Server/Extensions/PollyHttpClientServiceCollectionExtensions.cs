

namespace IngestTask.Server
{
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using Polly.Timeout;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// http请求熔断机制-实体
    /// </summary>
    public class HttpClientPollyOptions
    {
        /// <summary>
        /// 超时时间设置，单位为秒
        /// </summary>
        public int TimeoutTime { set; get; }
        /// <summary>
        /// 失败重试次数(RetryTimeoutArray,RetryCount二取一)
        /// </summary>
        public int RetryCount { set; get; }
        /// <summary>
        /// 重试超过次数执行方法
        /// </summary>
        public Action<object> RetryCountAction { set; get; }
        /// <summary>
        /// 失败重试等待时长数组 (RetryTimeoutArray,RetryCount二取一) 
        /// </summary>
        ///  ex:   new[]
        //        {
        //          TimeSpan.FromMilliseconds(100),
        //          TimeSpan.FromMilliseconds(200),
        //          TimeSpan.FromMilliseconds(300)
        //        }
#pragma warning disable CA1819 // 属性不应返回数组
        public TimeSpan[] RetryTimeoutArray { set; get; }
#pragma warning restore CA1819 // 属性不应返回数组
        /// <summary>
        /// 执行多少次异常，开启断路器（例：失败2次，开启断路器）
        /// </summary>
        public int CircuitBreakerOpenFallCount { set; get; }
        /// <summary>
        /// 断路器触发几次，执行操作（例：断路器触发2次，开启断路器触发终止程序）
        /// </summary>
        public static int CircuitBreakerOpenTriggerCount { set; get; }
        /// <summary>
        /// 断路生效次数超过执行方法
        /// </summary>
        public Action<object> CircuitBreakerAction { set; get; }

        /// <summary>
        /// 断路器开启的时间(例如：设置为2秒，短路器两秒后自动由开启到关闭)
        /// </summary>
        public int CircuitBreakerDownTime { set; get; } = 10;

        /// <summary>
        /// 降级处理(将异常消息封装成为正常消息返回，然后进行响应处理，例如：系统正在繁忙，请稍后处理.....)
        /// </summary>
        public HttpResponseMessage httpResponseMessage { set; get; }

        /// <summary>
        /// 委托Action实现方法
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="ac">执行方法体</param>
        /// <param name="inputParam">参数</param>
        public void ActionAchieve<T>(Action<T> ac, T inputParam)
        {
            if (ac != null)
            {
                ac(inputParam);
            }
            
        }

    }

    /// <summary>
    /// http请求熔断机制-工具
    /// </summary>
    public static class PollyHttpClientServiceCollectionExtensions
    {
        /// <summary>
        /// Httpclient扩展方法
        /// </summary>
        /// <param name="services">ioc容器</param>
        /// <param name="name">HttpClient 名称(针对不同的服务进行熔断，降级)</param>
        /// <param name="action">熔断降级配置</param>
        /// <param name="TResult">降级处理错误的结果</param>
        /// <returns></returns>
        public static IServiceCollection AddHttpClientPolly(this IServiceCollection services, string name, Action<HttpClientPollyOptions> action)
        {
            // 1、创建选项配置类
            HttpClientPollyOptions options = new HttpClientPollyOptions();
            if (action != null)
            {
                action(options);
            }
            
            // 2、配置httpClient,熔断降级策略
            IHttpClientBuilder httpClientBuilder = services.AddHttpClient(name);
            if (options.httpResponseMessage != null)
            {
                //1.1 降级策略
                _ = httpClientBuilder.AddPolicyHandler(Policy<HttpResponseMessage>.HandleInner<Exception>().FallbackAsync(options.httpResponseMessage, async b =>
                  {
                    // 1、降级打印异常
                    Console.WriteLine($"服务{name}开始降级,异常消息：{b.Exception?.Message}");
                    // 2、降级后的数据
                    Console.WriteLine($"服务{name}降级内容响应：{options.httpResponseMessage.Content?.ReadAsStringAsync().GetAwaiter()}");
                    await Task.CompletedTask.ConfigureAwait(false);
                  }));
            }
            if (options.CircuitBreakerOpenFallCount > 0)
            {
                // 1.2 断路器策略
                httpClientBuilder.AddPolicyHandler(Policy<HttpResponseMessage>.Handle<Exception>()
                    .CircuitBreakerAsync(options.CircuitBreakerOpenFallCount, TimeSpan.FromSeconds(options.CircuitBreakerDownTime), (ex, ts) =>
                {
                    Console.WriteLine($"服务{name}断路器开启，异常消息：{ex.Exception?.Message}");
                    Console.WriteLine($"服务{name}断路器开启时间：{ts.TotalSeconds}s");
                    if (options.CircuitBreakerAction != null)
                    {
                        options.ActionAchieve<object>(options.CircuitBreakerAction, ++HttpClientPollyOptions.CircuitBreakerOpenTriggerCount);
                    }
                }, () =>
                {
                    Console.WriteLine($"服务{name}断路器关闭");
                }, () =>
                {
                    Console.WriteLine($"服务{name}断路器半开启(时间控制，自动开关)");
                }));
            }
            if (options.RetryCount > 0)
            {
                // 1.3.1 重试策略 次数
                httpClientBuilder.AddPolicyHandler(Policy<HttpResponseMessage>.Handle<Exception>().RetryAsync(options.RetryCount, (ex, ts) =>
                {
                    Console.WriteLine($"服务{name}重试开启，异常消息：{ex.Exception?.Message}");
                    Console.WriteLine($"服务{name}重试第：{ts}次");
                    if (options.RetryCountAction != null)//委托方法
                    {
                        options.ActionAchieve<object>(options.RetryCountAction, ts);
                    }
                })
               );
            }
            if (options.RetryTimeoutArray != null)
            {
                // 1.3.2 重试策略  数组(第一次100ms 二次200ms) 
                httpClientBuilder.AddPolicyHandler(Policy<HttpResponseMessage>
                   .Handle<TimeoutRejectedException>()
                   .WaitAndRetryAsync(options.RetryTimeoutArray, (ex, ts) =>
                   {
                       Console.WriteLine($"服务{name}重试超时开启，异常消息：{ex.Exception?.Message}");
                       Console.WriteLine($"服务{name} 等待：{ts.TotalMilliseconds}毫秒后重试");})
                );
            }
            if (options.TimeoutTime > 0)
            {
                // 1.4 超时策略
                //一个是悲观策略（Pessimistic），一个是乐观策略（Optimistic）
                //悲观策略超时后会直接抛异常，而乐观策略则不会，而只是触发CancellationTokenSource.Cancel函数，需要等待委托自行终止操作。一般情况下，我们都会用悲观策略。
                httpClientBuilder.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(options.TimeoutTime), TimeoutStrategy.Pessimistic));
            }
            return services;
        }

    }
}
