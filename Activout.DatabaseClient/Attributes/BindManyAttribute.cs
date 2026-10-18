using System;

namespace Activout.DatabaseClient.Attributes;

/// <summary>
/// Marks an <c>IEnumerable&lt;T&gt;</c> parameter of a <see cref="SqlUpdateAttribute"/> method so that the
/// statement is executed once per item. The public properties of each item are bound like
/// <see cref="BindPropertiesAttribute"/>; all other parameters are shared by every execution.
/// The value must not be <c>null</c> or contain <c>null</c> items.
/// Property bindings, including prefixed aliases, must not overlap each other or shared bindings,
/// ignoring case. Overlaps throw <see cref="NotSupportedException"/> before execution, even for an empty collection.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class BindManyAttribute : Attribute
{
}
