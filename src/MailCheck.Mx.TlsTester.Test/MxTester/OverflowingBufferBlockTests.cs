using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MailCheck.Mx.TlsTester.MxTester;
using NUnit.Framework;

namespace MailCheck.Mx.TlsTester.Test.MxTester
{
    [TestFixture]
    public class OverflowingBufferBlockTests
    {
        [Test]
        public async Task ItemsOverflowWhenCapacityIsReached()
        {
            OverflowingBufferBlock<int> overflowingBufferBlock = new OverflowingBufferBlock<int>(4);
            List<int> mainline = new List<int>();
            List<int> overflow = new List<int>();

            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            ActionBlock<int> mainSink = new ActionBlock<int>(async i =>
            {
                await completionSource.Task;
                mainline.Add(i);
            }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
            var overflowBlock = new ActionBlock<int>(i => overflow.Add(i));
            overflowingBufferBlock.Source.LinkTo(mainSink, new DataflowLinkOptions { PropagateCompletion = true });
            overflowingBufferBlock.Overflow.LinkTo(overflowBlock, new DataflowLinkOptions { PropagateCompletion = true });

            foreach (int i in Enumerable.Range(0, 10))
            {
                await overflowingBufferBlock.Target.SendAsync(i);
            }

            overflowingBufferBlock.Target.Complete();
            await overflowingBufferBlock.Target.Completion;
            completionSource.SetResult(true);
            await Task.WhenAll(mainSink.Completion, overflowBlock.Completion);

            Assert.That(mainline, Has.Count.GreaterThan(4));
            Assert.That(overflow, Has.Count.EqualTo(10 - mainline.Count));
        }

        [Test]
        public async Task DoesntOverflowWhenCapacityNotReached()
        {
            OverflowingBufferBlock<int> overflowingBufferBlock = new OverflowingBufferBlock<int>(20);
            List<int> mainline = new List<int>();
            List<int> overflow = new List<int>();

            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            ActionBlock<int> mainSink = new ActionBlock<int>(async i =>
            {
                await completionSource.Task;
                mainline.Add(i);
            }, new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
            var overflowBlock = new ActionBlock<int>(i => overflow.Add(i));
            overflowingBufferBlock.Source.LinkTo(mainSink, new DataflowLinkOptions { PropagateCompletion = true });
            overflowingBufferBlock.Overflow.LinkTo(overflowBlock, new DataflowLinkOptions { PropagateCompletion = true });

            foreach (int i in Enumerable.Range(0, 10))
            {
                await overflowingBufferBlock.Target.SendAsync(i);
            }

            overflowingBufferBlock.Target.Complete();
            await overflowingBufferBlock.Target.Completion;
            completionSource.SetResult(true);
            await Task.WhenAll(mainSink.Completion, overflowBlock.Completion);

            Assert.That(mainline, Is.EquivalentTo(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
            Assert.That(overflow, Is.Empty);
        }
    }
}
