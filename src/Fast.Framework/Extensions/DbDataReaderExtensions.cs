﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Fast.Framework.Cache;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Data;
using Fast.Framework.Utils;
using System.Text.Json;

namespace Fast.Framework.Extensions
{

    /// <summary>
    /// DbDataReader扩展类
    /// </summary>
    public static class DbDataReaderExtensions
    {
        /// <summary>
        /// 获取方法缓存
        /// </summary>
        private static readonly Dictionary<Type, MethodInfo> getMethodCache;

        /// <summary>
        /// 转换方法名称
        /// </summary>
        private static readonly Dictionary<Type, string> convertMethodName;

        /// <summary>
        /// 是否DBNull方法
        /// </summary>
        private static readonly MethodInfo isDBNullMethod;

        /// <summary>
        /// 是否空或空格字符串方法
        /// </summary>
        private static readonly MethodInfo isNullOrWhiteSpaceMethod;

        #region 初始化
        /// <summary>
        /// 静态构造方法
        /// </summary>
        static DbDataReaderExtensions()
        {
            var getValueMethod = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });

            isDBNullMethod = typeof(IDataRecord).GetMethod("IsDBNull", new Type[] { typeof(int) });

            isNullOrWhiteSpaceMethod = typeof(string).GetMethod("IsNullOrWhiteSpace", new Type[] { typeof(string) });

            getMethodCache = new Dictionary<Type, MethodInfo>()
            {
                { typeof(object),getValueMethod},
                { typeof(short),typeof(IDataRecord).GetMethod("GetInt16", new Type[] { typeof(int) })},
                { typeof(ushort),getValueMethod},
                { typeof(int),typeof(IDataRecord).GetMethod("GetInt32", new Type[] { typeof(int) })},
                { typeof(uint),getValueMethod},
                { typeof(long),typeof(IDataRecord).GetMethod("GetInt64", new Type[] { typeof(int) })},
                { typeof(ulong),getValueMethod},
                { typeof(float),typeof(IDataRecord).GetMethod("GetFloat", new Type[] { typeof(int) })},
                { typeof(double),typeof(IDataRecord).GetMethod("GetDouble", new Type[] { typeof(int) })},
                { typeof(decimal),typeof(IDataRecord).GetMethod("GetDecimal", new Type[] { typeof(int) })},
                { typeof(char),typeof(IDataRecord).GetMethod("GetChar", new Type[] { typeof(int) })},
                { typeof(byte),typeof(IDataRecord).GetMethod("GetByte", new Type[] { typeof(int) })},
                { typeof(sbyte),getValueMethod},
                { typeof(bool),typeof(IDataRecord).GetMethod("GetBoolean",new Type[]{ typeof(int)})},
                { typeof(string),typeof(IDataRecord).GetMethod("GetString",new Type[]{ typeof(int)})},
                { typeof(DateTime),typeof(IDataRecord).GetMethod("GetDateTime",new Type[]{ typeof(int)})}
            };

