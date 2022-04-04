using System;
using UnityEngine;

namespace Fizz6.Autofill
{
    public class AutofillAttribute : PropertyAttribute
    {
        [Flags]
        public enum Target
        {
            None = 0b_0000_0000, // 0
            Self = 0b_0000_0001, // 1
            Parent = 0b_0000_0010, // 2
            Children = 0b_0000_0100, // 4
        }
        
        public Target Targets { get; }

        public AutofillAttribute(Target targets = Target.Self)
        {
            Targets = targets;
        }
    }
}
