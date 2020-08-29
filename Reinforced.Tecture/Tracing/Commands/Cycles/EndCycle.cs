﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Reinforced.Tecture.Commands;

namespace Reinforced.Tecture.Tracing.Commands.Cycles
{
    [CommandCode("CEND")]
    public class EndCycle : CommandBase, ITracingOnly
    {
        internal EndCycle() { }
        public int TotalCommands { get; internal set; }
        public int IterationsCount { get; internal set; }

        public override void Describe(TextWriter tw)
        {
            if (string.IsNullOrEmpty(Annotation))
            {
                tw.Write("Cycle ends in ");
                tw.Write(IterationsCount);
                tw.Write("iterations");
            }
            else
            {
                tw.Write(Annotation);
                tw.Write(" ends in ");
                tw.Write(IterationsCount);
                tw.Write(" iterations and ");
                tw.Write(TotalCommands);
                tw.Write(" commands");
            }
        }

        /// <summary>
        /// Clones command for tracing purposes
        /// </summary>
        /// <returns>Command clone</returns>
        protected override CommandBase DeepCloneForTracing()
        {
            return new EndCycle() { TotalCommands = TotalCommands, IterationsCount = IterationsCount };
        }
    }
}