namespace Audio.Processing.Pipelines {
    public class AudioProcessingContext {
        public int TrackTime { get; set; }
        public int NoteTime { get; set; }
        public double Frequency { get; set; }
        public double Volume { get; set; }
        public double Pan { get; set; }
    }
    
    // Interfaces
    public interface ISink<in T>
    {
        void Consume(T value);
    }
    public interface ISource<out T>
    {
        void Produce(ISink<T> consumer);
    }
    public interface IFlushable
    {
        public void Flush();
    }
    public interface IFlowNode<in TIn> : ISink<TIn>
    {
        void AddParent<TOut>();
    }
    public class FlowNodeAdapter<T> : IFlowNode<T>
    {
        private readonly IFlowNode<object> _target;

        public FlowNodeAdapter(IFlowNode<object> target)
        {
            _target = target;
        }

        public void Consume(T value)
        {
            _target.Consume(value!);
        }

        public void AddParent<TOut>()
        {
            _target.AddParent<TOut>();
        }
    }
    public abstract class Processor<TIn, TOut> : ISink<TIn>
    {
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

    // Buffers
    public abstract class BaseInputBuffer<TIn, TOut> : IFlushable
    {
        public TOut Buffer { get; set; }
        public abstract void Add(TIn input);
        public abstract bool IsFilled();
        public virtual void Flush() => Buffer = default(TOut);
        public virtual void AddParent<T>() { }
    }
    public class InputBuffer<T> : BaseInputBuffer<T, T>
    {
        private bool _filled = false;
        public InputBuffer() => Buffer = default(T);

        public override void Add(T input)
        {
            Buffer = input;
            _filled = true;
        }

        public override bool IsFilled() => _filled;
        public override void Flush() => _filled = false;
    }
    public class MultiBuffer<TIn> : BaseInputBuffer<TIn, List<TIn>>
    {
        private int _parentCount = 0;
        public MultiBuffer() => Buffer = [];

        public override void Add(TIn input)
        {
            Buffer.Add(input);
        }
        public override bool IsFilled() => Buffer.Count == _parentCount;
        public override void AddParent<T>() => _parentCount++;
        public override void Flush() => Buffer.Clear();
    }
    public class CompositeInputBuffer<T1, TP1, TB1, T2, TP2, TB2> : BaseInputBuffer<object, (TP1, TP2)>
        where TB1 : BaseInputBuffer<T1, TP1>, new()
        where TB2 : BaseInputBuffer<T2, TP2>, new()
    {
        // Composite Input Buffer for multiple heterogeneous inputs
        
        private readonly TB1 _buffer1 = new();
        private readonly TB2 _buffer2 = new();

        public override void Add(object input)
        {
            switch (input)
            {
                case T1 t1:
                    _buffer1.Add(t1);
                    break;
                case T2 t2:
                    _buffer2.Add(t2);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid input type: {input.GetType()}");
            }

            if (IsFilled())
                Buffer = (_buffer1.Buffer, _buffer2.Buffer);
        }
        
        public override void AddParent<T>()
        {
            if (typeof(T) == typeof(T1))
            {
                _buffer1.AddParent<TP1>();
            }
            else if (typeof(T) == typeof(T2))
            {
                _buffer2.AddParent<TP2>();
            }
            else
            {
                throw new InvalidOperationException($"Invalid input type: {typeof(T)}");
            }
        }

        public override bool IsFilled() => _buffer1.IsFilled() && _buffer2.IsFilled();
        public override void Flush()
        {
            _buffer1.Flush();
            _buffer2.Flush();
        }
    }

