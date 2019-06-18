using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    class NoHelpAttribute : Attribute
    {
    }
}
