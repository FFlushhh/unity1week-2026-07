using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シャッター時点の写真と、写真に写った被写体の集計を保持します。
/// </summary>
public sealed class CapturedPhoto
{
    private readonly Dictionary<SubjectId, int> subjectCounts = new();

    public CapturedPhoto(Texture2D image, IEnumerable<StageSubject> subjects)
    {
        Image = image;

        if (subjects == null)
        {
            return;
        }

        foreach (var subject in subjects)
        {
            if (subject == null)
            {
                continue;
            }

            subjectCounts.TryGetValue(subject.Id, out var count);
            subjectCounts[subject.Id] = count + 1;
        }
    }

    public Texture2D Image { get; }

    public IReadOnlyDictionary<SubjectId, int> SubjectCounts => subjectCounts;

    public int GetSubjectCount(SubjectId subjectId)
    {
        return subjectCounts.TryGetValue(subjectId, out var count) ? count : 0;
    }
}
