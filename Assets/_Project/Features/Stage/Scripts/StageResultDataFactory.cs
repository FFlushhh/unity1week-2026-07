using System;
using System.Collections.Generic;
using ResultScene;

/// <summary>
/// Stageが記録した撮影結果を、Resultが受け取る表示用データへ変換します。
/// </summary>
public static class StageResultDataFactory
{
    public static ResultData Create(
        CapturedPhoto capturedPhoto,
        string playerName,
        string locationName
    )
    {
        if (capturedPhoto == null)
        {
            throw new ArgumentNullException(nameof(capturedPhoto));
        }

        return new ResultData
        {
            PlayerName = playerName,
            LocationName = locationName,
            CapturedImage = capturedPhoto.Image,
            Bonuses = CreateBonuses(capturedPhoto),
        };
    }

    private static List<BonusInputData> CreateBonuses(CapturedPhoto capturedPhoto)
    {
        var bonuses = new List<BonusInputData>();
        AddBonusIfCaptured(bonuses, capturedPhoto, SubjectId.Dog, "犬");
        AddBonusIfCaptured(bonuses, capturedPhoto, SubjectId.DirtyClothesPerson, "汚れた服の人");
        AddBonusIfCaptured(bonuses, capturedPhoto, SubjectId.RabidDog, "狂犬");
        AddBonusIfCaptured(bonuses, capturedPhoto, SubjectId.PlasticBag, "ビニール袋");
        AddBonusIfCaptured(bonuses, capturedPhoto, SubjectId.Bird, "ハト");
        AddBonusIfCaptured(bonuses, capturedPhoto, SubjectId.Sparrow, "青鳥");
        AddBonusIfCaptured(bonuses, capturedPhoto, SubjectId.SelfieGirl, "自撮り");
        return bonuses;
    }

    private static void AddBonusIfCaptured(
        ICollection<BonusInputData> bonuses,
        CapturedPhoto capturedPhoto,
        SubjectId subjectId,
        string bonusName
    )
    {
        var count = capturedPhoto.GetSubjectCount(subjectId);
        if (count <= 0)
        {
            return;
        }

        bonuses.Add(new BonusInputData { BonusName = bonusName, Count = count });
    }
}
