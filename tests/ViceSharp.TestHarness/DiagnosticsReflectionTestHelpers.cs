namespace ViceSharp.TestHarness;

using System.Reflection;
using Xunit;

internal static class DiagnosticsReflectionTestHelpers
{
    public static Type RequiredType(string assemblyQualifiedName)
    {
        var type = Type.GetType(assemblyQualifiedName, throwOnError: false);
        Assert.NotNull(type);
        return type;
    }

    public static object RequiredProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target);
        Assert.NotNull(value);
        return value;
    }

    public static T RequiredProperty<T>(object target, string propertyName)
    {
        var value = RequiredProperty(target, propertyName);
        return Assert.IsType<T>(value);
    }

    public static object Invoke(object target, string methodName, params object?[] arguments)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        var value = method.Invoke(target, arguments);
        if (method.ReturnType == typeof(void))
            return new object();

        Assert.NotNull(value);
        return value;
    }

    public static async Task<object> InvokeAsync(object target, string methodName, params object?[] arguments)
    {
        var value = Invoke(target, methodName, arguments);
        return await AwaitAsync(value);
    }

    public static async Task<object> AwaitAsync(object value)
    {
        if (value is Task task)
        {
            await task;
            return RequiredTaskResult(task);
        }

        var asTask = value.GetType().GetMethod("AsTask", Type.EmptyTypes);
        if (asTask is not null && asTask.Invoke(value, null) is Task converted)
        {
            await converted;
            return RequiredTaskResult(converted);
        }

        return value;
    }

    public static object CreateInstance(Type type, params object?[] arguments)
    {
        var instance = Activator.CreateInstance(type, arguments);
        Assert.NotNull(instance);
        return instance;
    }

    private static object RequiredTaskResult(Task task)
    {
        var result = task.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)?.GetValue(task);
        Assert.NotNull(result);
        return result;
    }
}
