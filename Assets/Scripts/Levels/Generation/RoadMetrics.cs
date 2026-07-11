/// <summary>
/// Single source of truth for road geometry the gameplay layer must agree on
/// with the art. Scene-template worlds use the painted road (288 px at 24 PPU
/// = 12 world units wide, two lanes with centers at ±3 — see
/// art_src/driving/template_table.json "road_width"). Legacy placeholder-tile
/// worlds keep the original tuning.
/// </summary>
public static class RoadMetrics
{
    public const float SceneRoadHalfWidth = 6f;
    public const float SceneLaneOffset    = 3f;

    public const float PlaceholderRoadHalfWidth = 3f;
    public const float PlaceholderLaneOffset    = 1.35f;
}
