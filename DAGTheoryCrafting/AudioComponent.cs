using System;
using System.Collections.Generic;
using Audio.Processing.Pipelines;

namespace Audio.Processing.Components {

    public class PassThrough<T> : Processor<T, T>
    {
        public override T Process(T value)
        {
            return value;
        }
    }
    public class HalfInput : Processor<double, double>
    {
        public override double Process(double value)
        {
            return value / 2;
        }
    }

    public class AddOne : Processor<double, double>
    {
        public override double Process(double value)
        {
            return value + 1;
        }
    }

    public class MinusOne : Processor<double, double>
    {
        public override double Process(double value)
        {
            return value - 1;
        }
    }

    public class MultiplyByTwo : Processor<double, double>
    {
        public override double Process(double value)
        {
            return value * 2;
        }
    }

    public class ToInteger : Processor<double, int>
    {
        public override int Process(double value)
        {
            return (int)value;
        }
    }
    
    public class ToDouble : Processor<int, double>
    {
        public override double Process(int value)
        {
            return value;
        }
    }

    public class Average : Processor<List<double>, double>
    {
        public override double Process(List<double> value)
        {
            return value.Average();
        }
    }

    public class AudioProcessingContext {
        public int TrackTime { get; set; }
        public int NoteTime { get; set; }
        public double Frequency { get; set; }
        public double Volume { get; set; }
        public double Pan { get; set; }
    }
}

namespace Audio.Processing.Pipelines {
    public interface ISink<in T> {
        void Consume(T value);
    }

    public interface ISource<out T> {
        void Produce(ISink<T> consumer);
    }
    
    public abstract class BaseInputBuffer<TIn, TOut> {
        public TOut Buffer { get; set; }
        public abstract void Add(TIn input);
        public abstract bool IsFilled();
    }

    public class SimpleBuffer<T> : BaseInputBuffer<T, T> {
        private bool _filled = false;
        public SimpleBuffer() => Buffer = default(T);

        public override void Add(T input)
        {
            Buffer = input;
            _filled = true;
        }
        public override bool IsFilled() => _filled;
    }

    public class MultiBuffer<T> : BaseInputBuffer<T, List<T>> {
        private int _parentCount = 0;
        public MultiBuffer() => Buffer = [];
        public override void Add(T input) => Buffer.Add(input);
        public override bool IsFilled() => Buffer.Count == _parentCount;
        public void AddParent() => _parentCount++;
    }

    public abstract class Processor<TIn, TOut> : ISink<TIn> {
        protected ISink<TOut> sink;

        public abstract TOut Process(TIn value);
        public virtual void Flow(ISource<TIn> source, ISink<TOut> sink)
        {
            this.sink = sink;
            source.Produce(this);
        }
        public void Consume(TIn value)
        {
            this.sink.Consume(Process(value));
        }
    }

    public interface IInputNode<in TIn> : ISink<TIn>
    {
        public void AddParent();
    }
    
    public class Node<TBuffer, TIn, TProcessIn, TOut> : IInputNode<TIn>, ISource<TProcessIn> where TBuffer : BaseInputBuffer<TIn, TProcessIn>, new() {
        protected readonly TBuffer InputBuffer;
        private readonly List<ISink<TOut>> _nextNodes = [];
        private readonly Processor<TProcessIn, TOut>? _process;

        public Node(Processor<TProcessIn, TOut>? process = null) {
            this._process = process;
            InputBuffer = new TBuffer();
        }

        public void Consume(TIn value) {
            InputBuffer.Add(value);

            if (_process == null || !InputBuffer.IsFilled()) return;
            
            foreach (var node in _nextNodes) {
                _process.Flow(this, node);
            }
        }

        public void Produce(ISink<TProcessIn> consumer) {
            consumer.Consume(InputBuffer.Buffer);
        }
        
        public void Connect<T>(T nextNode) where T : IInputNode<TOut> {
            _nextNodes.Add(nextNode);
            nextNode.AddParent();
        }

        public TProcessIn GetData() => InputBuffer.Buffer;
        
        public virtual void AddParent() { }
    }

    public class MultiNode<TIn, TOut> : Node<MultiBuffer<TIn>, TIn, List<TIn>, TOut>
    {
        public MultiNode(Processor<List<TIn>, TOut>? process = null) : base(process) { }
        public override void AddParent()
        {
            InputBuffer.AddParent();
        }
    }
}