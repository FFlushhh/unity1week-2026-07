using UnityEngine;

public enum SubjectId
{
    Dog,
    DirtyClothesPerson,
    RabidDog,
    PlasticBag,
    Bird,
    Sparrow,
}

/// <summary>
/// 被写体の識別子、写真の判定点、移動経路を配置する基準点を保持します。
/// </summary>
public sealed class StageSubject : MonoBehaviour
{
    [SerializeField]
    private SubjectId subjectId;

    [SerializeField]
    private int score;

    [SerializeField]
    private Transform judgementPoint;

    [SerializeField]
    private Transform pathAnchor;

    [SerializeField]
    private SpriteRenderer subjectRenderer;

    public SubjectId Id => subjectId;

    public int Score => score;

    public Transform JudgementPoint => judgementPoint;

    public Transform PathAnchor => pathAnchor;

    public SpriteRenderer SubjectRenderer => subjectRenderer;

    /// <summary>
    /// 写真上の前後関係を、実際に描画に使うSpriteRendererの設定から取得する。
    /// </summary>
    public bool TryGetSortingPriority(out int sortingLayerValue, out int sortingOrder)
    {
        sortingLayerValue = default;
        sortingOrder = default;

        if (subjectRenderer == null)
        {
            return false;
        }

        sortingLayerValue = SortingLayer.GetLayerValueFromID(subjectRenderer.sortingLayerID);
        sortingOrder = subjectRenderer.sortingOrder;
        return true;
    }
}
