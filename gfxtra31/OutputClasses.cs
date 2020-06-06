using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gfxtra31
{
    class OutputClasses
    {
        public string Name { get; set; }
        public string PageUrl { get; set; }
        public string FileUrl { get; set; }
    }

    class Arguments
    {
        public int firstPage { get; set; }
        public int lastPage { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
    }
}
