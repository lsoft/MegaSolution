using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE100;

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

        [STAThread]
        static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Параметр 1: Путь к папке, откуда начать рекурсивно искать проекты. Туда же будет сохранен файл sln");
                Console.WriteLine("Параметр 2: название солюшена без суффикса .sln");
                Console.ResetColor();

                return;
            }

            _solutionFolder = args[0] == "." ? Directory.GetCurrentDirectory() : new DirectoryInfo(args[0]).FullName;
            _solutionName = args[1];

            if (File.Exists(_solutionFilePath))
            {
                File.Delete(_solutionFilePath);
            }

            using (new MessageFilter())
            {
                Console.WriteLine("Loading Visual Studio DTE");

                var solutionType = Type.GetTypeFromProgID("VisualStudio.Solution");
                var solution = System.Activator.CreateInstance(solutionType) as Solution4;

                try
                {
                    Console.WriteLine("Scanning...");

                    solution.Create(
                        _solutionFolder,
                        _solutionName);

                    ScanFoldersForProjects(
                        _solutionFolder,
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

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success");
            Console.ResetColor();
        }

        private static void ScanFoldersForProjects(
            string currentFolder,
            List<IFolder> folderList)
        {
            if (currentFolder == null)
            {
                throw new ArgumentNullException("currentFolder");
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
                    folderList);

                folderList.RemoveAt(folderList.Count - 1);
            }

            foreach (var file in Directory.GetFiles(currentFolder))
            {
                if (file.ToLower().EndsWith(".csproj"))
                {
                    if (!file.ToLower().EndsWith(".cf.csproj"))
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
