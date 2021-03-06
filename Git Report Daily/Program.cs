﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace GitDailyReport
{
   internal class Program
   {
      private static DateTimeOffset since = new DateTimeOffset(DateTime.Now.Date);

      private static void Main(string[] args)
      {
         try
         {
            var welcomeText = ReadWelcomFile("Welcome.txt");
            PrintWelcome(welcomeText);
         }
         catch (Exception ex)
         {
            System.Diagnostics.Debug.WriteLine(ex.Message);
         }

         try
         {
            string path = args.FirstOrDefault() ?? throw new Exception("Missing git path argument. Please add first $REPO arg to SourceTree action parameters");
            Console.WriteLine($"Open git {path}");
            if (Repository.IsValid(path))
            {
               using (var repo = new Repository(path))
               {
                  do
                  {
                     var commits = GetCommitsForDate(repo, since);
                     Console.WriteLine($"Git daily report commits ({commits.Count}) for {since.Date.ToShortDateString()}");
                     foreach (var item in commits.GroupBy(x => x.Author))
                     {
                        Console.WriteLine($"[Author {item.Key}]");
                        item.ToList().ForEach(x => Console.WriteLine(x.Message.Trim()));
                     }
                     Console.WriteLine("-----------------------------------------------------------------------------");
                  } while (ProcessInput());
               }
            }
            else
            {
               throw new Exception($"Invalid git path {path}. Please add first $REPO arg");
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex.Message);
         }
      }

      private static List<CommitItem> GetCommitsForDate(Repository repository, DateTimeOffset since)
      {
         var filter = new CommitFilter();
         var commitLog = repository.Commits.QueryBy(filter);
         var commits = commitLog
            .Where(c => c.Committer.When.Date == since)
            .Select(x => new CommitItem
            {
               Message = x.Message,
               Date = x.Committer.When,
               Author = x.Committer.Name
            }).ToList();

         if (repository.Submodules.Any())
         {
            Console.WriteLine($"Lookup submodules ({repository.Submodules.Count()})...");
         }

         foreach (var sub in repository.Submodules)
         {
            using (var subRep = new Repository(sub.Path))
            {
               Console.WriteLine($"Submodule {sub.Name} opened");
               commits.AddRange(GetCommitsForDate(subRep, since));
            }
         }
         return commits;
      }

      private static string ReadWelcomFile(string filename)
      {
         var assembly = Assembly.GetExecutingAssembly();
         string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(str => str.EndsWith(filename));

         using (Stream stream = assembly.GetManifestResourceStream(resourceName))
         using (StreamReader reader = new StreamReader(stream))
         {
            return reader.ReadToEnd();
         }
      }

      private static void PrintWelcome(string text)
      {
         var lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
         foreach (var line in lines)
         {
            Console.WriteLine(line);
            Thread.Sleep(50);
         }
      }

      private static bool ProcessInput()
      {
         var key = Console.ReadKey();
         switch (key.Key)
         {
            case ConsoleKey.LeftArrow:
               since = since.AddDays(-1);
               return true;

            case ConsoleKey.RightArrow:
               since = since.AddDays(1);
               return true;
         }
         return false;
      }
   }
}