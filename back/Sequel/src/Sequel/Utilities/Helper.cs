using System;
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
            catch
            {
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
            catch
            {
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
            catch
            {
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
            catch
            {
                result = defaultValue;
            }

            return result;
        }
    }
}
