using System.Collections;
using Serilog.Core;
using Serilog.Events;

namespace ZimMarket.API.Logging;

/// <summary>
/// When objects are logged with <c>@</c> destructuring, replaces values of sensitive properties
/// so they are never written to sinks (Password, NationalIdNumber, CardNumber).
/// </summary>
internal sealed class RedactSensitivePropertiesDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "NationalIdNumber",
        "CardNumber"
    };

    private static readonly AsyncLocal<HashSet<object>?> CycleGuard = new();

#pragma warning disable CS8767 // Serilog IDestructuringPolicy nullability contract
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result)
    {
        result = null;

        if (value is null)
            return false;

        Type type = value.GetType();

        if (type.IsPrimitive
            || value is string
            || value is decimal
            || value is Guid
            || value is DateTime
            || value is DateTimeOffset
            || value is TimeSpan
            || type.IsEnum)
        {
            return false;
        }

        if (value is IEnumerable && value is not string)
            return false;

        string? ns = type.Namespace;
        if (string.IsNullOrEmpty(ns) || !ns.StartsWith("ZimMarket", StringComparison.Ordinal))
            return false;

        if (type.IsClass)
        {
            HashSet<object>? visited = CycleGuard.Value;
            if (visited is null)
            {
                visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
                CycleGuard.Value = visited;
            }

            if (!visited.Add(value))
            {
                result = new ScalarValue("[cyclic reference]");
                return true;
            }

            try
            {
                return TryDestructureCore(value, type, propertyValueFactory, out result);
            }
            finally
            {
                visited.Remove(value);
                if (visited.Count == 0)
                    CycleGuard.Value = null;
            }
        }

        return TryDestructureCore(value, type, propertyValueFactory, out result);
    }
#pragma warning restore CS8767

    private static bool TryDestructureCore(
        object value,
        Type type,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue? result)
    {
        var properties = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        var elements = new List<LogEventProperty>();

        foreach (System.Reflection.PropertyInfo prop in properties)
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            object? propValue;
            try
            {
                propValue = prop.GetValue(value);
            }
            catch
            {
                continue;
            }

            LogEventPropertyValue propLogValue = SensitivePropertyNames.Contains(prop.Name)
                ? propertyValueFactory.CreatePropertyValue("[REDACTED]", destructureObjects: false)
                : propertyValueFactory.CreatePropertyValue(propValue, destructureObjects: true);

            elements.Add(new LogEventProperty(prop.Name, propLogValue));
        }

        result = new StructureValue(elements);
        return true;
    }
}
