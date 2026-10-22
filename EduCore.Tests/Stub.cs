using System.Reflection;

namespace EduCore.Tests;

// A stand-in for an interface a test does not exercise, or exercises through one or two methods:
// unhandled calls return the type's default (a completed task, null, false), so a test fails on
// its assertions rather than on a method it never meant to touch.
public class Stub : DispatchProxy
{
    private Func<MethodInfo, object?[]?, object?>? _handler;

    public static T Of<T>(Func<MethodInfo, object?[]?, object?>? handler = null) where T : class
    {
        var proxy = Create<T, Stub>();
        ((Stub)(object)proxy)._handler = handler;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        var result = _handler?.Invoke(method!, args);
        if (result is not null)
            return result;

        var type = method!.ReturnType;
        if (type == typeof(Task))
            return Task.CompletedTask;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var inner = type.GetGenericArguments()[0];
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(inner)
                .Invoke(null, [inner.IsValueType ? Activator.CreateInstance(inner) : null]);
        }

        return type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
    }
}
