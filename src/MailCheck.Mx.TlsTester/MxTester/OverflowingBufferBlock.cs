using System.Threading.Tasks.Dataflow;

namespace MailCheck.Mx.TlsTester.MxTester
{

    public class OverflowingBufferBlock<T>
    {
        public OverflowingBufferBlock(int capacity)
        {
            capacity--; // decrement capacity by one due to buffering in ForkBlock
            BufferBlock<T> source = new BufferBlock<T>();
            ForkBlock<T> target = new ForkBlock<T>(item => source.Count <= capacity);

            target.SourceLeft.LinkTo(source, new DataflowLinkOptions { PropagateCompletion = true });

            Target = target.Target;
            Source = source;
            Overflow = target.SourceRight;
        }

        public ITargetBlock<T> Target { get; set; }

        public ISourceBlock<T> Source { get; set; }

        public ISourceBlock<T> Overflow { get; }
    }
}
