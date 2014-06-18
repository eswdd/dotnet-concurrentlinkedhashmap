using System;

namespace ConcurrentLinkedDictionary
{
    /// <summary>
    /// A class that can determine the weight of a value. The total weight threshold
    /// is used to determine when an eviction is required.
    /// </summary>
    public interface IWeigher<T>
    {

        /// <summary>
        /// Measures an object's weight to determine how many units of capacity that
        /// the value consumes. A value must consume a minimum of one unit.
        /// </summary>
        /// <param name="value">value the object to weigh</param>
        /// <returns>the object's weight</returns>
        int weightOf(T value);
    }
}

