using System;
using EnvDTE80;

namespace MegaSolutionContructor
{
    internal interface IFolder
    {
        Func<string, SolutionFolder> CreateChildSolutionFolder
        {
            get;
        }

        void CreateSolutionFolder();

        void AddProject(string filepath);
    }
}