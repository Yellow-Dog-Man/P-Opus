using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace POpusCodec
{
    public static class Opus
    {
        public static string Version
        {
            get
            {
                var ptr = Wrapper.opus_get_version_string();
                return Marshal.PtrToStringAnsi(ptr);
            }
        }
    }
}