            convertMethodName = new Dictionary<Type, string>()
            {
                { typeof(short),"ToInt16"},
                { typeof(ushort),"ToUInt16"},
                { typeof(int),"ToInt32"},
                { typeof(uint),"ToUInt32"},
                { typeof(long),"ToInt64"},
                { typeof(ulong),"ToUInt64"},
                { typeof(float),"ToSingle"},
                { typeof(double),"ToDouble"},
                { typeof(decimal),"ToDecimal"},
                { typeof(char),"ToChar"},
                { typeof(byte),"ToByte"},
                { typeof(sbyte),"ToSByte"},
                { typeof(bool),"ToBoolean"},
                { typeof(string),"ToString"},
                { typeof(DateTime),"ToDateTime"}
            };
        }
        #endregion

        /// <summary>
        /// 绑定表达式构建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbColumns">数据库列</param>
        /// <returns></returns>
        private static Func<DbDataReader, T> BindExpBuild<T>(this ReadOnlyCollection<DbColumn> dbColumns)
        {
            var parameterExpression = Expression.Parameter(typeof(DbDataReader), "r");

            var arguments = new List<Expression>();
            var memberBindings = new List<MemberBinding>();

            var entityInfo = typeof(T).GetEntityInfo();

            for (int i = 0; i < dbColumns.Count; i++)
            {
                var columnInfo = entityInfo.ColumnsInfos.FirstOrDefault(f => f.ColumnName == dbColumns[i].ColumnName);

                if (columnInfo == null && entityInfo.IsAnonymousType && dbColumns[i].ColumnName.StartsWith("fast_args_index_"))
                {
                    var index = Convert.ToInt32(dbColumns[i].ColumnName.Split("_")[3]);
                    arguments.Insert(index, Expression.Default(entityInfo.ColumnsInfos[index].PropertyInfo.PropertyType));
                }
                else if (columnInfo != null)
                {
                    var constantExpression = Expression.Constant(i);
                    var isDBNullMethodCall = Expression.Call(parameterExpression, isDBNullMethod, constantExpression);
                    Expression getValueExpression;

                    if (!getMethodCache.ContainsKey(dbColumns[i].DataType))
                    {
                        throw new Exception($"该类型不支持绑定{dbColumns[i].DataType.FullName}.");
                    }

                    var memberType = columnInfo.IsField ? columnInfo.FieldInfo.FieldType : columnInfo.PropertyInfo.PropertyType;

                    var getMethod = getMethodCache[dbColumns[i].DataType];

                    if (columnInfo.IsJson)
                    {
                        if (!getMethod.ReturnType.Equals(typeof(string)))
                        {
                            throw new Exception($"数据库列{dbColumns[i].ColumnName}不是字符串类型不支持Json序列化.");
                        }
                        var deserializeGenericMethod = typeof(Json).GetMethod("Deserialize", new Type[] { typeof(string), typeof(JsonSerializerOptions) });

                        var deserializeMethod = deserializeGenericMethod.MakeGenericMethod(columnInfo.IsField ? columnInfo.FieldInfo.FieldType : columnInfo.PropertyInfo.PropertyType);

                        MemberInfo memberInfo = memberInfo = columnInfo.IsField ? columnInfo.FieldInfo : columnInfo.PropertyInfo;

                        if (entityInfo.IsAnonymousType)
                        {
                            memberInfo = entityInfo.EntityType.GetField($"<{memberInfo.Name}>i__Field", BindingFlags.NonPublic | BindingFlags.Instance);
                        }

                        var getStringCall = Expression.Call(parameterExpression, getMethod, constantExpression);
                        var isNullOrWhiteSpaceCall = Expression.Call(null, isNullOrWhiteSpaceMethod, new List<Expression>() { getStringCall });
                        getValueExpression = Expression.Call(null, deserializeMethod, new List<Expression>() { getStringCall, Expression.Default(typeof(JsonSerializerOptions)) });
                        getValueExpression = Expression.Condition(isNullOrWhiteSpaceCall, Expression.Default(memberType), getValueExpression);
                    }
                    else
                    {
                        var mapperType = memberType;
                        var isConvert = false;

                        //获取可空类型具体类型
                        if (columnInfo.IsNullable)
                        {
                            mapperType = mapperType.GenericTypeArguments[0];
                            isConvert = true;
                        }

                        getValueExpression = Expression.Call(parameterExpression, getMethod, constantExpression);

                        //返回类型
                        var returnType = getMethod.ReturnType;

                        if (getMethod.ReturnType.Equals(typeof(float)) || getMethod.ReturnType.Equals(typeof(double)) || getMethod.ReturnType.Equals(typeof(decimal)))
                        {
                            //格式化去除后面多余的0
                            var toString = getMethod.ReturnType.GetMethod("ToString", new Type[] { typeof(string) });
                            getValueExpression = Expression.Call(getValueExpression, toString, Expression.Constant("G0"));
                            returnType = typeof(string);//重定义返回类型
                        }

                        if (mapperType == typeof(object))
                        {
                            isConvert = true;
                        }
                        else if (mapperType != returnType)
                        {
                            if (mapperType.Equals(typeof(Guid)))
                            {
                                getValueExpression = Expression.New(typeof(Guid).GetConstructor(new Type[] { typeof(string) }), getValueExpression);
                            }
                            else
                            {
                                if (!convertMethodName.ContainsKey(mapperType))
                                {
                                    throw new Exception($"该类型转换不受支持{mapperType.FullName}.");
                                }
                                var convertMethodInfo = typeof(Convert).GetMethod(convertMethodName[mapperType], new Type[] { returnType });
                                getValueExpression = Expression.Call(convertMethodInfo, getValueExpression);
                            }
                        }

                        if (isConvert)
                        {
                            getValueExpression = Expression.Convert(getValueExpression, memberType);
                        }
                    }

                    //数据列允许DBNull增加IsDbNull判断
                    if (dbColumns[i].AllowDBNull == null || dbColumns[i].AllowDBNull.Value)
                    {
                        getValueExpression = Expression.Condition(isDBNullMethodCall, Expression.Default(memberType), getValueExpression);
                    }

                    if (entityInfo.IsAnonymousType)
                    {
                        arguments.Add(getValueExpression);
                    }
                    else
                    {
                        memberBindings.Add(Expression.Bind(columnInfo.IsField ? columnInfo.FieldInfo : columnInfo.PropertyInfo, getValueExpression));
                    }
                }
            }
            Expression initExpression = entityInfo.IsAnonymousType ? Expression.New(entityInfo.EntityType.GetConstructors()[0], arguments) : Expression.MemberInit(Expression.New(entityInfo.EntityType), memberBindings);
            var lambdaExpression = Expression.Lambda<Func<DbDataReader, T>>(initExpression, parameterExpression);
            return lambdaExpression.Compile();
        }

        /// <summary>
        /// 绑定表达式构建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbColumn">数据库列</param>
        /// <returns></returns>
        private static Func<DbDataReader, T> BindExpBuild<T>(this DbColumn dbColumn)
        {
            var type = typeof(T);
            if (!getMethodCache.ContainsKey(dbColumn.DataType))
            {
                throw new Exception($"该类型不支持绑定{dbColumn.DataType.FullName}.");
            }
            var mapperType = type;
            var isConvert = false;

            var parameterExpression = Expression.Parameter(typeof(DbDataReader), "r");

            //获取可空类型具体类型
            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                mapperType = type.GenericTypeArguments[0];
                isConvert = true;
            }
            var constantExpression = Expression.Constant(0);
            var isDBNullMethodCall = Expression.Call(parameterExpression, isDBNullMethod, constantExpression);
            var getMethod = getMethodCache[dbColumn.DataType];
            Expression getValueExpression = Expression.Call(parameterExpression, getMethod, constantExpression);

            //返回类型
            var returnType = getMethod.ReturnType;

            if (getMethod.ReturnType.Equals(typeof(float)) || getMethod.ReturnType.Equals(typeof(double)) || getMethod.ReturnType.Equals(typeof(decimal)))
            {
                //格式化去除后面多余的0
                var toString = getMethod.ReturnType.GetMethod("ToString", new Type[] { typeof(string) });
                getValueExpression = Expression.Call(getValueExpression, toString, Expression.Constant("G0"));
                returnType = typeof(string);//重定义返回类型
            }

            if (mapperType == typeof(object))
            {
                isConvert = true;
            }
            else if (mapperType != returnType)
            {
                if (mapperType.Equals(typeof(Guid)))
                {
                    getValueExpression = Expression.New(typeof(Guid).GetConstructor(new Type[] { typeof(string) }), getValueExpression);
                }
                else
                {
                    if (!convertMethodName.ContainsKey(mapperType))
                    {
                        throw new Exception($"该类型转换不受支持{mapperType.FullName}.");
                    }
                    var convertMethodInfo = typeof(Convert).GetMethod(convertMethodName[mapperType], new Type[] { returnType });
                    getValueExpression = Expression.Call(convertMethodInfo, getValueExpression);
                }
            }

            if (isConvert)
            {
                getValueExpression = Expression.Convert(getValueExpression, type);
            }

            //数据列允许DBNull增加IsDbNull判断
            if (dbColumn.AllowDBNull == null || dbColumn.AllowDBNull.Value)
            {
                getValueExpression = Expression.Condition(isDBNullMethodCall, Expression.Default(type), getValueExpression);
            }

            var lambdaExpression = Expression.Lambda<Func<DbDataReader, T>>(getValueExpression, parameterExpression);
            return lambdaExpression.Compile();
        }

        /// <summary>
        /// 获取数据绑定表达式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbColumns">数据库列</param>
        /// <returns></returns>
        private static Func<DbDataReader, T> GetDataBindExp<T>(this ReadOnlyCollection<DbColumn> dbColumns)
        {
            var type = typeof(T);

            var keys = dbColumns.Select(s =>
            {
                if (s.AllowDBNull == null)
                {
                    return $"{s.ColumnName}_{s.DataTypeName}_True";
                }
                else
                {
                    return $"{s.ColumnName}_{s.DataTypeName}_{s.AllowDBNull}";
                }
            });

            var cacheKey = $"{type.FullName}_DataBindingExpBuild_{string.Join(",", keys)}";

            return StaticCache<Func<DbDataReader, T>>.GetOrAdd(cacheKey, () =>
            {
                if (type.IsClass && type != typeof(string))
                {
                    return dbColumns.BindExpBuild<T>();
                }
                return dbColumns[0].BindExpBuild<T>();
            });
        }

        /// <summary>
        /// 最终处理
        /// </summary>
        /// <param name="reader">阅读器</param>
        /// <returns></returns>
        private static void FinalProcessing(this DbDataReader reader)
        {
            if (!reader.NextResult())
            {
                reader.Close();
            }
        }

        /// <summary>
        /// 最终处理异步
        /// </summary>
        /// <param name="reader">阅读器</param>
        /// <returns></returns>
        private static async Task FinalProcessingAsync(this DbDataReader reader)
        {
            if (!await reader.NextResultAsync())
            {
                await reader.CloseAsync();
            }
        }

        /// <summary>
        /// 第一构建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataReader">数据读取</param>
        /// <returns></returns>
        public static T FirstBuild<T>(this DbDataReader dataReader)
        {
            var reader = dataReader;
            var dbColumns = reader.GetColumnSchema();
            T t = default;
            if (reader.Read())
            {
                var func = dbColumns.GetDataBindExp<T>();
                t = func.Invoke(reader);
            }
            reader.FinalProcessing();
            return t;
        }

        /// <summary>
        /// 第一构建异步
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataReader">数据读取</param>
        /// <returns></returns>
        public static async Task<T> FirstBuildAsync<T>(this Task<DbDataReader> dataReader)
        {
            var reader = await dataReader;
            var dbColumns = await reader.GetColumnSchemaAsync();
            T t = default;
            if (await reader.ReadAsync())
            {
                var func = dbColumns.GetDataBindExp<T>();
                t = func.Invoke(reader);
            }
            await reader.FinalProcessingAsync();
            return t;
        }

        /// <summary>
        /// 列表构建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataReader">数据读取</param>
        /// <returns></returns>
        public static List<T> ListBuild<T>(this DbDataReader dataReader)
        {
            var reader = dataReader;
            var dbColumns = reader.GetColumnSchema();
            var list = new List<T>();
            var func = dbColumns.GetDataBindExp<T>();
            while (reader.Read())
            {
                list.Add(func.Invoke(reader));
            }
            reader.FinalProcessing();
            return list;
        }

        /// <summary>
        /// 列表构建异步
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataReader">数据读取</param>
        /// <returns></returns>
        public static async Task<List<T>> ListBuildAsync<T>(this Task<DbDataReader> dataReader)
        {
            var reader = await dataReader;
            var dbColumns = await reader.GetColumnSchemaAsync();
            var list = new List<T>();
            var func = dbColumns.GetDataBindExp<T>();
            while (await reader.ReadAsync())
            {
                list.Add(func.Invoke(reader));
            }
            await reader.FinalProcessingAsync();
            return list;
        }

        /// <summary>
        /// 字典构建
        /// </summary>
        /// <param name="dataReader">数据读取</param>
        /// <returns></returns>
        public static Dictionary<string, object> DictionaryBuild(this DbDataReader dataReader)
        {
            var reader = dataReader;
            var data = new Dictionary<string, object>();
            var dbColumns = reader.GetColumnSchema();
            if (dbColumns.Count > 0 && reader.Read())
            {
                data = new Dictionary<string, object>();
                foreach (var c in dbColumns)
                {
                    data.Add(c.ColumnName, reader.IsDBNull(c.ColumnOrdinal.Value) ? null : reader.GetValue(c.ColumnOrdinal.Value));
                }
            }
            reader.FinalProcessing();
            return data;
        }

        /// <summary>
        /// 字典构建异步
        /// </summary>
        /// <param name="dataReader">数据读取</param>
        /// <returns></returns>
        public static async Task<Dictionary<string, object>> DictionaryBuildAsync(this Task<DbDataReader> dataReader)
        {
            var reader = await dataReader;
            var data = new Dictionary<string, object>();
            var dbColumns = await reader.GetColumnSchemaAsync();
            if (dbColumns.Count > 0 && await reader.ReadAsync())
            {
                data = new Dictionary<string, object>();
                foreach (var c in dbColumns)
                {
                    data.Add(c.ColumnName, reader.IsDBNull(c.ColumnOrdinal.Value) ? null : reader.GetValue(c.ColumnOrdinal.Value));
                }
            }
            await reader.FinalProcessingAsync();
            return data;
        }

        /// <summary>
        /// 字典列表构建
        /// </summary>
        /// <param name="dataReader">数据读取</param>
        /// <returns></returns>
        public static List<Dictionary<string, object>> DictionaryListBuild(this DbDataReader dataReader)
        {
            var reader = dataReader;
            var data = new List<Dictionary<string, object>>();
            var dbColumns = reader.GetColumnSchema();
            if (dbColumns.Count > 0)
            {
                while (reader.Read())
                {
                    var keyValues = new Dictionary<string, object>();
                    foreach (var c in dbColumns)
                    {
                        keyValues.Add(c.ColumnName, reader.IsDBNull(c.ColumnOrdinal.Value) ? null : reader.GetValue(c.ColumnOrdinal.Value));
                    }
                    data.Add(keyValues);
                }
            }
            reader.FinalProcessing();
            return data;
        }

        /// <summary>
        /// 字典列表构建异步
        /// </summary>
        /// <param name="dataReader">数据读取</param>
        /// <returns></returns>
        public static async Task<List<Dictionary<string, object>>> DictionaryListBuildAsync(this Task<DbDataReader> dataReader)
        {
            var reader = await dataReader;
            var data = new List<Dictionary<string, object>>();
            var dbColumns = await reader.GetColumnSchemaAsync();
            if (dbColumns.Count > 0)
            {
                while (await reader.ReadAsync())
                {
                    var keyValues = new Dictionary<string, object>();
                    foreach (var c in dbColumns)
                    {
                        keyValues.Add(c.ColumnName, reader.IsDBNull(c.ColumnOrdinal.Value) ? null : reader.GetValue(c.ColumnOrdinal.Value));
                    }
                    data.Add(keyValues);
                }
            }
            await reader.FinalProcessingAsync();
            return data;
        }
    }
}
