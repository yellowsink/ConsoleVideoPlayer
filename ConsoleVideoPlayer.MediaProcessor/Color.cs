namespace ConsoleVideoPlayer.MediaProcessor;

internal struct Color
{
	public readonly byte Red   = 0;
	public readonly byte Green = 0;
	public readonly byte Blue  = 0;

	public Color() { }

	public Color(byte red, byte green, byte blue)
	{
		Red   = red;
		Green = green;
		Blue  = blue;
	}

	public static bool operator ==(Color first, Color second)
		=> first.Red == second.Red && first.Blue == second.Blue && first.Green == second.Green;

	public static bool operator !=(Color first, Color second) => !(first == second);
}