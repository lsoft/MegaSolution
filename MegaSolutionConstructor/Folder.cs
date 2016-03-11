using System;
using EnvDTE80;

namespace MegaSolutionContructor
{
    internal class Folder : IFolder
    {
        private readonly Func<string, SolutionFolder> _createSolutionFolder;

        private readonly string _name;
        private SolutionFolder _sf;

        public Func<string, SolutionFolder> CreateChildSolutionFolder
        {
            get
            {
                return
                    (childFolderName) => _sf.AddSolutionFolder(childFolderName).Object;
            }
        }

        public Folder(
            IFolder parent,
            string name
            )
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            _createSolutionFolder = (childFolderName) => parent.CreateChildSolutionFolder(childFolderName);
            _name = name;
        }

        public void CreateSolutionFolder()
        {
            if (this._sf != null)
            {
                return;
            }

            this._sf = _createSolutionFolder(this._name);
        }

        public void AddProject(string filepath)
        {
            if (filepath == null)
            {
                throw new ArgumentNullException("filepath");
            }

            this._sf.AddFromFile(filepath);
        }
    }
}