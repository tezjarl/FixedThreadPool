using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FixedThreadPool
{
	public class FixedThreadPool
	{
		private bool _isStopped;
		private readonly int _workCount;
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private readonly ConcurrentDictionary<ITask, Task> _executingTasks = new ConcurrentDictionary<ITask, Task>();
		private readonly ConcurrentQueue<ITask> _highPriorityTasks = new ConcurrentQueue<ITask>();
		private readonly ConcurrentQueue<ITask> _normalPriorityTasks = new ConcurrentQueue<ITask>();
		private readonly ConcurrentQueue<ITask> _lowPriorityTasks = new ConcurrentQueue<ITask>();
		private readonly TimeSpan _spinTimeout = TimeSpan.FromMilliseconds(5);
		private readonly TimeSpan _finishTasksTimeout = TimeSpan.FromSeconds(20);
		private const int HighTaskRatio = 3;
		private const int NormalTaskRatio = 1;
		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="workCount"> Количество потоков для обработки задач</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public FixedThreadPool(int workCount)
		{
			if (workCount <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(workCount));
			}

			_workCount = workCount;
			var cancellationToken = _cancellationTokenSource.Token;
			Task.Run(async () => await ScheduleTaskAsync(cancellationToken), cancellationToken);
		}
		/// <summary>
		/// Ставит задачу на выполнение с указанным приоритетом
		/// </summary>
		/// <param name="task">Задача</param>
		/// <param name="priority">Приоритет</param>
		/// <returns>Флаг, показывающий была ли задача поставлена в очередь на выполнение</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidEnumArgumentException"></exception>
		public bool Execute(ITask task, Priority priority)
		{
			if (task == null)
			{
				throw new ArgumentNullException(nameof(task));
			}

			if (_isStopped)
			{
				return false;
			}

			switch (priority)
			{
				case Priority.HIGH:
					_highPriorityTasks.Enqueue(task);
					break;
				case Priority.NORMAL:
					_normalPriorityTasks.Enqueue(task);
					break;
				case Priority.LOW:
					_lowPriorityTasks.Enqueue(task);
					break;
				default:
					throw new ArgumentException("Указаное значение приоритета не поддерживается",nameof(priority));
			}

			return true;
		}

		public void Stop()
		{
			_isStopped = true;
			Task.WaitAll(_executingTasks.Values.ToArray(), _finishTasksTimeout);
			_cancellationTokenSource.Cancel();
		}

		private async Task ScheduleTaskAsync(CancellationToken cancellationToken)
		{
			var executedHighTaskCount = 0;
			var normalTasksToExecute = 0;

			while (!cancellationToken.IsCancellationRequested)
			{
				if (AreThereScheduledTasks() &&
				    !IsWorkCapacityReached())
				{
					var isExecutionOfNormalTaskAllowed =
						executedHighTaskCount >= HighTaskRatio && normalTasksToExecute < NormalTaskRatio;

					if (_highPriorityTasks.Any())
					{
						var couldHighTaskBeExecuted = !_normalPriorityTasks.Any() || !isExecutionOfNormalTaskAllowed;
						if (couldHighTaskBeExecuted &&
						    TryExecuteTaskFromQueue(_highPriorityTasks))
						{
							executedHighTaskCount++;
							continue;
						}
					}

					if (_normalPriorityTasks.Any())
					{
						var couldNormalTaskBeExecuted = !_highPriorityTasks.Any() || isExecutionOfNormalTaskAllowed;
						if (couldNormalTaskBeExecuted &&
						    TryExecuteTaskFromQueue(_normalPriorityTasks))
						{
							normalTasksToExecute++;
						}

						if (normalTasksToExecute >= NormalTaskRatio)
						{
							executedHighTaskCount = 0;
						}

						continue;
					}

					if (_lowPriorityTasks.Any())
					{
						var couldLowTaskBeExecuted = !_highPriorityTasks.Any() && !_normalPriorityTasks.Any();
						if (couldLowTaskBeExecuted)
						{
							TryExecuteTaskFromQueue(_lowPriorityTasks);
						}
					}
				}
				else
				{
					await Task.Delay(_spinTimeout, cancellationToken);
				}
			}
		}

		private bool IsWorkCapacityReached() => _executingTasks.Count > _workCount;

		private bool AreThereScheduledTasks() => _highPriorityTasks.Any() || _normalPriorityTasks.Any() || _lowPriorityTasks.Any();

		private bool TryExecuteTaskFromQueue(ConcurrentQueue<ITask> queue)
		{
			if (queue.TryDequeue(out var task))
			{
				ExecuteTask(task);
				return true;
			}

			return false;
		}

		private void ExecuteTask(ITask task)
		{
			if (_executingTasks.TryAdd(task, new Task(task.Execute)))
			{
				Task.Run(() => task.Execute());
				_executingTasks.TryRemove(task, out _);
			}
		}


	}
}