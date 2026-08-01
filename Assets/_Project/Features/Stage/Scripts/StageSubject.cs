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
/// 被写体の識別子と、写真に写ったときの加減点を保持します。
/// </summary>
public sealed class StageSubject : MonoBehaviour
{
    [SerializeField]
    private SubjectId subjectId;

    [SerializeField]
    private int score;

    public SubjectId Id => subjectId;

    public int Score => score;
}
