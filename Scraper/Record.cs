using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scraper
{
    class Record
    {
        public Record() { }

        public Record(string t, string ph, string lt, string ln, string ani, string add,
            string bl, string ct, string ps, string tp, string pr, string dt)
        {
            this.dName = t;
            this.phName = ph;
            this.ltName = lt;
            this.lnName = ln;
            this.aniName = ani;
            this.addName = add;
            this.blName = bl;
            this.ctName = ct;
            this.psName = ps;
            this.tpName = tp;
            this.prName = pr;
            this.dtName = dt;
        }

        public string dName { get; set; }

        public string phName { get; set; }

        public string ltName { get; set; }

        public string lnName { get; set; }

        public string aniName { get; set; }

        public string addName { get; set; }

        public string blName { get; set; }

        public string ctName { get; set; }

        public string psName { get; set; }

        public string tpName { get; set; }

        public string prName { get; set; }

        public string dtName { get; set; }



    }
}
