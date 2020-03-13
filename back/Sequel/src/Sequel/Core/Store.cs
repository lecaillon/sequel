using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sequel.Core
{
    public static class Store<T> where T : class, new()
    {
        private static readonly string RootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sequel");
        private static readonly string FilePath = Path.Combine(RootDirectory, typeof(T).Name.ToLower() + ".json");
        
        public static async Task<List<T>> GetCollection()
        {
            using var fs = OpenFile();
            return await DeserializeCollection(fs);
        }

        public static async Task Add(T item, bool unique = false)
        {
            Check.NotNull(item, nameof(item));

            using var fs = OpenFile();
            var list = await DeserializeCollection(fs);
            if (unique && list.Any(item.Equals))
            {
                throw new Exception("Duplicate key: this item already exists.");
            }

            list.Add(item);
            await JsonSerializer.SerializeAsync(fs, list);
        }

        public static async Task<T> GetItem()
        {
            using var fs = OpenFile();
            return await DeserializeItem(fs);
        }

        public static async Task Save(T item)
        {
            Check.NotNull(item, nameof(item));

            using var fs = OpenFile();
            await JsonSerializer.SerializeAsync(fs, item);
        }

        private static FileStream OpenFile()
        {
            Directory.CreateDirectory(RootDirectory);
            return File.Open(FilePath, FileMode.OpenOrCreate);
        }

        private static async Task<List<T>> DeserializeCollection(FileStream fs)
            => fs.Length > 0 ? await JsonSerializer.DeserializeAsync<List<T>>(fs) : new List<T>();

        private static async Task<T> DeserializeItem(FileStream fs)
            => fs.Length > 0 ? await JsonSerializer.DeserializeAsync<T>(fs) : new T();
    }
}
