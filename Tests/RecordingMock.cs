using System.Collections.Concurrent;
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
			Calls.Enqueue(_priority);
		}
	}
}