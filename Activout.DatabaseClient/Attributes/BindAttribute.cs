using System;

namespace Activout.DatabaseClient.Attributes;

/// <summary>
/// Binds a method parameter, or a property of a <see cref="BindPropertiesAttribute"/> parameter,
/// to a named SQL parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class BindAttribute : Attribute
{
    /// <summary>The SQL parameter name, or <c>null</c> to use the C# parameter or property name.</summary>
    public string? ParameterName { get; }

    /// <summary>Binds using the C# parameter or property name.</summary>
    public BindAttribute()
    {
        // deliberately empty    
    }

    /// <summary>Binds to the given SQL parameter name.</summary>
    /// <param name="parameterName">The SQL parameter name, without prefix.</param>
    public BindAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }
}
