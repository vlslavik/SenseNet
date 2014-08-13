using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SenseNet.Packaging
{
    public class StepParameter
    {
        internal static readonly string ParameterRegex = @"^(PHASE\d+\.)?(STEP\d+)(\.[\w_]+)?:";

        public int PhaseIndex { get; private set; }
        public int StepIndex { get; private set; }
        public string PropertyName { get; private set; }
        public string Value { get; private set; }

        public static bool IsValidParameter(string parameter)
        {
            return Regex.Match(parameter, ParameterRegex, RegexOptions.IgnoreCase).Success;
        }
        internal static StepParameter Parse(string parameter)
        {
            var product = new StepParameter { PhaseIndex = 0, StepIndex = 0, PropertyName = string.Empty, Value = string.Empty };
            var match = Regex.Match(parameter, ParameterRegex, RegexOptions.IgnoreCase);
            var segments = new List<string>(match.Value.TrimEnd(':').Split('.'));
            if (segments[0].StartsWith("PHASE", StringComparison.OrdinalIgnoreCase))
            {
                product.PhaseIndex = int.Parse(segments[0].Substring(5)) - 1;
                segments.RemoveAt(0);
            }

            if (!segments[0].StartsWith("STEP", StringComparison.OrdinalIgnoreCase))
                throw new InvalidStepParameterException("Missing step index");
            product.StepIndex = int.Parse(segments[0].Substring(4)) - 1;

            if (segments.Count > 1)
                product.PropertyName = segments[1];

            if (match.Value.Length < parameter.Length)
                product.Value = parameter.Substring(match.Value.Length);

            return product;
        }
    }
}
