using System;

namespace Activout.DatabaseClient.Attributes;

/// <summary>
/// Binds each public property of the parameter as an SQL parameter, both as <c>@property</c>
/// and as <c>@parameter_property</c>. The parameter value must not be <c>null</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class BindPropertiesAttribute : Attribute
{
}
