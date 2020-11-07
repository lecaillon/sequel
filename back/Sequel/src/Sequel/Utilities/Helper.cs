using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Sequel
{
    public static class Helper
    {
        public static bool IgnoreErrors(Action operation)
        {
            if (operation is null)
            {
                return false;
            }

            try
            {
                operation.Invoke();
            }
            catch (Exception ex)
            {
                ex.LogWarning();
                return false;
            }

            return true;
        }

        public static async Task<bool> IgnoreErrorsAsync(Func<Task> operation)
        {
            if (operation is null)
            {
                return false;
            }

            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                ex.LogWarning();
                return false;
            }

            return true;
        }

        public static T IgnoreErrors<T>(Func<T> operation, T defaultValue = default)
        {
            if (operation is null)
            {
                return defaultValue;
            }

            T result;
            try
            {
                result = operation.Invoke();
            }
            catch(Exception ex)
            {
                ex.LogWarning();
                result = defaultValue;
            }

            return result;
        }

        public static async Task<T> IgnoreErrorsAsync<T>(Func<Task<T>> operation, T defaultValue = default)
        {
            if (operation is null)
            {
                return defaultValue;
            }

            T result;
            try
            {
                result = await operation();
            }
            catch (Exception ex)
            {
                ex.LogWarning();
                result = defaultValue;
            }

            return result;
        }

        public static bool IsNullOrEmpty<T>([NotNullWhen(returnValue: false)] this IEnumerable<T>? enumerable)
        {
            if (enumerable is null)
            {
                return true;
            }

            return !enumerable.Any();
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(keySelector, nameof(keySelector));

            return _(); IEnumerable<TSource> _()
            {
                var knownKeys = new HashSet<TKey>();
                foreach (var element in source)
                {
                    if (knownKeys.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
            }
        }

        public static void LogWarning(this Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(ex);
            Console.ResetColor();
        }
    }
}
