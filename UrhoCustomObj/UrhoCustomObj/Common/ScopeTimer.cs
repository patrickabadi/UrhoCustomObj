using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UrhoCustomObj.Common
{
    public class ScopeTimer: IDisposable
    {
        private readonly string _message;
        private readonly Stopwatch _sw;

        public ScopeTimer(string message)
        {
            _message = message;
            _sw = new Stopwatch();
            _sw.Start();
        }

        public void Dispose()
        {
            _sw.Stop();

            Debug.WriteLine($"{_message} in {_sw.ElapsedMilliseconds} milliseconds");
        }

    }
}
