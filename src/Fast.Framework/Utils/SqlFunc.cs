﻿using Fast.Framework.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Fast.Framework.Extensions
{

    /// <summary>
    /// Sql函数
    /// </summary>
    public static class SqlFunc
    {

        #region 字符串函数
        /// <summary>
        /// 长度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int Len<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// 长度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int Length<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// 运算
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="op">运算符</param>
        /// <param name="obj2"></param>
        /// <returns></returns>
        public static bool Operation(object obj1, string op, object obj2)
        {
            return default;
        }

        #endregion

        #region 聚合函数

        /// <summary>
        /// 最大值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T Max<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// 最小值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T Min<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// 计数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int Count<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// 合计
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T Sum<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// 平均
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Avg<T>(T t)
        {
            return default;
        }
        #endregion

        #region 数学函数
        /// <summary>
        /// 绝对值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static T Abs<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// 四舍五入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="decimals">小数点</param>
        /// <returns></returns>
        public static decimal Round<T>(T t, int decimals)
        {
            return default;
        }
        #endregion

        #region 日期函数

        /// <summary>
        /// 日期差异
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns></returns>
        public static int DateDiff(DateTime startDate, DateTime endDate)
        {
            return default;
        }

        /// <summary>
        /// 日期差异
        /// </summary>
        /// <param name="dateType">日期类型</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns></returns>
        public static int DateDiff(DateType dateType, DateTime startDate, DateTime endDate)
        {
            return default;
        }

        /// <summary>
        /// 日期差异
        /// </summary>
        /// <param name="dateType">日期类型</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns></returns>
        public static int TimestampDiff(DateType dateType, DateTime startDate, DateTime endDate)
        {
            return default;
        }

        /// <summary>
        /// 年
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int Year<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// 月
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int Month<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// 日
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static int Day<T>(T t)
        {
            return default;
        }
        #endregion

        #region 其它函数

        /// <summary>
        /// 是否空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static T IsNull<T>(T t, T value)
        {
            return default;
        }

        /// <summary>
        /// 如果空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static T IfNull<T>(T t, T value)
        {
            return default;
        }

        /// <summary>
        /// 空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static T Nvl<T>(T t, T value)
        {
            return default;
        }

        /// <summary>
        /// 行ID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="orderFields">排序字段</param>
        /// <returns></returns>
        public static int RowNumber<T>(T orderFields)
        {
            return default;
        }

        #region Case When
        /// <summary>
        /// Case
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public static CaseWarp<T> Case<T>(T t)
        {
            return default;
        }

        /// <summary>
        /// CaseWhen
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static CaseWarp<T> CaseWhen<T>(bool value)
        {
            return default;
        }

        /// <summary>
        /// When
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">源</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static CaseWarp<T> When<T>(this CaseWarp<T> source, T value)
        {
            return default;
        }

        /// <summary>
        /// When
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">源</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static CaseWarp<T> When<T>(this CaseWarp<T> source, bool value)
        {
            return default;
        }

        /// <summary>
        /// Then
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source">源</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static CaseWarp<T> Then<T, TValue>(this CaseWarp<T> source, TValue value)
        {
            return default;
        }

        /// <summary>
        /// Else
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source">源</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static CaseWarp<T> Else<T, TValue>(this CaseWarp<T> source, TValue value)
        {
            return default;
        }

        /// <summary>
        /// End
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">源</param>
        /// <returns></returns>
        public static T End<T>(this CaseWarp<T> source)
        {
            return default;
        }
        #endregion

        #endregion
    }

    /// <summary>
    /// Case包装类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CaseWarp<T>
    {
    }
}

