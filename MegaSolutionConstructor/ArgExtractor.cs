using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaSolutionContructor
{
    public class ArgExtractor
    {
        private readonly string[] _args;

        public ArgExtractor(
            params string[] args)
        {
            _args = args ?? new string[0];
        }

        public int GetArgumentCount()
        {
            return
                _args != null ? _args.Length : 0;
        }

        public bool Exists(
            string argHead)
        {
            if (argHead == null)
            {
                throw new ArgumentNullException("argHead");
            }

            var uah = argHead.ToUpper();

            var result = _args.Any(j => j.ToUpper().StartsWith(uah));

            return result;
        }

        public string ExtractFirstTail(
            string argHead)
        {
            if (argHead == null)
            {
                throw new ArgumentNullException("argHead");
            }

            var uah = argHead.ToUpper();

            string result = null;

            var a = _args.ToList().Find(j => j.ToUpper().StartsWith(uah));
            if (a != null)
            {
                result = a.Substring(argHead.Length);
            }

            return result;
        }


        public List<string> ExtractTails(
            string argHead)
        {
            if (argHead == null)
            {
                throw new ArgumentNullException("argHead");
            }

            var uah = argHead.ToUpper();

            var result = new List<string>();

            var alist = _args.ToList().FindAll(j => j.ToUpper().StartsWith(uah));
            foreach(var a in alist)
            {
                var item = a.Substring(argHead.Length);
                result.Add(item);
            }

            return result;
        }
    }
}
