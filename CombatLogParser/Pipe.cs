using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatLogParser
{
    public interface Pipe
    {
        object ProcessNoType(object input);
    }

    public abstract class Pipe<TReturn>
        : Pipe
    {
        public object ProcessNoType(object input)
        {
            return Process(input);
        }
        public abstract TReturn Process(object input);
    }

    public abstract class Pipe<TReturn,TInput>
        : Pipe<TReturn>
    {
        public sealed override TReturn Process(object input)
        {
            return Process((TInput)input);
        }
        public Pipe<TNewReturn, TInput> Then<TNewReturn>(
            Pipe<TNewReturn, TReturn> thenPipe)
        {
            return new PipeConnector<TNewReturn, TReturn, TInput>(this, thenPipe);
        }
        public abstract TReturn Process(TInput input);
    }

    public sealed class PipeConnector<TOut, TMid, TIn>
        : Pipe<TOut,TIn>
    {
        public PipeConnector(
            Pipe<TMid,TIn> firstPipe,
            Pipe<TOut,TMid> secondPipe)
        {
            this.firstPipe = firstPipe;
            this.secondPipe = secondPipe;
        }
        public override TOut Process(TIn input)
        {
            return secondPipe.Process(firstPipe.Process(input));
        }
        private Pipe<TMid, TIn> firstPipe;
        private Pipe<TOut, TMid> secondPipe;
    }
}
