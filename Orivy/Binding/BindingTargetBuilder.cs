using Orivy.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Orivy.Binding;

public sealed class BindingTargetBuilder<TTarget, TValue>
    where TTarget : ElementBase
{
    private readonly TTarget _target;
    private readonly Expression<Func<TTarget, TValue>> _targetProperty;

    internal BindingTargetBuilder(TTarget target, Expression<Func<TTarget, TValue>> targetProperty)
    {
        _target = target;
        _targetProperty = targetProperty;
    }

    public BindingSourceBuilder<TTarget, TSource, TValue, TSourceValue> From<TSource, TSourceValue>(TSource source, Expression<Func<TSource, TSourceValue>> sourceProperty)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceProperty);
        return new BindingSourceBuilder<TTarget, TSource, TValue, TSourceValue>(_target, _targetProperty, source, sourceProperty, usesDataContext: false);
    }

    public BindingSourceBuilder<TTarget, TSource, TValue, TSourceValue> FromData<TSource, TSourceValue>(Expression<Func<TSource, TSourceValue>> sourceProperty)
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(sourceProperty);
        return new BindingSourceBuilder<TTarget, TSource, TValue, TSourceValue>(_target, _targetProperty, null, sourceProperty, usesDataContext: true);
    }

    public BindingHandle FromData<TSource, TSourceCollection, TSourceItem>(
        Expression<Func<TSource, TSourceCollection>> sourceProperty,
        Func<TSourceItem, object> itemFactory)
        where TSource : class
        where TSourceCollection : IEnumerable<TSourceItem>
    {
        ArgumentNullException.ThrowIfNull(sourceProperty);
        ArgumentNullException.ThrowIfNull(itemFactory);
        return BindingExtensions.CreateDataContextPropertyBinding(
            _target,
            _targetProperty,
            sourceProperty,
            BindingMode.OneWay,
            (object? sourceValue) => sourceValue is IEnumerable<TSourceItem> enumerable
                ? ReplaceCollectionItems(enumerable, itemFactory)
                : null);
    }

    private static object? ReplaceCollectionItems<TSourceItem>(IEnumerable<TSourceItem> sourceValues, Func<TSourceItem, object> itemFactory)
    {
        var result = new System.Collections.ArrayList();
        foreach (var sourceItem in sourceValues)
            result.Add(itemFactory(sourceItem));
        return result;
    }
}

public sealed class BindingSourceBuilder<TTarget, TSource, TTargetValue, TSourceValue>
    where TTarget : ElementBase
    where TSource : class
{
    private readonly TTarget _target;
    private readonly Expression<Func<TTarget, TTargetValue>> _targetProperty;
    private readonly TSource? _source;
    private readonly Expression<Func<TSource, TSourceValue>> _sourceProperty;
    private readonly bool _usesDataContext;
    private readonly Func<object?, object?>? _collectionItemConverter;

    internal BindingSourceBuilder(
        TTarget target,
        Expression<Func<TTarget, TTargetValue>> targetProperty,
        TSource? source,
        Expression<Func<TSource, TSourceValue>> sourceProperty,
        bool usesDataContext,
        Func<object?, object?>? collectionItemConverter = null)
    {
        _target = target;
        _targetProperty = targetProperty;
        _source = source;
        _sourceProperty = sourceProperty;
        _usesDataContext = usesDataContext;
        _collectionItemConverter = collectionItemConverter;
    }

    public BindingHandle OneWay()
    {
        return Build(BindingMode.OneWay);
    }

    public BindingHandle TwoWay()
    {
        return Build(BindingMode.TwoWay);
    }

    private BindingHandle Build(BindingMode mode)
    {
        if (_usesDataContext)
            return BindingExtensions.CreateDataContextPropertyBinding(_target, _targetProperty, _sourceProperty, mode, _collectionItemConverter);

        if (_source == null)
            throw new InvalidOperationException("Explicit source binding requires a non-null source instance.");

        return BindingExtensions.CreatePropertyBinding(_target, _targetProperty, _source, _sourceProperty, mode, _collectionItemConverter);
    }
}