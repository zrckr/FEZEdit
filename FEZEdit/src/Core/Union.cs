using System;

namespace FEZEdit.Core;

public abstract class Union<T1, T2>
{
    private Union() { }
    
    public abstract TResult Match<TResult>(
        Func<T1, TResult> case1,
        Func<T2, TResult> case2);
    
    public abstract void Match(
        Action<T1> case1,
        Action<T2> case2);

    public void Match<TContext>(TContext context, Action<T1, TContext> case1, Action<T2, TContext> case2)
    {
        Match(
            v1 =>
            {
                case1(v1, context);
                return 0;
            },
            v2 =>
            {
                case2(v2, context);
                return 0;
            });
    }
    
    private sealed class Case1(T1 value) : Union<T1, T2>
    {
        public override TResult Match<TResult>(
            Func<T1, TResult> case1,
            Func<T2, TResult> case2) => case1(value);
        
        public override void Match(
            Action<T1> case1,
            Action<T2> case2) => case1(value);
    }
    
    private sealed class Case2(T2 value) : Union<T1, T2>
    {
        public override TResult Match<TResult>(
            Func<T1, TResult> case1,
            Func<T2, TResult> case2) => case2(value);
        
        public override void Match(
            Action<T1> case1,
            Action<T2> case2) => case2(value);
    }
    
    public static implicit operator Union<T1, T2>(T1 value) => new Case1(value);
    public static implicit operator Union<T1, T2>(T2 value) => new Case2(value);
}