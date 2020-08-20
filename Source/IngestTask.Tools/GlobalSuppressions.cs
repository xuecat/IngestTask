// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1712:不要对枚举值使用类名作为前缀", Justification = "<挂起>", Scope = "type", Target = "~T:IngestTask.Tools.Msv.MG_AudioWriteType")]
[assembly: SuppressMessage("Performance", "CA1815:重写值类型上的 Equals 和相等运算符", Justification = "<挂起>", Scope = "type", Target = "~T:IngestTask.Tools.Msv.TS_Signal_Info")]
[assembly: SuppressMessage("Usage", "CA2227:集合属性应为只读", Justification = "<挂起>", Scope = "member", Target = "~P:IngestTask.Tools.Msv.TS_DataChannelInfoEx.pTS_PgmInfo")]
[assembly: SuppressMessage("Performance", "CA1815:重写值类型上的 Equals 和相等运算符", Justification = "<挂起>", Scope = "type", Target = "~T:IngestTask.Tools.Msv.SDISignalStatus")]
[assembly: SuppressMessage("Performance", "CA1815:重写值类型上的 Equals 和相等运算符", Justification = "<挂起>", Scope = "type", Target = "~T:IngestTask.Tools.Msv.UploadInfo")]
[assembly: SuppressMessage("Performance", "CA1815:重写值类型上的 Equals 和相等运算符", Justification = "<挂起>", Scope = "type", Target = "~T:IngestTask.Tools.Msv.SignalSourceFormat")]
[assembly: SuppressMessage("Performance", "CA1815:重写值类型上的 Equals 和相等运算符", Justification = "<挂起>", Scope = "type", Target = "~T:IngestTask.Tools.Msv.disk_info")]
[assembly: SuppressMessage("Performance", "CA1815:重写值类型上的 Equals 和相等运算符", Justification = "<挂起>", Scope = "type", Target = "~T:IngestTask.Tools.Msv.check_rest")]
[assembly: SuppressMessage("Performance", "CA1819:属性不应返回数组", Justification = "<挂起>", Scope = "member", Target = "~P:IngestTask.Tools.Msv.TSParam.dwRev")]
[assembly: SuppressMessage("Performance", "CA1819:属性不应返回数组", Justification = "<挂起>", Scope = "member", Target = "~P:IngestTask.Tools.Msv.TASK_PARAM.dwInFrame")]
[assembly: SuppressMessage("Performance", "CA1819:属性不应返回数组", Justification = "<挂起>", Scope = "member", Target = "~P:IngestTask.Tools.Msv.TASK_PARAM.dwOutFrame")]
[assembly: SuppressMessage("Performance", "CA1819:属性不应返回数组", Justification = "<挂起>", Scope = "member", Target = "~P:IngestTask.Tools.Msv.TASK_PARAM.dwTotalFrame")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.ClientParam2MSVTskParam(IngestTask.Tools.Msv.TaskParam,IngestTask.Tools.Msv.TASK_PARAM@)")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.MSVTskParam2ClientParam(IngestTask.Tools.Msv.TASK_PARAM,IngestTask.Tools.Msv.TaskParam@)")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.QuerySDIFormat(System.Int32,System.String,System.Int32@,Sobey.Core.Log.ILogger)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.Record(System.Int32,System.String,System.Int32@,Sobey.Core.Log.ILogger)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.RecordReady(System.Int32,System.String,IngestTask.Tools.Msv.TaskParam@,System.String,System.String,Sobey.Core.Log.ILogger)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.QueryTaskState(System.Int32,System.String,IngestTask.Tools.Msv.TaskParam@,Sobey.Core.Log.ILogger)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.QueryState(System.Int32,System.String,System.Int32@,Sobey.Core.Log.ILogger)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.Stop(System.Int32,System.String,System.Int32,System.Int32@,Sobey.Core.Log.ILogger)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.Trace(System.Int32,System.String,IngestTask.Tools.Msv.TaskParam@,System.String,System.String,Sobey.Core.Log.ILogger)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:验证公共方法的参数", Justification = "<挂起>", Scope = "member", Target = "~M:IngestTask.Tools.Msv.MsvClientCtrlSDK.QuerySignalStatus(System.Int32,System.String,IngestTask.Tools.Msv.SDISignalDetails@,Sobey.Core.Log.ILogger)~System.Boolean")]
[assembly: SuppressMessage("Usage", "CA2227:集合属性应为只读", Justification = "<挂起>", Scope = "member", Target = "~P:IngestTask.Tools.Dto.PeriodParamResponse.Params")]
[assembly: SuppressMessage("Usage", "CA2227:集合属性应为只读", Justification = "<挂起>", Scope = "member", Target = "~P:IngestTask.Tools.Dto.PeriodParam.Params")]
