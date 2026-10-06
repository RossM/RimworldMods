namespace XylXenos;

[UsedFromXml]
public class ColorGenerator_Spectrum : ColorGenerator
{
    public override Color ExemplaryColor => colors[0];
    public required List<Color> colors;

    public override Color NewRandomizedColor()
    {
        if (colors.Count == 1)
            return colors[0];

        int index = Rand.Range(0, colors.Count - 2);
        float fraction = Rand.Value;
        return Color.Lerp(colors[index], colors[index + 1], fraction);
    }
}
