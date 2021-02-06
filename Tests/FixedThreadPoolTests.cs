using System.Reflection.Metadata;
using FixedThreadPool;
using Moq;
using Moq.Sequences;
using NUnit.Framework;

namespace Tests
{
	public interface IHighPriorityTask : ITask { }
	public interface INormalPriorityTask : ITask { }
	public interface ILowPriorityTask : ITask { }

	public class FixedThreadPoolTests
	{
		[Test]
		public void FixedThreadPool_StopEmptyPool_CannotAddNewTaskAfterStop()
		{
			Assert.Pass();
		}
		
		[Test]
		public void FixedThreadPool_StopPoolWithTask_WaitForTaskIsFinished()
		{
			Assert.Pass();
		}
		
		[Test]
		public void FixedThreadPool_AddOneLowPriorityTask_ExecutedCorrectly()
		{
			Assert.Pass();
		}
		
		[Test]
		public void FixedThreadPool_AddLowAndNormalPrioritiesTasks_LowExecutedAfterNormal()
		{
			Assert.Pass();
		}
		
		[Test]
		public void FixedThreadPool_AddHighNormalAndLowPrioritiesTasks_ExecutedInCorrectOrder()
		{
			Assert.Pass();
		}
		
		[Test]
		public void FixedThreadPool_AddSixHighAndTwoLowPrioritiesTasks_ExecutedInCorrectOrder()
		{
			Assert.Pass();
		}
	}
}