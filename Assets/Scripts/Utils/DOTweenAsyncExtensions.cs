using System.Threading.Tasks;
using DG.Tweening;

namespace TestTask.Solitaire.Utils
{
    public static class DOTweenAsyncExtensions
    {
        public static Task AsyncWaitForCompletion(this Tween tween)
        {
            var tcs = new TaskCompletionSource<bool>();

            tween.onComplete += () => tcs.TrySetResult(true);
            tween.onKill += () => tcs.TrySetResult(true);

            return tcs.Task;
        }
    }
}
