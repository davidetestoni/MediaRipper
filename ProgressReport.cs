using System;
using System.Collections.Generic;
using System.Text;

namespace MediaRipper
{
    public struct ProgressReport
    {
        public float percent;
        public string message;

        public ProgressReport(float percent, string message)
        {
            this.percent = percent;
            this.message = message;
        }
    }
}
