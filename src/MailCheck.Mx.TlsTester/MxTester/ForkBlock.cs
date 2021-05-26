using System;
using System.Threading.Tasks.Dataflow;

namespace MailCheck.Mx.TlsTester.MxTester
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ForkBlock<T>
    {
        /// <summary>
        /// Creates a ForkBlock with the supplied condition predicate determining which branch is taken.
        /// </summary>
        /// <param name="condition">If condition predicate returns true the item is routed to the left source, if false it does right.</param>
        public ForkBlock(Predicate<T> condition)
        {
            TransformBlock<Tuple<T, bool>, T> sourceLeft = new TransformBlock<Tuple<T, bool>, T>(item => Unwrap(item), new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
            TransformBlock<Tuple<T, bool>, T> sourceRight = new TransformBlock<Tuple<T, bool>, T>(item => Unwrap(item), new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });

            TransformBlock<T, Tuple<T, bool>> target = new TransformBlock<T, Tuple<T, bool>>(item =>
            {
                bool result = condition(item);
                return Tuple.Create(item, result);
            }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });

            target.LinkTo(sourceLeft, new DataflowLinkOptions { PropagateCompletion = true }, item => item.Item2);
            target.LinkTo(sourceRight, new DataflowLinkOptions { PropagateCompletion = true }, item => !item.Item2);

            Target = target;
            SourceLeft = sourceLeft;
            SourceRight = sourceRight;
        }

        public ITargetBlock<T> Target { get; set; }

        public ISourceBlock<T> SourceLeft { get; set; }

        public ISourceBlock<T> SourceRight { get; set; }

        private static T Unwrap(Tuple<T, bool> wrapped)
        {
            return wrapped.Item1;
        }
    }
}
