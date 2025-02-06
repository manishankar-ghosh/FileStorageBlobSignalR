using System.Reflection;

namespace FileStorageManagement.Utils
{
   public class MiscUtils
   {
      public static IEnumerable<(string Property, string SourceObjectValue, string TargetObjectValue)> GetObjectDiff(object sourceObj, object targetObj)
      {
         var sourceObjProperties = ObjectToDictionary(sourceObj);
         var targetObjProperties = ObjectToDictionary(targetObj);

         var diff = from s in sourceObjProperties
                    join t in targetObjProperties on s.Key equals t.Key
                    where s.Value != t.Value
                    select
                    (
                       Property: s.Key,
                       SourceObjectValue: s.Value,
                       TargetObjectValue: t.Value
                    );

         return diff;
      }

      public static Dictionary<string, string> ObjectToDictionary(object obj, string prefix = "")
      {
         if (obj == null) return new Dictionary<string, string>();

         var result = new Dictionary<string, string>();
         var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

         foreach (var property in properties)
         {
            var value = property.GetValue(obj);
            var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

            if (IsSimpleType(property.PropertyType))
            {
               result[key] = value?.ToString()!;
            }
            else
            {
               var nestedDictionary = ObjectToDictionary(value!, key);
               foreach (var kvp in nestedDictionary)
               {
                  result[kvp.Key] = kvp.Value;
               }
            }
         }

         return result;
      }

      private static bool IsSimpleType(Type type)
      {
         return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);
      }
   }
}
