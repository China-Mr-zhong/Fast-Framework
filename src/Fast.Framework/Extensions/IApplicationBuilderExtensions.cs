﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Fast.Framework.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Fast.Framework.Extensions
{

    /// <summary>
    /// 应用程序构建扩展类
    /// </summary>
    public static class IApplicationBuilderExtensions
    {

        /// <summary>
        /// 使用实体缓存
        /// </summary>
        /// <param name="builder">应用程序建造</param>
        /// <param name="fileName">文件名称</param>
        /// <returns></returns>
        public static IApplicationBuilder UseEntityCache(this IApplicationBuilder builder, string fileName)
        {
            Task.Run(() =>
            {
                var logger = builder.ApplicationServices.GetService(typeof(ILogger<IApplicationBuilder>)) as ILogger<IApplicationBuilder>;
                try
                {
                    var assembly = Assembly.LoadFrom(fileName);
                    var types = assembly.GetTypes().Where(w => w.GetInterface(typeof(IEntityCache).FullName) != null).ToList();
                    foreach (var type in types)
                    {
                        var entityInfo = type.GetEntityInfo();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"加载实体信息异常:{ex.Message}");
                }
            });
            return builder;
        }
    }
}
