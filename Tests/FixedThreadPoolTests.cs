using System.Threading;
using FixedThreadPool;
using Moq;
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
			var pool = new FixedThreadPool.FixedThreadPool(1);
			var lowTask = new Mock<ILowPriorityTask>();
			
			pool.Stop();
			var taskAddToQueue = pool.Execute(lowTask.Object, Priority.LOW);
			
			Assert.That(taskAddToQueue, Is.False);
		}
		
		[Test]
		public void FixedThreadPool_StopPoolWithExecutingTask_WaitForTaskIsFinished()
		{
			var pool = new FixedThreadPool.FixedThreadPool(1);
			var lowTask = new Mock<ILowPriorityTask>();

			pool.Execute(lowTask.Object, Priority.LOW);
			Thread.Sleep(100);
			pool.Stop();
			
			lowTask.Verify(l=>l.Execute(), Times.Once);
		}
		
		[Test]
		public void FixedThreadPool_AddOneTask_SuccesfullyAdded()
		{
			var pool = new FixedThreadPool.FixedThreadPool(1);
			var lowTask = new Mock<ILowPriorityTask>();

			var taskAddToQueue = pool.Execute(lowTask.Object, Priority.LOW);
			
			Assert.That(taskAddToQueue, Is.True);
		}
		
		[Test]
		public void FixedThreadPool_AddLowAndNormalPrioritiesTasks_LowExecutedAfterNormal()
		{
			var pool = new FixedThreadPool.FixedThreadPool(10);
			var lowTask = new Mock<ILowPriorityTask>(MockBehavior.Strict);
			var normalTask = new Mock<INormalPriorityTask>(MockBehavior.Strict);
			var sequence = new MockSequence();
			
			normalTask.InSequence(sequence).Setup(n => n.Execute());
			lowTask.InSequence(sequence).Setup(l => l.Execute());
			
			pool.Execute(lowTask.Object, Priority.LOW);
			pool.Execute(normalTask.Object, Priority.NORMAL);
			Thread.Sleep(100);

			lowTask.Verify(l=>l.Execute(), Times.Once);
			normalTask.Verify(n=>n.Execute(), Times.Once);
		}
		
		[Test]
		public void FixedThreadPool_AddHighNormalAndLowPrioritiesTasks_ExecutedInCorrectOrder()
		{
			var pool = new FixedThreadPool.FixedThreadPool(10);
			var lowTask = new Mock<ILowPriorityTask>(MockBehavior.Strict);
			var normalTask = new Mock<INormalPriorityTask>(MockBehavior.Strict);
			var highTask = new Mock<IHighPriorityTask>(MockBehavior.Strict);
			var sequence = new MockSequence();

			highTask.InSequence(sequence).Setup(h => h.Execute());
			normalTask.InSequence(sequence).Setup(n => n.Execute());
			lowTask.InSequence(sequence).Setup(l => l.Execute());
			
			pool.Execute(lowTask.Object, Priority.LOW);
			pool.Execute(highTask.Object, Priority.HIGH);
			pool.Execute(normalTask.Object, Priority.NORMAL);
			Thread.Sleep(100);

			lowTask.Verify(l=>l.Execute(), Times.Once);
			normalTask.Verify(n=>n.Execute(), Times.Once);
			highTask.Verify(h=>h.Execute(), Times.Once);
		}

		[Test]
		public void FixedThreadPool_AddMultipleHignAndNormalTasks_ExecutedWithRatio3HighTo1Normal()
		{
			var pool = new FixedThreadPool.FixedThreadPool(10);
			var expectedExecutionOrder = new[] {"High", "High", "High", "Normal", "High", "High"};

			pool.Execute(new RecordingMock("High"), Priority.HIGH);
			pool.Execute(new RecordingMock("High"), Priority.HIGH);
			pool.Execute(new RecordingMock("High"), Priority.HIGH);
			pool.Execute(new RecordingMock("High"), Priority.HIGH);
			pool.Execute(new RecordingMock("High"), Priority.HIGH);
			pool.Execute(new RecordingMock("Normal"), Priority.NORMAL);
			Thread.Sleep(500);
			
			Assert.That(RecordingMock.Calls.ToArray(), Is.EqualTo(expectedExecutionOrder));
		}
		
		[Test]
		public void FixedThreadPool_SetWorkerCountLowerThanTaskAmount_ExecutedCorrectly()
		{
			var pool = new FixedThreadPool.FixedThreadPool(1);
			var lowTask = new Mock<ILowPriorityTask>(MockBehavior.Strict);
			var normalTask = new Mock<INormalPriorityTask>(MockBehavior.Strict);
			var sequence = new MockSequence();
			
			lowTask.InSequence(sequence).Setup(l => l.Execute());
			normalTask.InSequence(sequence).Setup(n => n.Execute());

			pool.Execute(lowTask.Object, Priority.LOW);
			pool.Execute(normalTask.Object, Priority.NORMAL);
			Thread.Sleep(100);

			lowTask.Verify(l=>l.Execute(), Times.Once);
			normalTask.Verify(n=>n.Execute(), Times.Once);
		}

		[TearDown]
		public void TearDown()
		{
			RecordingMock.Calls.Clear();
		}
	}
}