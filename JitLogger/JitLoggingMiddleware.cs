﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WonderTools.JitLogger;
using Newtonsoft.Json;

namespace WonderTools.JitLogger
{
    public class JitLoggingMiddleware
    {
        public JitLoggingMiddleware(JitLogRepository jitLogRepository, JitLoggerOptions options)
        {
            _jitLogRepository = jitLogRepository;
            _options = options;
        }

        private JitLogRepository _jitLogRepository;
        private readonly JitLoggerOptions _options;

        public async Task Process(HttpContext context, Func<Task> next)
        {
            var path = context.Request.Path;
            if (IsRequestForJitLogs(path)) await HandleJitLogsRequest(context);
            else if (IsRequestForJitUi(path)) await HandleJitUiRequest(context);
            else await next.Invoke();
        }

        private async Task HandleJitUiRequest(HttpContext context)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "text/html";
            var html = HtmlGenerator.GetHtml(_options.LoggerName, _jitLogRepository.GetLogs());
            await context.Response.WriteAsync(html, Encoding.UTF8);
        }

        private async Task HandleJitLogsRequest(HttpContext context)
        {
            
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            var logs = _jitLogRepository.GetLogs();

            logs = FilterLogsBasedOnQueryString(context, logs);

            var logsModel = new LogsModel()
            {
                Name = _options.LoggerName,
                Logs = logs,
            };

            var jsonString = JsonConvert.SerializeObject(logsModel);
            await context.Response.WriteAsync(jsonString, Encoding.UTF8);
        }

        private static List<Log> FilterLogsBasedOnQueryString(HttpContext context, List<Log> logs)
        {
            var QueryParameter = "exclusion-log-id-limit";
            if (!context.Request.Query.ContainsKey(QueryParameter)) return logs;
            var idAsString = context.Request.Query[QueryParameter].ToString();
            if (!Int32.TryParse(idAsString, out var id)) return logs;
            if (!logs.Any(x => x.LogId == id)) return logs;
            return logs.Where(x => x.LogId > id).ToList();
        }

        private bool IsRequestForJitUi(string path)
        {
            return IsRequestValid(path, "/ui");
        }

        private bool IsRequestForJitLogs(string path)
        {
            return IsRequestValid(path, "/logs");
        }

        private bool IsRequestValid(string basePath, string additionalPath)
        {
            var path = _options.JitEndPointBaseUrl + additionalPath;
            if (string.IsNullOrWhiteSpace(basePath)) return false;
            if (basePath.Equals(path, StringComparison.InvariantCultureIgnoreCase)) return true;
            if (basePath.Equals(path + "/", StringComparison.InvariantCultureIgnoreCase)) return true;
            return false;
        }
    }

    public class LogsModel
    {
        public string Name { get; set; }
        public List<Log> Logs { get; set; }
    }
}