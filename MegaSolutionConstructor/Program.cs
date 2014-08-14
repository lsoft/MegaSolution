using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE100;
using EnvDTE80;

namespace MegaSolutionContructor
{
    class Program
    {
        private static string _solutionName;
        private static string _solutionFolder;

        private static string _solutionFileName
        {
            get
            {
                return
                    _solutionName + ".sln";
            }
        }

        private static string _solutionFilePath
        {
            get
            {
                return
                    Path.Combine(
                        _solutionFolder,
                        _solutionFileName);
            }
        }

        private const string StudioVersionKey = "-studio_version:";
        private const string FolderKey = "-folder:";
        private const string SolutionNameKey = "-solution_name:";
        private const string PositiveFilterKey = "-must:";
        private const string NegativeFilterKey = "-mustnot:";


        [STAThread]
        private static void Main(string[] args)
        {
            var extractor = new ArgExtractor(args);

            var studioVersionExists = extractor.Exists(StudioVersionKey);
            var folderExists = extractor.Exists(FolderKey);
            var solutionNameExists = extractor.Exists(SolutionNameKey);


            if (!studioVersionExists || !folderExists || !solutionNameExists)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("-studio_version:Версия студии, которую надо открывать (9 - VS 2008, 11 - VS 2012 и т.п.)");
                Console.WriteLine("-folder:Путь к папке, откуда начать рекурсивно искать проекты. Туда же будет сохранен файл sln");
                Console.WriteLine("-solution_name:Название солюшена без суффикса .sln");
                Console.WriteLine("-must:Позитивный фильтр для имени файла проекта. Необязателен. Может быть несколько");
                Console.WriteLine("-mustnot:Негативный фильтр для имени файла проекта. Необязателен. Может быть несколько");
                Console.ResetColor();

                return;
            }

            var argFolder = extractor.ExtractFirstTail(FolderKey);

            _solutionFolder = argFolder == "." ? Directory.GetCurrentDirectory() : new DirectoryInfo(argFolder).FullName;
            _solutionName = extractor.ExtractFirstTail(SolutionNameKey);

            var studioVersion = extractor.ExtractFirstTail(StudioVersionKey);

            var positive = new List<string>();
            if (extractor.Exists(PositiveFilterKey))
            {
                positive = extractor.ExtractTails(PositiveFilterKey);
            }

            var negative = new List<string>();
            if (extractor.Exists(NegativeFilterKey))
            {
                negative = extractor.ExtractTails(NegativeFilterKey);
            }

            Console.WriteLine("Studio version: {0}", studioVersion);
            Console.WriteLine("Folder: {0}", _solutionFolder);
            Console.WriteLine("SLN name: {0}", _solutionName);
            Console.WriteLine("Positive filter: {0}", string.Join("   ", positive));
            Console.WriteLine("Negative filter: {0}", string.Join("   ", negative));

            if (File.Exists(_solutionFilePath))
            {
                File.Delete(_solutionFilePath);
            }

            using (new MessageFilter())
            {
                Console.WriteLine("Loading Visual Studio DTE");

                // Get the ProgID for DTE
                var t = System.Type.GetTypeFromProgID(
                    string.Format("VisualStudio.DTE.{0}.0", studioVersion),
                    true);

                // Create a new instance of the IDE.
                var obj = System.Activator.CreateInstance(t, true);

                // Cast the instance to DTE2 and assign to variable dte.
                var dte = (EnvDTE80.DTE2)obj;

                var studioVersionDict = new Dictionary<string, string>();
                studioVersionDict.Add("8.0", "2005");
                studioVersionDict.Add("9.0", "2008");
                studioVersionDict.Add("10.0", "2010");
                studioVersionDict.Add("11.0", "2012");

                Console.WriteLine(
                    "Loaded Visual Studio {0} ({1})",
                    studioVersionDict.ContainsKey(dte.Version) ? studioVersionDict[dte.Version] : string.Empty, 
                    dte.Version);

                var solution = (Solution2)dte.Solution;

                try
                {
                    Console.WriteLine("Scanning...");

                    solution.Create(
                        _solutionFolder,
                        _solutionName);

                    var snf = new SolutionNameFilter(
                        positive,
                        negative
                        );

                    ScanFoldersForProjects(
                        _solutionFolder,
                        snf,
                        new List<IFolder>
                        {
                            new Root(solution)
                        });

                    solution.SaveAs(_solutionFileName);

                }
                catch (Exception excp)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR");
                    Console.WriteLine(excp.GetType().Name);
                    Console.WriteLine(excp.Message);
                    Console.WriteLine(excp.StackTrace);
                    Console.ResetColor();
                }
                finally
                {
                    solution.Close();
                }
            }

