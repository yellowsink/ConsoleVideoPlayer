namespace ConsoleVideoPlayer.MediaProcessor;

public static class CollectionTools
{
	public static T[][] Split<T>(this T[] arr, int amount)
	{
		var actualLength = Math.Min(amount, arr.Length);
		var lengths          = new int[actualLength];

		for (var i = 0; i < arr.Length; i++) lengths[i % actualLength]++;

		var res = new T[]?[actualLength];
		for (var i = 0; i < arr.Length; i++)
		{
			if (res[i % actualLength] != null) 
				res[i % actualLength]![i / actualLength] = arr[i];
			else
			{
				res[i % actualLength] = new T[lengths[i % actualLength]];

				res[i % actualLength]![0] = arr[i];
			}
		}

		return res!;
	}
}