using System;
using System.Diagnostics.CodeAnalysis;

namespace Sequel
{
    /// <summary>
    ///     Static convenience methods to check that a method or a constructor is invoked with proper parameter or not.
    /// </summary>
    internal static class Check
    {
        private const string ArgumentIsEmpty = "The string cannot be empty.";
        private const string NumberNotPositive = "The number must be positive.";

        /// <summary>
        ///     Ensures that the string passed as a parameter is neither null or empty.
        /// </summary>
        /// <param name="text"> The string to test. </param>
        /// <param name="parameterName"> The name of the parameter to test. </param>
        /// <returns> The not null or empty string that was validated. </returns>
        /// <exception cref="ArgumentNullException"> Throws ArgumentNullException if the string is null. </exception>
        /// <exception cref="ArgumentException"> Throws ArgumentException if the string is empty. </exception>
        public static string NotNullOrEmpty(string? text, string parameterName)
        {
            if (text is null)
            {
                NotNullOrEmpty(parameterName, nameof(parameterName));

                throw new ArgumentNullException(parameterName);
            }
            else if (text.Trim().Length == 0)
            {
                NotNullOrEmpty(parameterName, nameof(parameterName));

                throw new ArgumentException(ArgumentIsEmpty, parameterName);
            }

            return text;
        }

        /// <summary>
        ///     Ensures that an object <paramref name="reference"/> passed as a parameter is not null.
        /// </summary>
        /// <typeparam name="T"> The type of the reference to test. </typeparam>
        /// <param name="reference"> An object reference. </param>
        /// <param name="parameterName"> The name of the parameter to test. </param>
        /// <returns> The non-null reference that was validated. </returns>
        /// <exception cref="ArgumentNullException"> Throws ArgumentNullException if the reference is null. </exception>
        public static T NotNull<T>([NotNull] T? reference, string parameterName) where T : class
        {
            if (reference is null)
            {
                NotNullOrEmpty(parameterName, nameof(parameterName));

                throw new ArgumentNullException(parameterName);
            }

            return reference;
        }

        /// <summary>
        ///     Ensures that the specified number is greater than zero.
        /// </summary>
        /// <param name="value"> The number to test. </param>
        /// <param name="parameterName"> The name of the parameter to test. </param>
        /// <returns> The number that was validated. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> Throws ArgumentOutOfRangeException if the number is not positive. </exception>
        public static T Positive<T>(T value, string parameterName) where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable
        {
            // https://msdn.microsoft.com/en-us/library/system.icomparable.compareto
            // Less than zero - This instance precedes obj in the sort order.
            // Zero - This instance occurs in the same position in the sort order as obj.
            // Greater than zero - This instance follows obj in the sort order.

            var minimumValue = default(T);
            var compare = value.CompareTo(minimumValue);
            if (compare <= 0)
            {
                NotNullOrEmpty(parameterName, nameof(parameterName));

                throw new ArgumentOutOfRangeException(parameterName, value, NumberNotPositive);
            }

            return value;
        }
    }
}
