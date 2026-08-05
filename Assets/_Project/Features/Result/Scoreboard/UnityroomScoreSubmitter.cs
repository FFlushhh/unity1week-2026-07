using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unityroom.Client;

namespace ResultScene
{
    /// <summary>
    /// リザルトで確定したいいねスコアを、unityroomのスコアボードへ送信します。
    /// </summary>
    public sealed class UnityroomScoreSubmitter : MonoBehaviour
    {
        [SerializeField]
        private string hmacKey;

        [SerializeField]
        private int scoreboardId;

        private bool isSending;

        public async UniTask SendScoreAsync(int score, CancellationToken cancellationToken)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // 送信中に連続で呼ばれても多重送信しない（unityroom-sdkは同時実行数に制限があるため）。
            if (isSending)
            {
                return;
            }

            isSending = true;
            try
            {
                using var client = new UnityroomClient { HmacKey = hmacKey };
                await client.Scoreboards.SendAsync(
                    new SendScoreRequest
                    {
                        ScoreboardId = scoreboardId,
                        Score = ClampScoreForSubmission(score),
                    },
                    cancellationToken
                );
            }
            catch (UnityroomApiException ex)
            {
                Debug.LogError(
                    $"[UnityroomScoreSubmitter] スコア送信に失敗しました。ErrorCode={ex.ErrorCode}, ErrorType={ex.ErrorType}"
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[UnityroomScoreSubmitter] スコア送信中に想定外の例外が発生しました: {ex}"
                );
            }
            finally
            {
                isSending = false;
            }
#else
            await UniTask.CompletedTask;
            Debug.Log(
                "[UnityroomScoreSubmitter] WebGLビルド以外の実行環境のため、unityroomへのスコア送信をスキップします。"
            );
#endif
        }

        /// <summary>
        /// 表示用のいいね数（0未満にしない）をそのまま送信スコアとして使う。
        /// </summary>
        public static float ClampScoreForSubmission(int rawScore)
        {
            return Mathf.Max(0, rawScore);
        }
    }
}
