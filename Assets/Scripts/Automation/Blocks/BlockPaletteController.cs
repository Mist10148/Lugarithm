using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Block Mode palette: one button per block the current level allows
/// (LevelDefinition.allowedBlocks). Clicking inserts at the canvas cursor.
/// </summary>
public class BlockPaletteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private Button buttonTemplate;

    // -------------------------------------------------------------------------

    public void Init(string[] allowedBlocks, BlockCanvasController canvas)
    {
        if (allowedBlocks == null || canvas == null || buttonTemplate == null) return;

        foreach (string name in allowedBlocks)
        {
            BlockType? type = BlockProgram.FromPaletteName(name);
            if (!type.HasValue) continue;

            BlockType blockType = type.Value;
            Button button = Instantiate(buttonTemplate, content);
            button.gameObject.SetActive(true);

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = PaletteLabel(blockType);

            var face = button.targetGraphic as Image;
            if (face != null)
                face.color = IsContainer(blockType)
                    ? new Color(0.34f, 0.26f, 0.46f)
                    : new Color(0.22f, 0.30f, 0.42f);

            button.onClick.AddListener(() => canvas.InsertBlock(blockType));
        }
    }

    // -------------------------------------------------------------------------

    static bool IsContainer(BlockType type) =>
        type == BlockType.If || type == BlockType.IfElse || type == BlockType.While;

    static string PaletteLabel(BlockType type)
    {
        switch (type)
        {
            case BlockType.While:  return "while …:";
            case BlockType.If:     return "if …:";
            case BlockType.IfElse: return "if …: else:";
            default:               return BlockProgram.ActionName(type) + "()";
        }
    }
}
