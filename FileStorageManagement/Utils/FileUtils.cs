namespace FileStorageManagement.Utils
{
   public class FileUtils
   {
      public static string GetMD5Hash(string input)
      {
         // Use input string to calculate MD5 hash
         using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
         {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes); // .NET 5 +

            // Convert the byte array to hexadecimal string prior to .NET 5
            // StringBuilder sb = new System.Text.StringBuilder();
            // for (int i = 0; i < hashBytes.Length; i++)
            // {
            //     sb.Append(hashBytes[i].ToString("X2"));
            // }
            // return sb.ToString();
         }
      }

      public static long GetFileSize(string path)
      {
         long fileSize = -1;
         try
         {
            var fileInfo = new System.IO.FileInfo(path);
            if (fileInfo.Exists)
            {
               fileSize = fileInfo.Length;
            }
         }
         catch (Exception ex)
         {
            throw new Exception($"Error reading file from File storage temp location. {ex.Message}");
         }
        
         return fileSize;
      }
   }
}
