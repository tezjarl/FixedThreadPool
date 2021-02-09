using System.Collections.Concurrent;
using System.Threading;
using FixedThreadPool;

namespace Tests
{
	public class RecordingMock : ITask
	{
		private readonly string _priority;
		public static ConcurrentQueue<string> Calls = new ConcurrentQueue<string>();

		public RecordingMock(string Priority)
		{
			_priority = Priority;
		}
		public void Execute()
		{
			//Thread.Sleep(30);
			Calls.Enqueue(_priority);
		}
	}
}