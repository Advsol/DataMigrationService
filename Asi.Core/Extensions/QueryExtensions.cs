using Asi.Soa.Core.ContractUtilities;
using Asi.Soa.Core.DataContracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Asi.DataMigrationService.Core.Extensions
{
    /// <summary>
    /// templated extension methods to allow sorting by object property by giving the property name.
    /// e.g. sequence.OrderBy("Name", true) to order a sequence by the "Name" property.
    /// </summary>
    public static class QueryExtensions
    {
        #region Public Static Methods

        /// <summary>   Applies the specified query to an IQueryable. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="queryable">        The queryable. </param>
        /// <param name="query">            The query. </param>
        /// <param name="applySkipTake">    if set to <c>true</c> [apply skip take]. </param>
        /// <returns>   IQueryable{`0}. </returns>
        public static IQueryable<T> ApplyQuery<T>(this IQueryable<T> queryable, IQuery<T> query, bool applySkipTake)
            where T : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.Filters != null)
            {
                queryable = query.Filters.Aggregate(queryable,
                    (current, filter) => current.Where(QueryTranslator<T>.ConvertCriterion(filter)));
            }

            queryable = ApplySortCriteria(queryable, query.SortFields);

            if (applySkipTake)
            {
                if (query.Offset > 0) queryable = queryable.Skip(query.Offset);
                if (query.Limit > 0) queryable = queryable.Take(query.Limit);
            }

            return queryable;
        }

        /// <summary>   Applies the sort criteria. </summary>
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="sequence">     The sequence. </param>
        /// <param name="sortFields">   The sort fields. </param>
        /// <returns>   IQueryable{`0}. </returns>
        public static IQueryable<T> ApplySortCriteria<T>(this IQueryable<T> sequence, SortFieldDataCollection sortFields) where T : class
        {
            if (sortFields != null && sortFields.Count > 0)
            {
                var sortCriteria = sortFields[0];
                var orderedSequence = sequence.OrderBy(sortCriteria.PropertyName,
                    sortCriteria.SortOrder.GetValueOrDefault() ==
                    SortOrderData.Descending);

                if (sortFields.Count > 1)
                {
                    for (var i = 1; i < sortFields.Count; i++)
                    {
                        sortCriteria = sortFields[i];
                        orderedSequence = orderedSequence.ThenBy(sortCriteria.PropertyName,
                            sortCriteria.SortOrder.GetValueOrDefault() ==
                            SortOrderData.Descending);
                    }
                }

                sequence = orderedSequence;
            }
            else
            {
                sequence = sequence.OrderBy(x => true);
            }

            return sequence;
        }

        /// <summary>   Copies the query. </summary>
        /// <typeparam name="TSource">      . </typeparam>
        /// <typeparam name="TDestination"> The type of the T destination. </typeparam>
        /// <param name="sourceQuery">  The source query. </param>
        /// <param name="mappings">     (Optional) The mappings. </param>
        /// <returns>   Query{``0}. </returns>
        public static IQuery<TDestination> Copy<TSource, TDestination>(this IQuery<TSource> sourceQuery, Dictionary<string, string> mappings = null)
            where TDestination : class where TSource : class
        {
            var query = new Query<TDestination> { Offset = sourceQuery.Offset, Limit = sourceQuery.Limit };
            if (sourceQuery.Condition != null) query.Condition = new ExpressionUpdater<TSource, TDestination>().GetUpdatedExpression(sourceQuery.Condition);
            foreach (var filter in sourceQuery.Filters)
            {
                var f = filter.Copy();
                f.PropertyName = sourceQuery.ApplyMapping(f.PropertyName, mappings);
                query.Filter(f);
            }

            foreach (var filter in sourceQuery.Filters)
            {
                var f = filter.Copy();
                f.PropertyName = sourceQuery.ApplyMapping(f.PropertyName, mappings);
                query.Filter(f);
            }

            foreach (var criteria in sourceQuery.SortFields)
            {
                var c = criteria.Copy();
                c.PropertyName = sourceQuery.ApplyMapping(c.PropertyName, mappings);
                query.SortCriteria(c);
            }

            return query;
        }

        /// <summary>   Filters the specified filter. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <param name="query">            The query. </param>
        /// <param name="criteriaFilter">   The filter. </param>
        /// <returns>   An IQuery. </returns>
        public static IQuery Filter(this IQuery query, CriteriaData criteriaFilter)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.Filters.Add(criteriaFilter);
            return query;
        }

        /// <summary>   Filters the specified criteria filter. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <typeparam name="T">    . </typeparam>
        /// <param name="query">            The query. </param>
        /// <param name="criteriaFilter">   The criteria filter. </param>
        /// <returns>   An IQuery&lt;T&gt; </returns>
        public static IQuery<T> Filter<T>(this IQuery<T> query, CriteriaData criteriaFilter) where T : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.Filters.Add(criteriaFilter);
            return query;
        }

        /// <summary>   Takes the specified take. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <param name="query">    The query. </param>
        /// <param name="limit">    The limit. </param>
        /// <returns>   Query{`0}. </returns>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "1#")]
        public static IQuery Limit(this IQuery query, int limit)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.Limit = limit;
            return query;
        }

        /// <summary>   Limits the specified limit. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <typeparam name="T">    . </typeparam>
        /// <param name="query">    The query. </param>
        /// <param name="limit">    The limit. </param>
        /// <returns>   IQuery&lt;T&gt;. </returns>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "1#")]
        public static IQuery<T> Limit<T>(this IQuery<T> query, int limit) where T : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.Limit = limit;
            return query;
        }

        /// <summary>   Skips the specified skip. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <param name="query">    The query. </param>
        /// <param name="offset">   The offset. </param>
        /// <returns>   Query{`0}. </returns>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "1#")]
        public static IQuery Offset(this IQuery query, int offset)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.Offset = offset;
            return query;
        }

        /// <summary>   Offsets the specified offset. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <typeparam name="T">    . </typeparam>
        /// <param name="query">    The query. </param>
        /// <param name="offset">   The offset. </param>
        /// <returns>   IQuery&lt;T&gt;. </returns>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "1#")]
        public static IQuery<T> Offset<T>(this IQuery<T> query, int offset) where T : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.Offset = offset;
            return query;
        }

        /// <summary>   Ordered Queryable. </summary>
        /// <typeparam name="T">    The type of the entity. </typeparam>
        /// <param name="source">           The source. </param>
        /// <param name="orderByProperty">  The order by property. </param>
        /// <param name="desc">             if set to <c>true</c> [desc]. </param>
        /// <returns>   IOrderedQueryable{``0}. </returns>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string orderByProperty, bool desc)
            where T : class
        {
            var command = desc ? "OrderByDescending" : "OrderBy";
            var type = typeof(T);
            var property = type.GetProperty(orderByProperty,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                BindingFlags.IgnoreCase);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), command,
                new[] { type, property.PropertyType },
                source.Expression,
                Expression.Quote(orderByExpression));
            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(resultExpression);
        }

        /// <summary>   Sorts the criteria. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <param name="query">            The query. </param>
        /// <param name="sortFieldData">    The sort field data. </param>
        /// <returns>   IQuery. </returns>
        public static IQuery SortCriteria(this IQuery query, SortFieldData sortFieldData)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.SortFields.Add(sortFieldData);
            return query;
        }

        /// <summary>   Sorts the criteria. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <typeparam name="T">    . </typeparam>
        /// <param name="query">            The query. </param>
        /// <param name="sortFieldData">    The sort field data. </param>
        /// <returns>   IQuery&lt;T&gt;. </returns>
        public static IQuery<T> SortCriteria<T>(this IQuery<T> query, SortFieldData sortFieldData) where T : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            query.SortFields.Add(sortFieldData);
            return query;
        }

        /// <summary>   Ordered Queryable. </summary>
        /// <typeparam name="T">    The type of the T entity. </typeparam>
        /// <param name="source">           The source. </param>
        /// <param name="orderByProperty">  The order by property. </param>
        /// <param name="desc">             if set to <c>true</c> [desc]. </param>
        /// <returns>   IOrderedQueryable{``0}. </returns>
        public static IOrderedQueryable<T> ThenBy<T>(this IQueryable<T> source, string orderByProperty, bool desc)
            where T : class
        {
            var command = desc ? "ThenByDescending" : "ThenBy";
            var type = typeof(T);
            var property = type.GetProperty(orderByProperty,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                BindingFlags.IgnoreCase);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), command,
                new[] { type, property.PropertyType },
                source.Expression,
                Expression.Quote(orderByExpression));
            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(resultExpression);
        }

        /// <summary>   Queries to URL args. </summary>
        /// <param name="query">    The query. </param>
        /// <returns>   System.String. </returns>
        public static string ToQueryString(this IQuery query)
        {
            var qs = new List<KeyValuePair<string, string>>();
            if (query.Filters != null && query.Filters.Count > 0)
            {
                foreach (var filter in query.Filters)
                {
                    string valueString = null;
                    if (filter.Values != null && filter.Values.Count > 0)
                    {
                        var values = filter.Values.ToArray();
                        valueString = string.Join("|", values);
                    }

                    var operation = filter.Operation;
                    if (CriteriaData.OperationRequiresValue(operation) && (filter.Values == null || filter.Values.Count == 0)) continue;
                    if (operation == OperationData.Equal)
                    {
                        qs.Add(new KeyValuePair<string, string>(filter.PropertyName, valueString));
                    }
                    else
                    {
                        var opStr = CriteriaData.OperationToString(operation);
                        if (opStr != null) qs.Add(new KeyValuePair<string, string>(filter.PropertyName, $"{opStr}:{valueString}"));
                    }
                }
            }

            if (query.SortFields != null && query.SortFields.Count > 0)
            {
                if (query.SortFields.Count > 0)
                {
                    var first = true;
                    var s = string.Empty;
                    foreach (var sortField in query.SortFields)
                    {
                        if (!first) s += ",";
                        s += sortField.PropertyName
                             + (sortField.SortOrder.GetValueOrDefault() == SortOrderData.Ascending
                                 ? ":Ascending"
                                 : ":Descending");
                        first = false;
                    }

                    qs.Add(new KeyValuePair<string, string>("OrderBy", s));
                }
            }

            if (query.Offset > 0) qs.Add(new KeyValuePair<string, string>("Offset", query.Offset.ToString(CultureInfo.InvariantCulture)));
            if (query.Limit > 0) qs.Add(new KeyValuePair<string, string>("Limit", query.Limit.ToString(CultureInfo.InvariantCulture)));
            var result = string.Empty;
            foreach (var item in qs)
            {
                if (item.Value != null)
                    result += (string.IsNullOrEmpty(result) ? string.Empty : "&") + item.Key + "=" + Uri.EscapeDataString(item.Value);
            }

            return result;
        }

        /// <summary>   URL args to query. </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when one or more required arguments are
        /// null.
        /// </exception>
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="args"> The args. </param>
        /// <returns>   Query{`0}. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static IQuery<T> UrlArgsToQuery<T>(this IEnumerable<KeyValuePair<string, string>> args) where T : class
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            return (IQuery<T>)UrlArgsToQueryCommon(args, new Query<T>());
        }

        #endregion

        #region  Private Methods

        /// <summary>   URL arguments to query common. </summary>
        /// <param name="args">     The arguments. </param>
        /// <param name="query">    The query. </param>
        /// <returns>   An IQuery. </returns>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Int32.TryParse(System.String,System.Int32@)")]
        private static IQuery UrlArgsToQueryCommon(IEnumerable<KeyValuePair<string, string>> args, IQuery query)
        {
            foreach (var arg in args)
            {
                var key = arg.Key;
                var keyUpper = key.ToUpperInvariant();
                var value = arg.Value;
                switch (keyUpper)
                {
                    case "OFFSET":
                        int offset;
                        int.TryParse(value, out offset);
                        query.Offset = offset;
                        break;
                    case "LIMIT":
                        int limit;
                        int.TryParse(value, out limit);
                        query.Limit = limit;
                        break;
                    case "ORDERBY":
                        var fields = value.Split('|');
                        foreach (var field in fields)
                        {
                            var fieldComponents = field.Split(new[] { ':' }, 2);
                            if (fieldComponents.Length == 1)
                            {
                                query.SortCriteria(new SortFieldData { PropertyName = fieldComponents[0], SortOrder = SortOrderData.Ascending });
                            }
                            else
                            {
                                if (fieldComponents.Length == 2)
                                {
                                    query.SortCriteria(new SortFieldData
                                    {
                                        PropertyName = fieldComponents[0],
                                        SortOrder =
                                            fieldComponents[1].Equals("Descending",
                                                StringComparison.OrdinalIgnoreCase)
                                                ? SortOrderData.Descending
                                                : SortOrderData.Ascending
                                    });
                                }
                            }
                        }

                        break;
                    case "PHRASE":
                        query.Filter(new CriteriaData { PropertyName = key, Operation = OperationData.Equal, Values = new Collection<string> { value } });
                        break;
                    default:
                        var values = value.Split('|');
                        if (values.Any())
                        {
                            var operation = OperationData.Equal;
                            var value0 = values[0].Split(new[] { ':' }, 2);
                            if (value0.Length == 2)
                            {
                                var operationString = value0[0];
                                var op = CriteriaData.StringToOperation(operationString);
                                if (op.HasValue)
                                {
                                    operation = op.GetValueOrDefault();
                                    values[0] = value0[1];
                                }
                            }

                            query.Filter(new CriteriaData { PropertyName = key, Operation = operation, Values = new Collection<string>(values) });
                        }

                        break;
                }
            }

            return query;
        }

        #endregion

        #region Nested type: ExpressionUpdater

        /// <summary>   An expression updater. </summary>
        /// <typeparam name="TSource">      Type of the source. </typeparam>
        /// <typeparam name="TDestination"> Type of the destination. </typeparam>
        internal class ExpressionUpdater<TSource, TDestination>
        {
            #region Public Instance Methods

            /// <summary>   Gets updated expression. </summary>
            /// <param name="expression">   The expression. </param>
            /// <returns>   The updated expression. </returns>
            public Expression<Func<TDestination, bool>> GetUpdatedExpression(Expression<Func<TSource, bool>> expression)
            {
                //parameter that will be used in generated expression
                var param = Expression.Parameter(typeof(TDestination));
                //visiting body of original expression that gives us body of the new expression
                var body = new CustomExpressionVisitor<TDestination>(param).Visit(expression.Body);
                //generating lambda expression form body and parameter 
                //notice that this is what you need to invoke the Method_2
                var lambda = Expression.Lambda<Func<TDestination, bool>>(body, param);

                return lambda;
            }

            #endregion

            #region Nested type: CustomExpressionVisitor

            /// <summary>   A custom expression visitor. </summary>
            /// <typeparam name="T">    Generic type parameter. </typeparam>
            private class CustomExpressionVisitor<T> : ExpressionVisitor
            {
                #region Fields

                /// <summary>   The parameter. </summary>
                private readonly ParameterExpression _parameter;

                #endregion

                #region Constructors

                /// <summary>   Constructor. </summary>
                /// <param name="parameter">    The parameter. </param>
                public CustomExpressionVisitor(ParameterExpression parameter)
                {
                    _parameter = parameter;
                }

                #endregion

                /// <summary>
                /// Visits the <see cref="T:System.Linq.Expressions.ParameterExpression"></see>.
                /// </summary>
                /// <param name="node"> The expression to visit. </param>
                /// <returns>
                /// The modified expression, if it or any subexpression was modified; otherwise, returns the
                /// original expression.
                /// </returns>
                protected override Expression VisitParameter(ParameterExpression node)
                {
                    return _parameter;
                }

                /// <summary>
                /// Visits the children of the <see cref="T:System.Linq.Expressions.MemberExpression"></see>.
                /// </summary>
                /// <param name="node"> The expression to visit. </param>
                /// <returns>
                /// The modified expression, if it or any subexpression was modified; otherwise, returns the
                /// original expression.
                /// </returns>
                protected override Expression VisitMember(MemberExpression node)
                {
                    if (node.Member.MemberType == MemberTypes.Property)
                    {
                        var memberName = node.Member.Name;
                        var otherMember = typeof(T).GetProperty(memberName);
                        var memberExpression = Expression.Property(Visit(node.Expression), otherMember);
                        return memberExpression;
                    }

                    return base.VisitMember(node);
                }
            }

            #endregion
        }

        #endregion

        #region Nested type: ReplaceExpressionVisitor

        /// <summary>   A replace expression visitor. </summary>
        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            #region Fields

            /// <summary>   The new value. </summary>
            private readonly Expression _newValue;

            /// <summary>   The old value. </summary>
            private readonly Expression _oldValue;

            #endregion

            #region Constructors

            /// <summary>   Constructor. </summary>
            /// <param name="oldValue"> The old value. </param>
            /// <param name="newValue"> The new value. </param>
            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            #endregion

            #region Public Instance Methods

            /// <summary>
            /// Dispatches the expression to one of the more specialized visit methods in this class.
            /// </summary>
            /// <param name="node"> The expression to visit. </param>
            /// <returns>
            /// The modified expression, if it or any subexpression was modified; otherwise, returns the
            /// original expression.
            /// </returns>
            public override Expression Visit(Expression node)
            {
                return node == _oldValue ? _newValue : base.Visit(node);
            }

            #endregion
        }

        #endregion
    }
}