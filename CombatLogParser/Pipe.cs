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

    public class IEnumerablePipe<TOut, TIn>
        : Pipe<IEnumerable<TOut>, IEnumerable<TIn>>
    {
        public IEnumerablePipe(
            Pipe<TOut, TIn> singlePipe)
        {
            this.singlePipe = singlePipe;
        }

        public override IEnumerable<TOut> Process(IEnumerable<TIn> input)
        {
            return new Enumerable(singlePipe,input);
        }

        public class Enumerable
            : IEnumerable<TOut>
        {
            public Enumerable(
                Pipe<TOut, TIn> singlePipe,
                IEnumerable<TIn> source)
            {
                this.transformer = singlePipe;
                this.source = source;
            }

            public class Enumerator
            : IEnumerator<TOut>
            {
                public Enumerator(
                    Pipe<TOut, TIn> singlePipe,
                    IEnumerator<TIn> sourceEnumerator
                    )
                {
                    this.transformer = singlePipe;
                    this.source = sourceEnumerator;
                }

                private Pipe<TOut, TIn> transformer;
                private IEnumerator<TIn> source;
                private TOut _current;

                public TOut Current
                {
                    get { return _current; }
                }

                public void Dispose()
                {
                    source.Dispose();
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return _current; }
                }

                public bool MoveNext()
                {
                    bool retVal = source.MoveNext();
                    if (retVal)
                        _current = transformer.Process(source.Current);
                    else
                        _current = default(TOut);
                    return retVal;
                }

                public void Reset()
                {
                    source.Reset();
                    _current = default(TOut);
                }
            }

            private Pipe<TOut, TIn> transformer;
            private IEnumerable<TIn> source;

            public IEnumerator<TOut> GetEnumerator()
            {
                return new Enumerable.Enumerator(transformer, source.GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new Enumerable.Enumerator(transformer, source.GetEnumerator());
            }
        }

        private Pipe<TOut, TIn> singlePipe;
    }
}
