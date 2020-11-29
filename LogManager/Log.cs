using System;
using System.Collections.Generic;
using System.Text;

namespace neutrino {
    class LogData {
        public string title;
        public StringBuilder body = new StringBuilder();
        public LogLevel logLevel;

        public void Reset() {
            body.Clear();
        }
    }
}
