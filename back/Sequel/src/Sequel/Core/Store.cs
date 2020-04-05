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
        
        public static async Task<List<T>> GetListAsync()
        {
            using var fs = OpenFile();
            return await DeserializeListAsync(fs);
        }

        public static async Task Add<TIdentity>(TIdentity item) where TIdentity : Identity, T
        {
            Check.NotNull(item, nameof(item));

            using var stream = OpenFile();
            var list = await DeserializeListAsync(stream);

            if (item.Id is null)
            {
                item.Id = list.Cast<Identity>()?.Max(x => x.Id) + 1 ?? 1;
            }
            else
            {
                list.Remove(item);
            }
            list.Add(item);
            await SaveFileAsync(stream, list);
        }

        public static async Task Delete(int id)
        {
            using var stream = OpenFile();
            var list = await DeserializeListAsync(stream);
            T item = new T();
            (item as Identity)?.WithId(id);
            list.Remove(item);
            await SaveFileAsync(stream, list);
        }

        public static async Task<T> GetItem()
        {
            using var stream = OpenFile();
            return await DeserializeItemAsync(stream);
        }

        public static async Task Save(T item)
        {
            Check.NotNull(item, nameof(item));

            using var fs = OpenFile();
            await SaveFileAsync(fs, item);
        }

        private static FileStream OpenFile()
        {
            Directory.CreateDirectory(RootDirectory);
            return File.Open(FilePath, FileMode.OpenOrCreate);
        }

        private static async Task<List<T>> DeserializeListAsync(FileStream stream)
            => stream.Length > 0 ? await JsonSerializer.DeserializeAsync<List<T>>(stream) : new List<T>();

        private static async Task<T> DeserializeItemAsync(FileStream stream)
            => stream.Length > 0 ? await JsonSerializer.DeserializeAsync<T>(stream) : new T();

        private static async Task SaveFileAsync(FileStream stream, T value)
        {
            stream.SetLength(0);
            await JsonSerializer.SerializeAsync(stream, value);
        }

        private static async Task SaveFileAsync(FileStream stream, List<T> value)
        {
            stream.SetLength(0);
            await JsonSerializer.SerializeAsync(stream, value);
        }
    }
}
