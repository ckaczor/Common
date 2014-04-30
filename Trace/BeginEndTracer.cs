using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Common.Debug
{
    public class BeginEndTracer : IDisposable
    {
        private string _sourceClass;
        private string _sourceMethod;

        private object _endParameter;
        private bool _endParameterSet;

        private const string EnterPattern = "Enter: {0} - {1}";
        private const string ExitPattern = "Exit: {0} - {1}";

        private const string EnterPatternWithParameter = "Enter: {0} - {1} - {2}";
        private const string ExitPatternWithParameter = "Exit: {0} - {1} - {2}";

        public BeginEndTracer(string sourceClass, [CallerMemberName] string sourceMethod = null, object beginParameter = null)
        {
            _sourceClass = sourceClass;
            _sourceMethod = sourceMethod;

            if (beginParameter == null)
                Tracer.WriteLine(string.Format(EnterPattern, _sourceClass, _sourceMethod));
            else
                Tracer.WriteLine(string.Format(EnterPatternWithParameter, _sourceClass, _sourceMethod, beginParameter));
        }

        public BeginEndTracer(string sourceClass, [CallerMemberName] string sourceMethod = null, params object[] beginParameters)
        {
            _sourceClass = sourceClass;
            _sourceMethod = sourceMethod;

            if (beginParameters == null || beginParameters.Length == 0)
                Tracer.WriteLine(string.Format(EnterPattern, _sourceClass, _sourceMethod));
            else if (beginParameters.Length == 1)
                Tracer.WriteLine(string.Format(EnterPatternWithParameter, _sourceClass, _sourceMethod, beginParameters[0]));
            else
            {
                string[] beginStrings = new string[beginParameters.Length];

                for (int i = 0; i < beginParameters.Length; i++)
                    beginStrings[i] = beginParameters[i].ToString();

                Tracer.WriteLine(string.Format(EnterPatternWithParameter, _sourceClass, _sourceMethod, string.Join(", ", beginStrings)));
            }
        }

        public void Dispose()
        {
            if (_endParameterSet)
                Tracer.WriteLine(string.Format(ExitPatternWithParameter, _sourceClass, _sourceMethod, _endParameter));
            else
                Tracer.WriteLine(string.Format(ExitPattern, _sourceClass, _sourceMethod));
        }

        public void SetEndParameter(object parameter)
        {
            _endParameter = parameter;

            _endParameterSet = true;
        }
    }
}
