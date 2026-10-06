namespace Xylib;

[UsedFromXml]
[PublicAPI]
public class GeneCompProperties_RenderNodeModifiers : GeneCompProperties
{
    public Vector3 headOffset = Vector3.zero;
    public float headScale = 1f;
    public Vector3 bodyOffset = Vector3.zero;
    public float bodyScale = 1f;

    public List<BodyTypeGraphicData>? bodyTypeGraphicPaths;

    public Dictionary<BodyTypeDef, Graphic>? bodyTypeGraphics;

    public GeneCompProperties_RenderNodeModifiers()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (bodyTypeGraphicPaths == null)
                return;

            bodyTypeGraphics = [];
            foreach (var data in bodyTypeGraphicPaths)
                bodyTypeGraphics[data.bodyType] = GraphicDatabase.Get<Graphic_Multi>(data.texturePath);
        });
    }
}
