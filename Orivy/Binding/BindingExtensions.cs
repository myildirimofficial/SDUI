using Orivy.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Orivy.Binding;

public static class BindingExtensions
{
    public static BindingTargetBuilder<TTarget, TValue> Link<TTarget, TValue>(
        this TTarget target,
        Expression<Func<TTarget, TValue>> targetProperty)
        where TTarget : ElementBase
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetProperty);
        return new BindingTargetBuilder<TTarget, TValue>(target, targetProperty);
    }

    internal static BindingHandle CreatePropertyBinding<TTarget, TSource, TTargetValue, TSourceValue>(
        TTarget target,
        Expression<Func<TTarget, TTargetValue>> targetProperty,
        TSource source,
        Expression<Func<TSource, TSourceValue>> sourceProperty,
        BindingMode mode = BindingMode.OneWay,
        Func<object?, object?>? collectionItemConverter = null)
        where TTarget : class
        where TSource : class
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetProperty);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceProperty);

        var targetAccessor = BindingMemberAccessor<TTarget, TTargetValue>.Create(targetProperty);
        var sourceAccessor = BindingMemberAccessor<TSource, TSourceValue>.Create(sourceProperty);

        var targetSetter = CreateTargetSetter<TTarget, TTargetValue, TSourceValue>(targetAccessor, collectionItemConverter);
        var targetGetter = mode == BindingMode.TwoWay
            ? CreateTargetGetter<TTarget, TTargetValue, TSourceValue>(targetAccessor)
            : null;

        var targetEventHooks = mode == BindingMode.TwoWay ? CreateEventHooks<TTarget>(targetAccessor.PropertyName) : null;
        var sourceEventHooks = CreateEventHooks<TSource>(sourceAccessor.PropertyName);

        var handle = CreateBinding(
            target,
            source,
            sourceProperty,
            targetGetter,
            targetSetter,
            targetEventHooks?.Subscribe,
            targetEventHooks?.Unsubscribe,
            sourceEventHooks?.Subscribe,
            sourceEventHooks?.Unsubscribe,
            mode);

        return TrackIfPossible(target, handle);
    }

    internal static BindingHandle CreateDataContextPropertyBinding<TTarget, TSource, TTargetValue, TSourceValue>(
        TTarget target,
        Expression<Func<TTarget, TTargetValue>> targetProperty,
        Expression<Func<TSource, TSourceValue>> sourceProperty,
        BindingMode mode = BindingMode.OneWay,
        Func<object?, object?>? collectionItemConverter = null)
        where TTarget : ElementBase
        where TSource : class
    {
        BindingHandle? currentBinding = null;

        void Rebind()
        {
            currentBinding?.Dispose();
            currentBinding = null;

            if (target.DataContext is TSource source)
                currentBinding = CreatePropertyBinding(target, targetProperty, source, sourceProperty, mode, collectionItemConverter);
        }

        EventHandler dataContextChanged = (_, _) => Rebind();
        target.DataContextChanged += dataContextChanged;
        Rebind();

        return target.TrackBinding(new BindingHandle(() =>
        {
            target.DataContextChanged -= dataContextChanged;
            currentBinding?.Dispose();
        }));
    }

    public static TElement SetDataContext<TElement>(this TElement element, object? dataContext)
        where TElement : ElementBase
    {
        ArgumentNullException.ThrowIfNull(element);
        element.DataContext = dataContext;
        return element;
    }

    private static BindingHandle BindToDataContext<TControl, TSource, TTargetValue, TSourceValue>(
        TControl control,
        Expression<Func<TSource, TSourceValue>> sourceProperty,
        Func<TControl, TSourceValue>? targetGetter,
        Action<TControl, TSourceValue> targetSetter,
        Action<TControl, EventHandler>? subscribeTargetChanged = null,
        Action<TControl, EventHandler>? unsubscribeTargetChanged = null,
        BindingMode mode = BindingMode.OneWay)
        where TControl : ElementBase
        where TSource : class
    {
        BindingHandle? currentBinding = null;

        void Rebind()
        {
            currentBinding?.Dispose();
            currentBinding = null;

            if (control.DataContext is TSource source)
                currentBinding = CreateBinding(control, source, sourceProperty, targetGetter, targetSetter, subscribeTargetChanged, unsubscribeTargetChanged, mode);
        }

        EventHandler dataContextChanged = (_, _) => Rebind();
        control.DataContextChanged += dataContextChanged;
        Rebind();

        return control.TrackBinding(new BindingHandle(() =>
        {
            control.DataContextChanged -= dataContextChanged;
            currentBinding?.Dispose();
        }));
    }

    private static BindingHandle CreateBinding<TControl, TSource, TValue>(
        TControl control,
        TSource source,
        Expression<Func<TSource, TValue>> sourceProperty,
        Func<TControl, TValue>? targetGetter,
        Action<TControl, TValue> targetSetter,
        Action<TControl, EventHandler>? subscribeTargetChanged,
        Action<TControl, EventHandler>? unsubscribeTargetChanged,
        Action<TSource, EventHandler>? subscribeSourceChanged,
        Action<TSource, EventHandler>? unsubscribeSourceChanged,
        BindingMode mode)
        where TControl : class
        where TSource : class
    {
        var binding = new PropertyBinding<TControl, TSource, TValue>(control, source, sourceProperty, targetGetter, targetSetter, subscribeTargetChanged, unsubscribeTargetChanged, subscribeSourceChanged, unsubscribeSourceChanged, mode);
        return new BindingHandle(binding.Dispose);
    }

    private static BindingHandle CreateBinding<TControl, TSource, TValue>(
        TControl control,
        TSource source,
        Expression<Func<TSource, TValue>> sourceProperty,
        Func<TControl, TValue>? targetGetter,
        Action<TControl, TValue> targetSetter,
        Action<TControl, EventHandler>? subscribeTargetChanged,
        Action<TControl, EventHandler>? unsubscribeTargetChanged,
        BindingMode mode)
        where TControl : class
        where TSource : class
    {
        return CreateBinding(control, source, sourceProperty, targetGetter, targetSetter, subscribeTargetChanged, unsubscribeTargetChanged, null, null, mode);
    }

    private static BindingHandle TrackIfPossible<TTarget>(TTarget target, BindingHandle handle)
        where TTarget : class
    {
        if (target is ElementBase element)
            return element.TrackBinding(handle);

        return handle;
    }

    private static EventHooks<TObject>? CreateEventHooks<TObject>(string propertyName)
        where TObject : class
    {
        var candidates = new[]
        {
            propertyName + "Changed",
            propertyName + "ValueChanged"
        };

        for (var i = 0; i < candidates.Length; i++)
        {
            var eventInfo = typeof(TObject).GetEvent(candidates[i], BindingFlags.Instance | BindingFlags.Public);
            if (eventInfo == null || eventInfo.EventHandlerType == null)
                continue;

            Delegate? proxy = null;
            return new EventHooks<TObject>(
                (obj, handler) =>
                {
                    proxy = CreateEventProxy(eventInfo.EventHandlerType, handler);
                    eventInfo.AddEventHandler(obj, proxy);
                },
                (obj, _) =>
                {
                    if (proxy == null)
                        return;

                    eventInfo.RemoveEventHandler(obj, proxy);
                    proxy = null;
                });
        }

        return null;
    }

    private static Action<TTarget, TSourceValue> CreateTargetSetter<TTarget, TTargetValue, TSourceValue>(
        BindingMemberAccessor<TTarget, TTargetValue> targetAccessor,
        Func<object?, object?>? collectionItemConverter = null)
        where TTarget : class
    {
        if (targetAccessor.Setter != null)
            return (target, value) => targetAccessor.Setter(target, ConvertValue<TTargetValue>(value));

        if (!typeof(IEnumerable).IsAssignableFrom(typeof(TSourceValue)))
            throw new InvalidOperationException($"Property '{targetAccessor.PropertyName}' on '{typeof(TTarget).Name}' is read-only and cannot be a binding target.");

        return (target, value) => ReplaceTargetCollection(targetAccessor.Getter(target), value as IEnumerable, collectionItemConverter);
    }

    private static Func<TTarget, TSourceValue>? CreateTargetGetter<TTarget, TTargetValue, TSourceValue>(BindingMemberAccessor<TTarget, TTargetValue> targetAccessor)
        where TTarget : class
    {
        if (typeof(IEnumerable).IsAssignableFrom(typeof(TTargetValue)) && typeof(IEnumerable).IsAssignableFrom(typeof(TSourceValue)))
            return null;

        return target => ConvertValue<TSourceValue>(targetAccessor.Getter(target));
    }

    private static void ReplaceTargetCollection(object? targetCollection, IEnumerable? values, Func<object?, object?>? itemConverter)
    {
        if (targetCollection is not IList list)
            throw new InvalidOperationException("Automatic collection binding requires the target collection to implement IList.");

        list.Clear();
        if (values == null)
            return;

        var listElementType = GetCollectionElementType(list);

        foreach (var value in values)
        {
            object? item;

            if (itemConverter != null)
            {
                item = itemConverter(value);
            }
            else if (list is GridListItemCollection)
            {
                item = ConvertToGridListItem(value);
            }
            else
            {
                item = ConvertCollectionItem(value, listElementType);
            }

            if (item == null)
                throw new InvalidOperationException("Null item mapped for target collection; converter must not return null.");

            list.Add(item);
        }
    }

    private static object? ConvertCollectionItem(object? value, Type? targetElementType)
    {
        if (value == null)
            return null;

        if (targetElementType == null || targetElementType.IsInstanceOfType(value))
            return value;

        if (targetElementType == typeof(GridListItem))
            return ConvertToGridListItem(value);

        try
        {
            // Support primitive conversions where possible.
            return Convert.ChangeType(value, targetElementType);
        }
        catch
        {
            throw new InvalidOperationException($"Collection binding cannot convert item of type '{value.GetType().Name}' to '{targetElementType.Name}'. Provide an explicit converter for this path.");
        }
    }

    private static Type? GetCollectionElementType(IList list)
    {
        var collectionType = list.GetType();

        Type? elementType = collectionType.IsGenericType
            ? collectionType.GetGenericArguments().FirstOrDefault()
            : null;

        if (elementType != null)
            return elementType;

        var collectionInterface = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));

        return collectionInterface?.GetGenericArguments().FirstOrDefault();
    }

    private static GridListItem ConvertToGridListItem(object model)
    {
        if (model is GridListItem existingItem)
            return existingItem;

        // Allow types to provide explicit conversion path via ToGridListItem() helper.
        var toGridListItemMethod = model.GetType().GetMethod("ToGridListItem", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (toGridListItemMethod != null && toGridListItemMethod.ReturnType == typeof(GridListItem))
            return (GridListItem)toGridListItemMethod.Invoke(model, null)!;

        var row = new GridListItem { Tag = model };

        var properties = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.Length == 0)
        {
            row.Cells.Add(new GridListCell { Text = model.ToString() ?? string.Empty });
            return row;
        }

        foreach (var prop in properties)
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            var value = prop.GetValue(model);
            row.Cells.Add(new GridListCell { Text = value?.ToString() ?? string.Empty });
        }

        return row;
    }

    private static TValue ConvertValue<TValue>(object? value)
    {
        if (value is TValue typedValue)
            return typedValue;

        if (value == null)
        {
            var destinationType = typeof(TValue);
            if (!destinationType.IsValueType || Nullable.GetUnderlyingType(destinationType) != null)
                return default!;
        }

        return (TValue)value!;
    }

    private static Delegate CreateEventProxy(Type eventHandlerType, EventHandler handler)
    {
        var invokeMethod = eventHandlerType.GetMethod("Invoke")
            ?? throw new InvalidOperationException($"Delegate type '{eventHandlerType.Name}' does not expose an Invoke method.");

        if (invokeMethod.ReturnType != typeof(void))
            throw new InvalidOperationException($"Delegate type '{eventHandlerType.Name}' must return void to be used for automatic binding.");

        var parameters = invokeMethod.GetParameters();
        var lambdaParameters = new ParameterExpression[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            lambdaParameters[i] = Expression.Parameter(parameters[i].ParameterType, parameters[i].Name);

        Expression senderExpression = lambdaParameters.Length > 0
            ? Expression.Convert(lambdaParameters[0], typeof(object))
            : Expression.Constant(null, typeof(object));

        var body = Expression.Call(
            Expression.Constant(handler),
            typeof(EventHandler).GetMethod(nameof(EventHandler.Invoke))!,
            senderExpression,
            Expression.Constant(EventArgs.Empty));

        return Expression.Lambda(eventHandlerType, body, lambdaParameters).Compile();
    }
}

