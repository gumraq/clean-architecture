using Cargo.Infrastructure.Data.Model.Settings.CommPayloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Cargo.Application
{
    internal static class LinqExtention
    {
        public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string orderByProperty,
                          bool desc)
        {
            string command = desc ? "OrderByDescending" : "OrderBy";
            var type = typeof(TEntity);
            var property = type.GetProperty(orderByProperty);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, property.PropertyType },
                                          source.Expression, Expression.Quote(orderByExpression));
            return source.Provider.CreateQuery<TEntity>(resultExpression);
        }

        #region CommPayloads
        public static IEnumerable<CommPayloadNode> Descendants(this CommPayloadNode root)
        {
            var nodes = new Stack<CommPayloadNode>(new[] { root });
            while (nodes.Any())
            {
                CommPayloadNode node = nodes.Pop();
                //  if (node.Childs==null || !node.Childs.Any())
                {
                    yield return node;
                }

                foreach (var n in node.Childs) nodes.Push(n);
            }
        }

        internal static IEnumerable<CommPayloadNode> Ancestors(this CommPayloadNode node)
        {
            yield return node;

            CommPayloadNode parent = node.Parent;
            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
            }
        }
        #endregion
    }

}
