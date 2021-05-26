using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NUnit.Framework;
using MailCheck.Mx.TlsTester.MxTester;

namespace MailCheck.Mx.TlsTester.Test.MxTester
{
    [TestFixture]
    public class ForkBlockTests
    {
        [Test]
        public async Task ItemsPassingPredicateGoLeftFailsGoRight()
        {
            var left = new List<int>();
            var right = new List<int>();
            var leftBlock = new ActionBlock<int>(i => left.Add(i));
            var rightBlock = new ActionBlock<int>(i => right.Add(i));

            var fork = new ForkBlock<int>(i => i < 5);
            fork.SourceLeft.LinkTo(leftBlock, new DataflowLinkOptions { PropagateCompletion = true });
            fork.SourceRight.LinkTo(rightBlock, new DataflowLinkOptions { PropagateCompletion = true });
            
            foreach (int i in Enumerable.Range(0, 10))
            {
                await fork.Target.SendAsync(i);
            }
            fork.Target.Complete();
            await Task.WhenAll(leftBlock.Completion, rightBlock.Completion);

            Assert.That(left, Is.EquivalentTo(new int[] { 0, 1, 2, 3, 4 }));
            Assert.That(right, Is.EquivalentTo(new int[] { 5, 6, 7, 8, 9 }));
        }

        [Test]
        public async Task ItemsAllPassingPredicate()
        {
            var left = new List<int>();
            var right = new List<int>();
            var leftBlock = new ActionBlock<int>(i => left.Add(i));
            var rightBlock = new ActionBlock<int>(i => right.Add(i));

            var fork = new ForkBlock<int>(i => i < 100);
            fork.SourceLeft.LinkTo(leftBlock, new DataflowLinkOptions { PropagateCompletion = true });
            fork.SourceRight.LinkTo(rightBlock, new DataflowLinkOptions { PropagateCompletion = true });            
            
            foreach (int i in Enumerable.Range(0, 10))
            {
                await fork.Target.SendAsync(i);
            }
            fork.Target.Complete();
            await Task.WhenAll(leftBlock.Completion, rightBlock.Completion);

            Assert.That(left, Is.EquivalentTo(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
            Assert.That(right, Is.EquivalentTo(new int[] { }));
        }

        [Test]
        public async Task ItemsAllFailPredicate()
        {
            var left = new List<int>();
            var right = new List<int>();
            var leftBlock = new ActionBlock<int>(i => left.Add(i));
            var rightBlock = new ActionBlock<int>(i => right.Add(i));

            var fork = new ForkBlock<int>(i => i > 100);
            fork.SourceLeft.LinkTo(leftBlock, new DataflowLinkOptions { PropagateCompletion = true });
            fork.SourceRight.LinkTo(rightBlock, new DataflowLinkOptions { PropagateCompletion = true });            

            foreach (int i in Enumerable.Range(0, 10))
            {
                await fork.Target.SendAsync(i);
            }
            fork.Target.Complete();
            await Task.WhenAll(leftBlock.Completion, rightBlock.Completion);

            Assert.That(left, Is.EquivalentTo(new int[] {  }));
            Assert.That(right, Is.EquivalentTo(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        }
    }
}
