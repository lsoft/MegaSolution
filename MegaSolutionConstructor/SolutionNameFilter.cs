using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaSolutionContructor
{
    public class SolutionNameFilter
    {
        private readonly List<string> _positive;
        private readonly List<string> _negative;

        public SolutionNameFilter(
            List<string> positive,
            List<string> negative
            )
        {
            if (positive == null)
            {
                throw new ArgumentNullException("positive");
            }
            if (negative == null)
            {
                throw new ArgumentNullException("negative");
            }
            _positive = positive;
            _negative = negative;
        }

        public bool IsProjectFile(
            string solutionFileName
            )
        {
            if (solutionFileName == null)
            {
                throw new ArgumentNullException("solutionFileName");
            }

            var result = true;

            var ftl = solutionFileName.ToLower();

            if (!ftl.EndsWith(".csproj"))
            {
                result = false;
            }

            return result;
        }

        public bool IsAllowedToProcess(
            string solutionFileName
            )
        {
            if (solutionFileName == null)
            {
                throw new ArgumentNullException("solutionFileName");
            }

            var allowedToProcees = true;

            var ftl = solutionFileName.ToLower();

            if (!IsProjectFile(ftl))
            {
                allowedToProcees = false;
            }
            if (_positive.Count > 0)
            {
                if(!_positive.All(j => ftl.Contains(j)))
                {
                    allowedToProcees = false;
                }
            }
            if (_negative.Count > 0)
            {
                if(_negative.Any(j => ftl.Contains(j)))
                {
                    allowedToProcees = false;
                }
            }

            return
                allowedToProcees;
        }
    }
}
