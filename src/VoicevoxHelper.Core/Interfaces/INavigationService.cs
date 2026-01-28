namespace VoicevoxHelper.Core.Interfaces;

/// <summary>
/// 画面遷移サービス。
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// ナビゲーション対象のフレームを設定する。
    /// </summary>
    /// <param name="frame">フレームオブジェクト。</param>
    void SetFrame(object frame);

    /// <summary>
    /// 指定されたページ型へ遷移する。
    /// </summary>
    /// <param name="pageType">ページ型。</param>
    /// <returns>遷移に成功した場合true。</returns>
    bool Navigate(Type pageType);

    /// <summary>
    /// 指定されたページ型へ遷移する。
    /// </summary>
    /// <typeparam name="TPage">ページ型。</typeparam>
    /// <returns>遷移に成功した場合true。</returns>
    bool Navigate<TPage>() where TPage : class;
}