    // Nodes
    public abstract class AbstractNode<TBuffer, TNodeIn, TProcessIn, TOut> : ISource<TProcessIn>, IFlowNode<TNodeIn>
        where TBuffer : BaseInputBuffer<TNodeIn, TProcessIn>, new()
    {
        private readonly List<IFlowNode<TOut>> _nextNodes = [];
        private readonly Processor<TProcessIn, TOut>? _process;
        protected readonly TBuffer InputBuffer;

        public AbstractNode(Processor<TProcessIn, TOut>? process = null)
        {
            _process = process;
            InputBuffer = new TBuffer();
        }

        public void Consume(TNodeIn value)
        {
            InputBuffer.Add(value);
            
            // If the buffer is not filled, or no process has been assigned, we can't process this node
            if (!InputBuffer.IsFilled() || _process == null) return;
            
            // Recursively traces the flow path through the graph
            foreach (var node in _nextNodes)
                _process.Flow(this, node);
            
            // On the return path flush the buffer to avoid tracing the graph twice
            InputBuffer.Flush();
        }
        
        public void Produce(ISink<TProcessIn> consumer)
        {
            consumer.Consume(GetData());
        }
        
        public virtual void Connect<T>(T nextNode) where T : IFlowNode<TOut>
        {
            _nextNodes.Add(nextNode);
            nextNode.AddParent<TOut>();
        }
        
        public void ConnectToObjectSink(IFlowNode<object> nextNode)
        {
            _nextNodes.Add(new FlowNodeAdapter<TOut>(nextNode));
            nextNode.AddParent<TOut>();
        }

        public TProcessIn GetData() => InputBuffer.Buffer;

        // Only multi-input nodes need to have knowledge of upstream connections
        public virtual void AddParent<T>() { }
    }
    public class Node<TIn, TOut> : AbstractNode<InputBuffer<TIn>, TIn, TIn, TOut>
    {
        public Node(Processor<TIn, TOut>? process = null) : base(process) { }
    }
    public class InputNode<T> : Node<T, T>
    {
        public InputNode() : base(new PassThrough<T>()) { }
    }
    public class MultiNode<TIn, TOut> : AbstractNode<MultiBuffer<TIn>, TIn, List<TIn>, TOut>
    {
        public MultiNode(Processor<List<TIn>, TOut>? process = null) : base(process) { }

        public override void AddParent<T>()
        {
            InputBuffer.AddParent<T>();
        }
    }
    public class CompositeNode<T1, T2, TOut> : AbstractNode<CompositeInputBuffer<T1, T1, InputBuffer<T1>, T2, T2, InputBuffer<T2>>, object, (T1, T2), TOut>
    {
        public CompositeNode(Processor<(T1, T2), TOut>? process = null) : base(process) { }
    }
    public class MultiCompositeNode<T1, T2, TOut> : AbstractNode<CompositeInputBuffer<T1, List<T1>, MultiBuffer<T1>, T2, List<T2>, MultiBuffer<T2>>, object, (List<T1>, List<T2>), TOut>
    {
        public MultiCompositeNode(Processor<(List<T1>, List<T2>), TOut>? process = null) : base(process) { }

        public override void AddParent<T>()
        {
            InputBuffer.AddParent<T>();
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

        public class AddTenWhenInputOn : Processor<(bool, double), double>
        {
            protected override double Process((bool, double) value)
            {
                return value.Item1 ? value.Item2 + 10 : value.Item2;
            }
        }
        
        public class AdsrSimple : Processor<(bool, double), double>
        {
            private int _counter;
            protected override double Process((bool, double) value)
            {
                var (trigger, v) = value;

                if (trigger)
                    _counter = 100;
                
                if (_counter > 0)
                    _counter--;
                
                return _counter > 0 ? v : 0;
            }
        }

        public class Oscillator : Processor<(int, double), double>
        {
            protected override double Process((int, double) value)
            {
                var (t, f) = value;
                return Math.Sin(2 * Math.PI * f * t / 44200);
            }
        }

        public class MultiCompositeProcess : Processor<(List<int>, List<double>), double>
        {
            protected override double Process((List<int>, List<double>) value)
            {
                var (ints, doubles) = value;
                int largest = Math.Max(ints.Count, doubles.Count);
                double total = 0;

                for (int i = 0; i < largest; i++)
                {
                    int lhs = ints.Count > i ? ints[i] : 0;
                    double rhs = doubles.Count > i ? doubles[i] : 0;
                    
                    total += lhs * rhs;
                }
                
                return total;
            }
        }
    #endregion Processors
}