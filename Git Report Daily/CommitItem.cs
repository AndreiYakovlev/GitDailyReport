using System;

namespace GitDailyReport
{
   public class CommitItem
   {
      public string Message { get; set; }
      public DateTimeOffset Date { get; set; }

      public string Author { get; set; }
   }
}