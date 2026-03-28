using Orivy.Controls;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Orivy.Binding;

public static class InteractionExtensions
{
    public static TTarget When<TTarget>(this TTarget target, string eventName, Action<TTarget> handler)
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        AttachHandler(target, eventName, () => handler(target));
        return target;
    }

    public static TTarget When<TTarget, TSource>(this TTarget target, string eventName, TSource source, Action<TSource, TTarget> handler)
        where TTarget : class
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(handler);

        AttachHandler(target, eventName, () => handler(source, target));
        return target;
    }

    public static TTarget When<TTarget, TSource>(this TTarget target, string eventName, Action<TSource, TTarget> handler)
        where TTarget : ElementBase
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        AttachHandler(target, eventName, () => handler(ResolveRequiredDataContext<TSource>(target), target));
        return target;
    }

    public static ElementBase When<TSource>(this ElementBase target, string eventName, Action<TSource, ElementBase> handler)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handler);

        AttachHandler(target, eventName, () => handler(ResolveRequiredDataContext<TSource>(target), target));
        return target;
    }

    private static void AttachHandler<TTarget>(TTarget target, string eventName, Action callback)
        where TTarget : class
    {
        var eventInfo = target.GetType().GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"Type '{target.GetType().Name}' does not expose an event named '{eventName}'.");

        if (eventInfo.EventHandlerType == null)
            throw new InvalidOperationException($"Event '{eventName}' on '{target.GetType().Name}' does not expose a handler type.");

        var proxy = CreateEventProxy(eventInfo.EventHandlerType, callback);
        eventInfo.AddEventHandler(target, proxy);
    }

    private static TSource ResolveRequiredDataContext<TSource>(ElementBase element)
        where TSource : class
    {
        if (element.DataContext is TSource source)
            return source;

        throw new InvalidOperationException($"Element '{element.Name}' requires a DataContext of type '{typeof(TSource).Name}' for this interaction.");
    }

    private static Delegate CreateEventProxy(Type eventHandlerType, Action callback)
    {
        var invokeMethod = eventHandlerType.GetMethod("Invoke")
            ?? throw new InvalidOperationException($"Delegate type '{eventHandlerType.Name}' does not expose an Invoke method.");

        if (invokeMethod.ReturnType != typeof(void))
            throw new InvalidOperationException($"Delegate type '{eventHandlerType.Name}' must return void to be used with When(...).");

        var parameters = invokeMethod.GetParameters();
        var lambdaParameters = new ParameterExpression[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            lambdaParameters[i] = Expression.Parameter(parameters[i].ParameterType, parameters[i].Name);

        var body = Expression.Call(
            Expression.Constant(callback),
            typeof(Action).GetMethod(nameof(Action.Invoke))!);

        return Expression.Lambda(eventHandlerType, body, lambdaParameters).Compile();
    }
}