using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace neutrino {
    public class Context : ExecutorContext {
        public Agent GetSender() {
            return s;
        }
    }
}
