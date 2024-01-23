using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace AsyncEnumerableQuery;

public static class AsyncEnumerableQuery {
    private static readonly Func<Type, Expression, IAsyncEnumerableQuery> AsyncEnumerableQueryFactory;
    
    static AsyncEnumerableQuery() {
        var createQueryMethod = typeof(AsyncEnumerableQuery)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(m => m is { Name: nameof(CreateQueryByExpression), IsGenericMethod: true }) ?? throw new InvalidOperationException();
        AsyncEnumerableQueryFactory = (resultType, queryable) => (IAsyncEnumerableQuery)createQueryMethod.MakeGenericMethod(resultType).Invoke(null, [queryable])!;
    }
    
    public static IQueryable<TElement> AsAsyncQueryable<TElement>(this IEnumerable<TElement> source) 
        => CreateQuery(source.AsQueryable());

    public static IQueryable AsAsyncQueryable(this IEnumerable source) 
        => CreateQuery(source.AsQueryable());
    
    private static AsyncEnumerableQuery<TResult> CreateQueryByExpression<TResult>(Expression expression) => new(new EnumerableQuery<TResult>(expression));
    
    internal static IAsyncEnumerableQuery CreateQuery(Type resultType, Expression expression) => AsyncEnumerableQueryFactory.Invoke(resultType, expression);
    internal static IAsyncEnumerableQuery CreateQuery(IQueryable queryable) {
        var resultType = queryable.GetType().GetGenericArguments()[0];
        return CreateQuery(resultType, queryable.Expression);
    }

    internal static AsyncEnumerableQuery<TSource> CreateQuery<TSource>(IQueryable<TSource> queryable) {
        return new AsyncEnumerableQuery<TSource>((EnumerableQuery<TSource>)queryable);
    }
}

internal interface IAsyncEnumerableQuery : IOrderedQueryable, IAsyncQueryProvider, IListSource {
    TResult ExecuteSourceAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
}

internal sealed class AsyncEnumeratorWrapper<TSource> : IAsyncEnumerator<TSource> {
    private readonly IEnumerator<TSource> _enumerator;
    public TSource Current => _enumerator.Current;

    public AsyncEnumeratorWrapper(IEnumerator<TSource> enumerator) {
        _enumerator = enumerator ?? throw new ArgumentNullException();
    }
        
    public ValueTask<bool> MoveNextAsync() => new(_enumerator.MoveNext());
        
    public ValueTask DisposeAsync() {
        _enumerator.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class AsyncEnumerableQuery<TSource> : IOrderedQueryable<TSource>, IAsyncEnumerable<TSource>, IAsyncEnumerableQuery {
    private readonly EnumerableQuery<TSource> _source;
    
    public Type ElementType => typeof(TSource);
    public Expression Expression => ((IQueryable)_source).Expression;
    public IQueryProvider Provider => this;
    
    public AsyncEnumerableQuery(EnumerableQuery<TSource> source) {
        _source = source;
    }
    
    public AsyncEnumerableQuery(Expression expression) : this(new EnumerableQuery<TSource>(expression)) { }

    public IQueryable CreateQuery(Expression expression) {
        IQueryable source = ((IQueryProvider)_source).CreateQuery(expression);
        return AsyncEnumerableQuery.CreateQuery(source);
    }

    public IQueryable<TEntity> CreateQuery<TEntity>(Expression expression) {
        var source = ((IQueryProvider)_source).CreateQuery<TEntity>(expression);
        return AsyncEnumerableQuery.CreateQuery(source);
    }

    public object? Execute(Expression expression) => ((IQueryProvider)_source).Execute(expression);

    public TResult Execute<TResult>(Expression expression) => ((IQueryProvider)_source).Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default) {
        // TResult = Task<bool> | Task<int> | Task<TEntity>
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        return AsyncEnumerableQuery.CreateQuery(expectedResultType, expression).ExecuteSourceAsync<TResult>(expression, cancellationToken);
    }

    public TResult ExecuteSourceAsync<TResult>(Expression expression, CancellationToken cancellationToken = default) {
        if (typeof(Task<TSource>).IsAssignableTo(typeof(TResult))) {
            var result = Task.FromResult(Execute<TSource>(expression));
            if (result is TResult cResult) return cResult;
        }
        if (typeof(ValueTask<TSource>).IsAssignableTo(typeof(TResult))) {
            var result = ValueTask.FromResult(Execute<TSource>(expression));
            if (result is TResult cResult) return cResult;
        }
        throw new NotSupportedException($"Async result of type '{typeof(TResult)}' is not supported");
    }

    public IEnumerator<TSource> GetEnumerator() => Execute<IEnumerable<TSource>>(Expression).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Execute<IEnumerable>(Expression).GetEnumerator();
    public IAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new AsyncEnumeratorWrapper<TSource>(GetEnumerator());
    IList IListSource.GetList() => throw new NotSupportedException();
    bool IListSource.ContainsListCollection => false;
}