internal sealed class PropertyBinding<TControl, TSource, TValue> : IDisposable
    where TControl : class
    where TSource : class
{
    private readonly TControl _control;
    private readonly TSource _source;
    private readonly BindingMemberAccessor<TSource, TValue> _sourceAccessor;
    private readonly Func<TControl, TValue>? _targetGetter;
    private readonly Action<TControl, TValue> _targetSetter;
    private readonly Action<TControl, EventHandler>? _unsubscribeTargetChanged;
    private readonly INotifyPropertyChanged? _notifyPropertySource;
    private readonly EventHandler? _targetChangedHandler;
    private readonly PropertyChangedEventHandler? _sourceChangedHandler;
    private readonly Action<TSource, EventHandler>? _unsubscribeSourceChanged;
    private readonly EventHandler? _sourceValueChangedHandler;
    private INotifyCollectionChanged? _collectionNotifier;
    private NotifyCollectionChangedEventHandler? _collectionChangedHandler;
    private int _updateDepth;

    public PropertyBinding(
        TControl control,
        TSource source,
        Expression<Func<TSource, TValue>> sourceProperty,
        Func<TControl, TValue>? targetGetter,
        Action<TControl, TValue> targetSetter,
        Action<TControl, EventHandler>? subscribeTargetChanged,
        Action<TControl, EventHandler>? unsubscribeTargetChanged,
        Action<TSource, EventHandler>? subscribeSourceChanged,
        Action<TSource, EventHandler>? unsubscribeSourceChanged,
        BindingMode mode)
    {
        _control = control;
        _source = source;
        _targetGetter = targetGetter;
        _targetSetter = targetSetter;
        _unsubscribeTargetChanged = unsubscribeTargetChanged;
        _unsubscribeSourceChanged = unsubscribeSourceChanged;
        _sourceAccessor = BindingMemberAccessor<TSource, TValue>.Create(sourceProperty);

        if (mode == BindingMode.TwoWay)
        {
            if (_sourceAccessor.Setter == null)
                throw new InvalidOperationException($"Property '{_sourceAccessor.PropertyName}' on '{typeof(TSource).Name}' is read-only and cannot be used in a two-way binding.");

            if (_targetGetter == null || subscribeTargetChanged == null || unsubscribeTargetChanged == null)
                throw new InvalidOperationException("Two-way binding requires a target getter and target change subscription hooks.");

            _targetChangedHandler = (_, _) => PushTargetToSource();
            subscribeTargetChanged(_control, _targetChangedHandler);
        }

        if (source is INotifyPropertyChanged notifyPropertySource)
        {
            _notifyPropertySource = notifyPropertySource;
            _sourceChangedHandler = (_, args) =>
            {
                if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == _sourceAccessor.PropertyName)
                    PushSourceToTarget();
            };

            _notifyPropertySource.PropertyChanged += _sourceChangedHandler;
        }
        else if (subscribeSourceChanged != null && unsubscribeSourceChanged != null)
        {
            _sourceValueChangedHandler = (_, _) => PushSourceToTarget();
            subscribeSourceChanged(_source, _sourceValueChangedHandler);
        }

        PushSourceToTarget();
    }

    public void Dispose()
    {
        if (_notifyPropertySource != null && _sourceChangedHandler != null)
            _notifyPropertySource.PropertyChanged -= _sourceChangedHandler;

        if (_targetChangedHandler != null && _unsubscribeTargetChanged != null)
            _unsubscribeTargetChanged(_control, _targetChangedHandler);

        if (_sourceValueChangedHandler != null && _unsubscribeSourceChanged != null)
            _unsubscribeSourceChanged(_source, _sourceValueChangedHandler);

        if (_collectionNotifier != null && _collectionChangedHandler != null)
            _collectionNotifier.CollectionChanged -= _collectionChangedHandler;
    }

    private void PushSourceToTarget()
    {
        _updateDepth++;
        try
        {
            var value = _sourceAccessor.Getter(_source);
            UpdateCollectionSubscription(value);
            _targetSetter(_control, value);
        }
        finally
        {
            _updateDepth--;
        }
    }

    private void PushTargetToSource()
    {
        if (_updateDepth > 0 || _targetGetter == null || _sourceAccessor.Setter == null)
            return;

        _updateDepth++;
        try
        {
            _sourceAccessor.Setter(_source, _targetGetter(_control));
        }
        finally
        {
            _updateDepth--;
        }
    }

    private void UpdateCollectionSubscription(TValue value)
    {
        var nextNotifier = value as INotifyCollectionChanged;
        if (ReferenceEquals(_collectionNotifier, nextNotifier))
            return;

        if (_collectionNotifier != null && _collectionChangedHandler != null)
            _collectionNotifier.CollectionChanged -= _collectionChangedHandler;

        _collectionNotifier = nextNotifier;
        if (_collectionNotifier == null)
        {
            _collectionChangedHandler = null;
            return;
        }

        _collectionChangedHandler = (_, _) => PushSourceToTarget();
        _collectionNotifier.CollectionChanged += _collectionChangedHandler;
    }
}

