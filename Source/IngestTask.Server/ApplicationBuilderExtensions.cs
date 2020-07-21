namespace IngestTask.Server
{
    using System;
    using IngestTask.Server.Extensions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    internal static partial class ApplicationBuilderExtensions
    {
        
        public static IApplicationBuilder UseCustomSerilogRequestLogging(this IApplicationBuilder application, ILoggerFactory loggerFactory)
        {
#pragma warning disable CA2000 // 丢失范围之前释放对象
            loggerFactory.AddProvider(new CustomConsoleLoggerProvider(
                              new CustomConsoleLoggerConfiguration
                              {
                                  LogLevel = LogLevel.Error,
                                  Color = ConsoleColor.Red
                              }));
            loggerFactory.AddProvider(new CustomConsoleLoggerProvider(
                              new CustomConsoleLoggerConfiguration
                              {
                                  LogLevel = LogLevel.Information,
                                  Color = ConsoleColor.Green
                              }));
            loggerFactory.AddProvider(new CustomConsoleLoggerProvider(
                              new CustomConsoleLoggerConfiguration
                              {
                                  LogLevel = LogLevel.Debug,
                                  Color = ConsoleColor.Blue
                              }));
#pragma warning restore CA2000 // 丢失范围之前释放对象
            return application;
        }
    }
}
