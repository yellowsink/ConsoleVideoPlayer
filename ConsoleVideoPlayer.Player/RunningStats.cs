using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleVideoPlayer.Player;

public record struct RunningStats(int Count = 0, long Mean = 0, long Max = 0, int Debted = 0, int Dropped = 0)
{
	public void Add(long amount)
	{
		Max  = Math.Max(Max, amount);
		Mean = (amount + Mean * Count) / ++Count;
		if (amount != 0) Debted++;
	}

	public void AddDropped() => Dropped++;

	public int    FullCount      => Count + Dropped;
	public double DroppedPercent => (double) Dropped * 100 / FullCount;
	public double DebtedPercent  => (double) Debted  * 100 / Count;


	private static readonly int PadLength = long.MaxValue.ToString().Length;
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string FormatLong(long val) => val.ToString().PadLeft(PadLength, '0');
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[SuppressMessage("ReSharper", "SpecifyACultureInStringConversionExplicitly")]
	private static string FormatPercent(double val) => Math.Round(val, 2).ToString().PadLeft(3, '0');

	public string Render(long current)
	{
		var debugInfo = new StringBuilder("\u001b[32;40m");
					
		debugInfo.Append("FRAME     | CURR: ");
		debugInfo.Append(FullCount.ToString());
		debugInfo.Append(" | DROPPED: ");
		debugInfo.Append(Dropped.ToString());
		debugInfo.Append(" | DROPPED %: ");
		debugInfo.Append(FormatPercent(DroppedPercent));
		debugInfo.Append(" | DEBTED: ");
		debugInfo.Append(Debted.ToString());
		debugInfo.Append(" | DEBTED %: ");
		debugInfo.AppendLine(FormatPercent(DebtedPercent));

		debugInfo.Append("TIME DEBT | CURR: ");
		debugInfo.Append(FormatLong(current));
		debugInfo.Append(" | MEAN: ");
		debugInfo.Append(FormatLong(Mean));
		debugInfo.Append(" | MAX: ");
		debugInfo.Append(FormatLong(Max));

		return debugInfo.ToString();
	}
}