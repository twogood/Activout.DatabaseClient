using System;

namespace Activout.DatabaseClient.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class BindAttribute : Attribute
    {
        public string? ParameterName { get; }

        public BindAttribute()
        {
            // deliberately empty    
        }

        public BindAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }
    }
}