using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Block Mode palette: one button per block the current level allows
/// (LevelDefinition.allowedBlocks). Drag a button onto the canvas to drop a new
/// block at a slot; clicking it appends to the end as a fallback.
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
                face.color = BlockCanvasController.CategoryColor(blockType);

            button.gameObject.AddComponent<PaletteDragSource>().Setup(canvas, blockType);
            button.onClick.AddListener(() => canvas.InsertBlock(blockType));
        }
    }

    // -------------------------------------------------------------------------

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
