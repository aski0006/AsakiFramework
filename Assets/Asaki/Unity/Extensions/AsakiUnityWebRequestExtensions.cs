using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Asaki.Unity.Extensions
{
    public static class AsakiUnityWebRequestExtensions
    {
        public static Task SendWebRequestAsTask(this UnityWebRequest uwr)
        {
            var tcs = new TaskCompletionSource<bool>();

            // UnityWebRequestAsyncOperation 是一个 AsyncOperation
            UnityWebRequestAsyncOperation op = uwr.SendWebRequest();

            op.completed += (asyncOp) =>
            {
                // 无论成功还是失败，只要请求结束，Task 就结束
                // 具体的成功/失败由 uwr.result 判断
                tcs.SetResult(true);
            };

            return tcs.Task;
        }
    }
}
