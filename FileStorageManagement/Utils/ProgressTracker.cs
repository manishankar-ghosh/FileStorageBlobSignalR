namespace FileStorageManagement.Utils
{
   public class ProgressTracker : IProgress<long>
   {
      private Func<double, Task> _reportProgressToClient;
      //private readonly TextWriter _logWriter;
      private readonly long _totalLength;
      private double _progress = 0;

      public ProgressTracker(Func<double, Task> reportProgressToClient, long totalLength)
      {
         _totalLength = totalLength;
         _reportProgressToClient = reportProgressToClient;
      }

      public void Report(long value)
      {
         var progress = Math.Round((double)value / _totalLength * 100, 2);
         if (progress - _progress > 1 || progress >= 100)
         {
            _progress = progress;
            //_logWriter.WriteLine($"{Math.Round(_progress, 0)}%");
            _reportProgressToClient(progress);
         }
      }
   }
}
