using Audio.Processing.Pipelines;

namespace Audio.Processing.Components {
    
}

namespace Audio.Processing.Pipelines {
    public class AudioProcessingContext {
        public int TrackTime { get; set; }
        public int NoteTime { get; set; }
        public double Frequency { get; set; }
        public double Volume { get; set; }
        public double Pan { get; set; }
    }
    
    public interface ISink<in T> {
        void Consume(T value);
    }
    public interface ISource<out T> {
        void Produce(ISink<T> consumer);
    }
    public interface IConnectable<in TIn> : ISink<TIn>
    {
        public void AddParent();
    }
    public abstract class Processor<TIn, TOut> : ISink<TIn> {
        private ISink<TOut> _sink;

        protected abstract TOut Process(TIn value);
        public virtual void Flow(ISource<TIn> source, ISink<TOut> sink)
        {
            _sink = sink;
            source.Produce(this);
        }
        public void Consume(TIn value)
        {
            _sink.Consume(Process(value));
        }
    }
    public abstract class BaseInputBuffer<TIn, TOut> {
        public TOut Buffer { get; set; }
        public abstract void Add(TIn input);
        public abstract bool IsFilled();
    }
    public class InputBuffer<T> : BaseInputBuffer<T, T> {
        private bool _filled = false;
        public InputBuffer() => Buffer = default(T);

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
    public class BaseNode<TBuffer, TNodeIn, TProcessIn, TOut> : IConnectable<TNodeIn>, ISource<TProcessIn> where TBuffer : BaseInputBuffer<TNodeIn, TProcessIn>, new() {
        protected readonly TBuffer InputBuffer;
        private readonly List<ISink<TOut>> _nextNodes = [];
        private readonly Processor<TProcessIn, TOut>? _process;

        public BaseNode(Processor<TProcessIn, TOut>? process = null) {
            this._process = process;
            InputBuffer = new TBuffer();
        }

        public void Consume(TNodeIn value) {
            InputBuffer.Add(value);

            if (_process == null || !InputBuffer.IsFilled()) return;
            
            foreach (var node in _nextNodes) {
                _process.Flow(this, node);
            }
        }

        public void Produce(ISink<TProcessIn> consumer) {
            consumer.Consume(InputBuffer.Buffer);
        }
        
        public virtual void Connect<T>(T nextNode) where T : IConnectable<TOut> {
            _nextNodes.Add(nextNode);
        }

        public TProcessIn GetData() => InputBuffer.Buffer;
        
        public virtual void AddParent() { }
    }
    public class Node<TIn, TOut> : BaseNode<InputBuffer<TIn>, TIn, TIn, TOut>
    {
        public Node(Processor<TIn, TOut>? process = null) : base(process) { }
    }
    public class MultiNode<TIn, TOut> : BaseNode<MultiBuffer<TIn>, TIn, List<TIn>, TOut>
    {
        public MultiNode(Processor<List<TIn>, TOut>? process = null) : base(process) { }
        public override void Connect<T>(T nextNode)
        {
            base.Connect(nextNode);
            nextNode.AddParent();
        }
        public override void AddParent()
        {
            InputBuffer.AddParent();
        }
    }

    #region Processors
        public class PassThrough<T> : Processor<T, T>
        {
            protected override T Process(T value)
            {
                return value;
            }
        }
        public class HalfInput : Processor<double, double>
        {
            protected override double Process(double value)
            {
                return value / 2;
            }
        }
        public class AddOne : Processor<double, double>
        {
            protected override double Process(double value)
            {
                return value + 1;
            }
        }
        public class MinusOne : Processor<double, double>
        {
            protected override double Process(double value)
            {
                return value - 1;
            }
        }
        public class MultiplyByTwo : Processor<double, double>
        {
            protected override double Process(double value)
            {
                return value * 2;
            }
        }
        public class ToInteger : Processor<double, int>
        {
            protected override int Process(double value)
            {
                return (int)value;
            }
        }
        public class ToDouble : Processor<int, double>
        {
            protected override double Process(int value)
            {
                return value;
            }
        }
        public class Average : Processor<List<double>, double>
        {
            protected override double Process(List<double> value)
            {
                return value.Average();
            }
        }
    #endregion Processors
}