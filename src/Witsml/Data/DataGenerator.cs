using System;
using System.Collections.Generic;

namespace PDS.Witsml.Data
{
    public class DataGenerator
    {
        public readonly string TimestampFormat = "yyMMdd-HHmmss-fff";

        /// <summary>
        /// Creates a <see cref="Guid"/>.
        /// </summary>
        /// <returns>The Guid in string</returns>
        public string Uid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates a name with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns>The name.</returns>
        public string Name(string prefix = null)
        {
            if (String.IsNullOrWhiteSpace(prefix))
                return DateTime.Now.ToString(TimestampFormat);

            return String.Format("{0} - {1}", prefix, DateTime.Now.ToString(TimestampFormat));
        }

        /// <summary>
        /// Lists the specified instances.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instances">The instances.</param>
        /// <returns></returns>
        public List<T> List<T>(params T[] instances)
        {
            return new List<T>(instances);
        }
    }
}
