using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sequel.Models;

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

        public static async Task Add<TIdentity>(TIdentity item) where TIdentity : Identity, T
        {
            Check.NotNull(item, nameof(item));

            using var fs = OpenFile();
            var list = await DeserializeCollection(fs);

            if (item.Id is null)
            {
                item.Id = list.Cast<Identity>()?.Max(x => x.Id) + 1 ?? 1;
            }
            else
            {
                list.Remove(item);
            }
            list.Add(item);
            await SaveFile(fs, list);
        }

        public static async Task Delete(int id)
        {
            using var fs = OpenFile();
            var list = await DeserializeCollection(fs);
            T item = new T();
            (item as Identity)?.WithId(id);
            list.Remove(item);
            await SaveFile(fs, list);
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
            await SaveFile(fs, item);
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

        private static async Task SaveFile(FileStream fs, T value)
        {
            fs.SetLength(0);
            await JsonSerializer.SerializeAsync(fs, value);
        }

        private static async Task SaveFile(FileStream fs, List<T> value)
        {
            fs.SetLength(0);
            await JsonSerializer.SerializeAsync(fs, value);
        }
    }
}