internal sealed class BindingMemberAccessor<TSource, TValue>
    where TSource : class
{
    private BindingMemberAccessor(string propertyName, Func<TSource, TValue> getter, Action<TSource, TValue>? setter)
    {
        PropertyName = propertyName;
        Getter = getter;
        Setter = setter;
    }

    public string PropertyName { get; }
    public Func<TSource, TValue> Getter { get; }
    public Action<TSource, TValue>? Setter { get; }

    public static BindingMemberAccessor<TSource, TValue> Create(Expression<Func<TSource, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var memberExpression = UnwrapMemberExpression(expression.Body)
            ?? throw new InvalidOperationException("Binding expressions must target a direct property or field access.");

        if (memberExpression.Expression != expression.Parameters[0])
            throw new InvalidOperationException("Only direct member access is supported in bindings. Use a computed view-model property for derived values.");

        var getter = expression.Compile();
        var setter = CreateSetter(expression.Parameters[0], memberExpression);
        return new BindingMemberAccessor<TSource, TValue>(memberExpression.Member.Name, getter, setter);
    }

    private static Action<TSource, TValue>? CreateSetter(ParameterExpression sourceParameter, MemberExpression memberExpression)
    {
        if (memberExpression.Member is PropertyInfo propertyInfo)
        {
            if (!propertyInfo.CanWrite)
                return null;

            var valueParameter = Expression.Parameter(typeof(TValue), "value");
            var assignValue = propertyInfo.PropertyType == typeof(TValue)
                ? (Expression)valueParameter
                : Expression.Convert(valueParameter, propertyInfo.PropertyType);
            var assignExpression = Expression.Assign(memberExpression, assignValue);
            return Expression.Lambda<Action<TSource, TValue>>(assignExpression, sourceParameter, valueParameter).Compile();
        }

        if (memberExpression.Member is FieldInfo fieldInfo)
        {
            var valueParameter = Expression.Parameter(typeof(TValue), "value");
            var assignValue = fieldInfo.FieldType == typeof(TValue)
                ? (Expression)valueParameter
                : Expression.Convert(valueParameter, fieldInfo.FieldType);
            var assignExpression = Expression.Assign(memberExpression, assignValue);
            return Expression.Lambda<Action<TSource, TValue>>(assignExpression, sourceParameter, valueParameter).Compile();
        }

        return null;
    }

    private static MemberExpression? UnwrapMemberExpression(Expression expression)
    {
        while (expression is UnaryExpression unaryExpression
               && (unaryExpression.NodeType == ExpressionType.Convert || unaryExpression.NodeType == ExpressionType.ConvertChecked))
        {
            expression = unaryExpression.Operand;
        }

        return expression as MemberExpression;
    }
}

internal sealed record EventHooks<TObject>(Action<TObject, EventHandler> Subscribe, Action<TObject, EventHandler> Unsubscribe)
    where TObject : class;