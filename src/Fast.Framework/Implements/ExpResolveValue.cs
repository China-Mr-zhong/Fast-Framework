﻿using Fast.Framework.CustomAttribute;
using Fast.Framework.Enum;
using Fast.Framework.Extensions;
using Fast.Framework.Interfaces;
using Fast.Framework.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fast.Framework.Implements
{

    /// <summary>
    /// 表达式解析值
    /// </summary>
    public class ExpResolveValue : IExpResolveValue
    {

        /// <summary>
        /// 成员信息
        /// </summary>
        private readonly Stack<MemberInfoEx> memberInfos;

        /// <summary>
        /// 数组索引
        /// </summary>
        private Stack<int> arrayIndexs;

        /// <summary>
        /// 首次表达式
        /// </summary>
        private Expression bodyExpression;

        /// <summary>
        /// 是否取反
        /// </summary>
        private bool isNot;

        /// <summary>
        /// 方法调用前
        /// </summary>
        /// <returns></returns>
        public Action<object, MethodCallExpression> MethodCallBefore { get; set; }

        /// <summary>
        /// 方法调用后
        /// </summary>
        /// <returns></returns>
        public Action<object, object, MethodCallExpression> MethodCallAfter { get; set; }

        /// <summary>
        /// 构造方法
        /// </summary>
        public ExpResolveValue()
        {
            this.memberInfos = new Stack<MemberInfoEx>();
            this.arrayIndexs = new Stack<int>();
        }

        /// <summary>
        /// 访问
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        public object Visit(Expression node)
        {
            //Console.WriteLine($"当前访问 {node.NodeType} 类型表达式");
            switch (node)
            {
                case LambdaExpression:
                    {
                        return Visit(VisitLambda(node as LambdaExpression));
                    };
                case NewExpression:
                    {
                        return VisitNew(node as NewExpression);
                    }
                case MemberInitExpression:
                    {
                        return VisitMemberInit(node as MemberInitExpression);
                    }
                case ListInitExpression:
                    {
                        return VisitListInit(node as ListInitExpression);
                    }
                case NewArrayExpression:
                    {
                        return VisitNewArray(node as NewArrayExpression);
                    }
                case UnaryExpression:
                    {
                        return VisitUnary(node as UnaryExpression);
                    };
                case BinaryExpression:
                    {
                        return VisitBinary(node as BinaryExpression);
                    }
                case MethodCallExpression:
                    {
                        return VisitMethodCall(node as MethodCallExpression);
                    }
                case ConditionalExpression:
                    {
                        return VisitConditional(node as ConditionalExpression);
                    }
                case MemberExpression:
                    {
                        return Visit(VisitMember(node as MemberExpression));
                    }
                case ConstantExpression:
                    {
                        return VisitConstant(node as ConstantExpression);
                    };
                default: return null;
            }
        }

        /// <summary>
        /// 访问Lambda表达式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private Expression VisitLambda(LambdaExpression node)
        {
            bodyExpression = node.Body;
            return bodyExpression;
        }

        /// <summary>
        /// 访问一元表达式
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private object VisitUnary(UnaryExpression node)
        {
            isNot = node.NodeType == ExpressionType.Not;
            var value = Visit(node.Operand);
            if (node.NodeType == ExpressionType.Negate)
            {
                return $"-{value}";
            }
            return value;
        }

        /// <summary>
        /// 访问列表初始化表达式
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private object VisitListInit(ListInitExpression node)
        {
            var args = new List<object>();
            foreach (var item in node.NewExpression.Arguments)
            {
                args.Add(Visit(item));
            }
            IList list;
            if (args.Count > 0)
            {
                list = Activator.CreateInstance(node.NewExpression.Type, args) as IList;
            }
            else
            {
                list = Activator.CreateInstance(node.NewExpression.Type) as IList;
            }
            foreach (var item in node.Initializers)
            {
                list.Add(Visit(item.Arguments[0]));
            }
            return list;
        }

        /// <summary>
        /// 访问New表达式
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private object VisitNew(NewExpression node)
        {
            var arguments = new List<object>();
            foreach (var item in node.Arguments)
            {
                arguments.Add(Visit(item));
            }
            var obj = node.Constructor.Invoke(arguments.ToArray());
            return obj;
        }

        /// <summary>
        /// 访问成员初始化表达式
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private object VisitMemberInit(MemberInitExpression node)
        {
            var obj = Visit(node.NewExpression);
            foreach (var item in node.Bindings)
            {
                if (item.BindingType == MemberBindingType.Assignment)
                {
                    var memberAssignment = item as MemberAssignment;
                    object value = Visit(memberAssignment.Expression);
                    if (memberAssignment.Member.MemberType == MemberTypes.Property)
                    {
                        var propertyInfo = memberAssignment.Member as PropertyInfo;
                        propertyInfo.SetValue(obj, value);
                    }
                    else if (memberAssignment.Member.MemberType == MemberTypes.Field)
                    {
                        var fieldInfo = memberAssignment.Member as FieldInfo;
                        fieldInfo.SetValue(obj, value);
                    }
                }
            }
            return obj;
        }

        /// <summary>
        /// 访问对象数组表达式
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private object VisitNewArray(NewArrayExpression node)
        {
            var args = new List<object>();
            foreach (var item in node.Expressions)
            {
                args.Add(Visit(item));
            }

            var array = Array.CreateInstance(node.Type.GetElementType(), args.Count);

            for (int i = 0; i < args.Count; i++)
            {
                array.SetValue(args[i], i);
            }

            return array;
        }

        /// <summary>
        /// 访问二元表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private object VisitBinary(BinaryExpression node)
        {
            if (node.Method != null)
            {
                var arguments = new List<object>
                {
                    Visit(node.Left),
                    Visit(node.Right)
                };
                return node.Method.Invoke(this, arguments.ToArray());
            }
            var leftValue = Visit(node.Left);
            var rightValue = Visit(node.Right);

            #region 加减乘除
            if (node.NodeType == ExpressionType.Add)
            {
                if (node.Left.Type.Equals(typeof(long)) || node.Right.Type.Equals(typeof(long)))
                {
                    return Convert.ToInt64(leftValue) + Convert.ToInt64(rightValue);
                }
                else
                {
                    return Convert.ToDecimal(leftValue) + Convert.ToDecimal(rightValue);
                }
            }
            else if (node.NodeType == ExpressionType.Subtract)
            {
                if (node.Left.Type.Equals(typeof(long)) || node.Right.Type.Equals(typeof(long)))
                {
                    return Convert.ToInt64(leftValue) - Convert.ToInt64(rightValue);
                }
                else
                {
                    return Convert.ToDecimal(leftValue) - Convert.ToDecimal(rightValue);
                }
            }
            else if (node.NodeType == ExpressionType.Multiply)
            {
                if (node.Left.Type.Equals(typeof(long)) || node.Right.Type.Equals(typeof(long)))
                {
                    return Convert.ToInt64(leftValue) * Convert.ToInt64(rightValue);
                }
                else
                {
                    return Convert.ToDecimal(leftValue) * Convert.ToDecimal(rightValue);
                }
            }
            else if (node.NodeType == ExpressionType.Divide)
            {
                if (node.Left.Type.Equals(typeof(long)) || node.Right.Type.Equals(typeof(long)))
                {
                    return Convert.ToInt64(leftValue) / Convert.ToInt64(rightValue);
                }
                else
                {
                    return Convert.ToDecimal(leftValue) / Convert.ToDecimal(rightValue);
                }
            }
            #endregion

            #region 比较
            if (node.NodeType == ExpressionType.Equal)
            {
                return leftValue.Equals(rightValue);
            }
            else if (node.NodeType == ExpressionType.NotEqual)
            {
                return !leftValue.Equals(rightValue);
            }
            else if (node.NodeType == ExpressionType.GreaterThan)
            {
                if (node.Left.Type.Equals(typeof(DateTime)))
                {
                    return Convert.ToDateTime(leftValue) > Convert.ToDateTime(rightValue);
                }
                else if (node.Left.Type.Equals(typeof(long)) || node.Right.Type.Equals(typeof(long)))
                {
                    return Convert.ToInt64(leftValue) > Convert.ToInt64(rightValue);
                }
                else
                {
                    return Convert.ToDecimal(leftValue) > Convert.ToDecimal(rightValue);
                }
            }
            else if (node.NodeType == ExpressionType.LessThan)
            {
                if (node.Left.Type.Equals(typeof(DateTime)))
                {
                    return Convert.ToDateTime(leftValue) < Convert.ToDateTime(rightValue);
                }
                else if (node.Left.Type.Equals(typeof(long)) || node.Right.Type.Equals(typeof(long)))
                {
                    return Convert.ToInt64(leftValue) < Convert.ToInt64(rightValue);
                }
                else
                {
                    return Convert.ToDecimal(leftValue) < Convert.ToDecimal(rightValue);
                }
            }
            else if (node.NodeType == ExpressionType.GreaterThanOrEqual)
            {
                if (node.Left.Type.Equals(typeof(DateTime)))
                {
                    return Convert.ToDateTime(leftValue) >= Convert.ToDateTime(rightValue);
                }
                else if (node.Left.Type.Equals(typeof(long)) || node.Right.Type.Equals(typeof(long)))
                {
                    return Convert.ToInt64(leftValue) >= Convert.ToInt64(rightValue);
                }
                else
                {
                    return Convert.ToDecimal(leftValue) >= Convert.ToDecimal(rightValue);
                }
            }
            else if (node.NodeType == ExpressionType.LessThanOrEqual)
            {
                if (node.Left.Type.Equals(typeof(DateTime)))
                {
                    return Convert.ToDateTime(leftValue) <= Convert.ToDateTime(rightValue);
                }
                else if (node.Left.Type.Equals(typeof(long)) || node.Right.Type.Equals(typeof(long)))
                {
                    return Convert.ToInt64(leftValue) <= Convert.ToInt64(rightValue);
                }
                else
                {
                    return Convert.ToDecimal(leftValue) <= Convert.ToDecimal(rightValue);
                }
            }
            #endregion

            return null;
        }

        /// <summary>
        /// 访问方法表达式
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private object VisitMethodCall(MethodCallExpression node)
        {
            var arguments = new List<object>();
            foreach (var item in node.Arguments)
            {
                if (item.Type.FullName.StartsWith("System.Linq.Expressions.Expression"))
                {
                    var unaryExpression = item as UnaryExpression;
                    arguments.Add(unaryExpression.Operand);
                }
                else
                {
                    arguments.Add(Visit(item));
                }
            }
            object result;
            if (node.Object == null)
            {
                MethodCallBefore?.Invoke(null, node);
                result = node.Method.Invoke(this, arguments.ToArray());
                MethodCallAfter?.Invoke(null, result, node);
            }
            else
            {
                var obj = Visit(node.Object);
                MethodCallBefore?.Invoke(obj, node);
                result = node.Method.Invoke(obj, arguments.ToArray());
                MethodCallAfter?.Invoke(obj, result, node);
            }
            if (result != null && result.GetType().Equals(typeof(bool)) && isNot)
            {
                result = !Convert.ToBoolean(result);
                isNot = false;
            }
            return result;
        }

        /// <summary>
        /// 访问条件表达式树
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private object VisitConditional(ConditionalExpression node)
        {
            var value = Convert.ToBoolean(Visit(node.Test));
            if (value)
            {
                return Visit(node.IfTrue);
            }
            else
            {
                return Visit(node.IfFalse);
            }
        }

        /// <summary>
        /// 访问成员表达式
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private Expression VisitMember(MemberExpression node)
        {
            #region Datetime特殊处理
            if (node.Type.Equals(typeof(DateTime)) && node.Expression == null)
            {
                memberInfos.Push(new MemberInfoEx()
                {
                    ArrayIndex = arrayIndexs,
                    MemberInfo = node.Member
                });
                return Expression.Constant(default(DateTime));
            }
            #endregion

            if (node.Expression != null)
            {
                if (node.Expression is ParameterExpression)
                {
                    throw new Exception($"{node},非表达式无法获取值.");
                }
                if (node.Expression.NodeType == ExpressionType.MemberAccess || node.Expression.NodeType == ExpressionType.Constant)
                {
                    memberInfos.Push(new MemberInfoEx()
                    {
                        ArrayIndex = arrayIndexs,
                        MemberInfo = node.Member
                    });
                }
            }

            if (arrayIndexs.Count > 0)
            {
                arrayIndexs = new Stack<int>();
            }
            return node.Expression;
        }

        /// <summary>
        /// 访问常量表达式
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns></returns>
        private object VisitConstant(ConstantExpression node)
        {
            var value = node.Value;
            if (memberInfos.Count > 0)
            {
                value = memberInfos.GetValue(value, out var memberName);//获取成员变量值
                memberInfos.Clear();
            }
            if (node.Type.Equals(typeof(bool)) && isNot)
            {
                value = !Convert.ToBoolean(value);
                isNot = false;
            }
            return value;
        }
    }
}
