using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ALang
{
    class Scope
    {
        public List<string> ids;
        public List<TokenType> types;
        public int index;
        public int dindex;
        public Scope? parentScope;

        public Scope(List<string> ids, List<TokenType> types, int index, int dindex, Scope? parentScope)
        {
            this.ids = ids;
            this.types = types;
            this.index = index;
            this.dindex = dindex;
            this.parentScope = parentScope;
        }
    }
}
