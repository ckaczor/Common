using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class Extensions
    {
        public static bool GetBitValue(this int integer, int bit)
        {
            return (integer & (1 << bit)) != 0;
        }

        public static int SetBitValue(this int integer, int bit, bool value)
        {
            if (value)
                integer |= 1 << bit;
            else
                integer &= ~(1 << bit);

            return integer;
        }

        public static string ToJson<T>(this T obj) where T : class
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                return Encoding.Default.GetString(stream.ToArray());
            }
        }

        public static Task<T> WithTimeout<T>(this Task<T> task, int duration)
        {
            return Task.Factory.StartNew(() =>
            {
                bool b = task.Wait(duration);
                if (b) return task.Result;
                return default(T);
            });
        }

        public static bool IsBetween<T>(this T item, T start, T end, bool inclusive = false)
        {
            return inclusive ?
                Comparer<T>.Default.Compare(item, start) >= 0 && Comparer<T>.Default.Compare(item, end) <= 0 :
                Comparer<T>.Default.Compare(item, start) > 0 && Comparer<T>.Default.Compare(item, end) < 0;
        }
    }
}
