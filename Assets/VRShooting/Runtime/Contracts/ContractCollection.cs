using System;
using System.Collections.Generic;

namespace VRShooting.Contracts
{
    internal static class ContractCollection
    {
        public static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values)
        {
            if (values == null || values.Count == 0) return Array.Empty<T>();
            var copy = new T[values.Count];
            for (var i = 0; i < copy.Length; i++) copy[i] = values[i];
            return Array.AsReadOnly(copy);
        }
    }
}