            //removing absolute paths
            Console.WriteLine("Removing absolute paths");

            var newSln = new StringBuilder();


            //читаем хидер проекта
            var header = new byte[3];
            using (var fs = new FileStream(_solutionFilePath, FileMode.Open, FileAccess.Read))
            {
                fs.Read(header, 0, 3);
            }

            var slnLines = File.ReadAllLines(_solutionFilePath);
            foreach (var l in slnLines)
            {
                var toWrite = l;

                if (toWrite.StartsWith("Project("))
                {
                    var dc = Regex.Matches(toWrite, "\\.\\.").Count;
                    if (dc > 0)
                    {
                        var si = toWrite.IndexOf("..");
                        var replaced = toWrite.Replace("..\\", "");

                        for (var i = 0; i < dc; i++)
                        {
                            var slashi = replaced.Substring(si).IndexOf("\\");
                            replaced = replaced.Remove(si, slashi + 1);
                        }

                        toWrite = replaced;
                    }
                }

                newSln.AppendLine(toWrite);
            }

            using (var fs = new FileStream(_solutionFilePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(header, 0, header.Length);

                var bytes = Encoding.UTF8.GetBytes(newSln.ToString());
                fs.Write(bytes, 0, bytes.Length);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success");
            Console.ResetColor();
        }

        private static void ScanFoldersForProjects(
            string currentFolder,
            SolutionNameFilter snf,
            List<IFolder> folderList)
        {
            if (currentFolder == null)
            {
                throw new ArgumentNullException("currentFolder");
            }
            if (snf == null)
            {
                throw new ArgumentNullException("snf");
            }
            if (folderList == null)
            {
                throw new ArgumentNullException("folderList");
            }

            Console.WriteLine(
                "Scanning {0}",
                currentFolder);

            foreach (var cycleFolder in Directory.GetDirectories(currentFolder))
            {
                var folderName = ObtainDirectoryName(cycleFolder);

                var folder = new Folder(
                    folderList.Last(),
                    folderName
                    );

                folderList.Add(folder);

                ScanFoldersForProjects(
                    cycleFolder,
                    snf,
                    folderList);

                folderList.RemoveAt(folderList.Count - 1);
            }

            foreach (var file in Directory.GetFiles(currentFolder))
            {
                if (snf.IsProjectFile(file))
                {
                    if (snf.IsAllowedToProcess(file))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("ADDED {0}", file);
                        Console.ResetColor();

                        //файл проекта, годный

                        //создаем (если не создано папки в солюшене)
                        //последнюю папку пропускаем, так как она просто хранилище для csproj и скорее всего совпадает по имени с ним
                        var toCreate = new List<IFolder>(folderList);
                        toCreate.RemoveAt(toCreate.Count - 1);

                        toCreate.ForEach(j => j.CreateSolutionFolder());

                        //добавляем проект
                        toCreate.Last().AddProject(file);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("SKIPPED {0}", file);
                        Console.ResetColor();
                    }
                }
            }
        }

        private static string ObtainDirectoryName(string pathToDirOrFile)
        {
            if (pathToDirOrFile == null)
            {
                throw new ArgumentNullException("pathToDirOrFile");
            }

            return
                Directory.Exists(pathToDirOrFile)
                    ? new DirectoryInfo(pathToDirOrFile).Name
                    : new DirectoryInfo(new FileInfo(pathToDirOrFile).DirectoryName).Name;
        }
    }
}
