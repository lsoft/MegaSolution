using System;
using EnvDTE100;
using EnvDTE80;

namespace MegaSolutionContructor
{
    internal class Root : IFolder
    {
        private readonly Solution4 _solution;

        public Root(
            Solution4 solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            _solution = solution;
        }

        public Func<string, SolutionFolder> CreateChildSolutionFolder
        {
            get
            {
                return
                    (childFolderName) => _solution.AddSolutionFolder(childFolderName).Object;
            }
        }

        public void CreateSolutionFolder()
        {
            //nothing to do
        }

        public void AddProject(string filepath)
        {
            if (filepath == null)
            {
                throw new ArgumentNullException("filepath");
            }

            this._solution.AddFromFile(filepath);
        }
    }
}