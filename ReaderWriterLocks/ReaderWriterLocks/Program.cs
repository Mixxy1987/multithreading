using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ReaderWriterLocks
{
	public class RWMeasure
    {
		private const int Count = 100000;
		private const int ReadersCount = 5;
		private const int WritersCount = 1;
		private const int ReadPayload = 100;
		private const int WritePayload = 100;

		[Benchmark]
        public void MeasureSimpleLock()
        {
            Measure(SimpleLockReader, SimpleLockWriter);
        }

		[Benchmark]
		public void MeasureRWLock()
        {
			Measure(RWLockReader, RWLockWriter);
		}

        [Benchmark]
		public void MeasureRWSlimLock()
        {
			Measure(RWLockSlimReader, RWLockSlimWriter);
		}
		#region Tests common
		private static readonly Dictionary<int, string> Map = new Dictionary<int, string>();

		private static void ReaderProc()
		{
			Map.TryGetValue(Environment.TickCount % Count, out string val);
			// Do some work
			Thread.SpinWait(ReadPayload);
		}

		private static void WriterProc()
		{
			var n = Environment.TickCount % Count;
			// Do some work
			Thread.SpinWait(WritePayload);
			Map[n] = n.ToString();
		}

		private static void Measure(Action reader, Action writer)
		{
			var threads =
				Enumerable
					.Range(0, ReadersCount)
					.Select(
						n => new Thread(
							() =>
							{
								for (int i = 0; i < Count; i++)
									reader();
							}))
					.Concat(
						Enumerable
							.Range(0, WritersCount)
							.Select(
								n => new Thread(
									() =>
									{
										for (int i = 0; i < Count; i++)
											writer();
									})))
					.ToArray();
			Map.Clear();
			foreach (var thread in threads)
				thread.Start();
			foreach (var thread in threads)
				thread.Join();
		}
		#endregion

		#region Simple lock
		private static readonly object SimpleLockLock = new object();

		private static void SimpleLockReader()
		{
			lock (SimpleLockLock)
				ReaderProc();
		}

		private static void SimpleLockWriter()
		{
			lock (SimpleLockLock)
				WriterProc();
		}
		#endregion

		#region ReaderWriterLock
		private static readonly ReaderWriterLock RwLock = new ReaderWriterLock();

		private static void RWLockReader()
		{
			RwLock.AcquireReaderLock(-1);
			try
			{
				ReaderProc();
			}
			finally
			{
				RwLock.ReleaseReaderLock();
			}
		}

		private static void RWLockWriter()
		{
			RwLock.AcquireWriterLock(-1);
			try
			{
				WriterProc();
			}
			finally
			{
				RwLock.ReleaseWriterLock();
			}
		}
		#endregion

		#region ReaderWriterLockSlim
		private static readonly ReaderWriterLockSlim RwLockSlim = new ReaderWriterLockSlim();

		private static void RWLockSlimReader()
		{
			RwLockSlim.EnterReadLock();
			try
			{
				ReaderProc();
			}
			finally
			{
				RwLockSlim.ExitReadLock();
			}
		}

		private static void RWLockSlimWriter()
		{
			RwLockSlim.EnterWriteLock();
			try
			{
				WriterProc();
			}
			finally
			{
				RwLockSlim.ExitWriteLock();
			}
		}
        #endregion
	}


	class Program
    {
		static void Main()
		{
            BenchmarkRunner.Run<RWMeasure>();
		}
	}
}
