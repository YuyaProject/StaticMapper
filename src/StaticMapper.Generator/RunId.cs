namespace StaticMapper.Generator;

public static class RunId
{
	private static int _runId;

	public static int GetNextRunId()
	{
		return Interlocked.Increment(ref _runId);
	}
}