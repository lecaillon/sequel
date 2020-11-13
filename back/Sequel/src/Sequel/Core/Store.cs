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
        private static readonly string FilePath = Path.Combine(Program.RootDirectory, typeof(T).Name.ToLower() + ".json");
        
        public static async Task<List<T>> GetList()
        {
            using var fs = OpenFile();
            return await DeserializeList(fs);
        }

        public static async Task Init(IEnumerable<T> list)
        {
            Check.NotNull(list, nameof(list));
            
            using var fs = OpenFile(FileMode.Create);
            await SaveFile(fs, list);
        }

        public static async Task Add<TIdentity>(TIdentity item) where TIdentity : Identity, T
        {
            Check.NotNull(item, nameof(item));

            using var stream = OpenFile();
            var list = await DeserializeList(stream);

            if (item.Id is null)
            {
                item.Id = list.Cast<Identity>()?.Max(x => x.Id) + 1 ?? 1;
            }
            else
            {
                list.Remove(item);
            }
            list.Add(item);
            await SaveFile(stream, list);
        }

        public static async Task Delete(int id)
        {
            using var stream = OpenFile();
            var list = await DeserializeList(stream);
            T item = new T();
            (item as Identity)?.WithId(id);
            list.Remove(item);
            await SaveFile(stream, list);
        }

        public static bool Exists() => File.Exists(FilePath);

        private static FileStream OpenFile(FileMode mode = FileMode.OpenOrCreate)
        {
            Directory.CreateDirectory(Program.RootDirectory);
            return File.Open(FilePath, mode);
        }

        private static async Task<List<T>> DeserializeList(FileStream stream)
            => stream.Length > 0 ? await JsonSerializer.DeserializeAsync<List<T>>(stream) ?? new List<T>() : new List<T>();

        private static async Task SaveFile(FileStream stream, IEnumerable<T> value)
        {
            stream.SetLength(0);
            await JsonSerializer.SerializeAsync(stream, value, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
