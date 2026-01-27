using Asaki.Core.Context;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.UI
{
	public interface IAsakiUIService : IAsakiModule
	{
		UniTask<T> OpenAsync<T>(int uiId, object args = null, CancellationToken token = default(CancellationToken))
			where T : class, IAsakiWindow;
		void Close<T>() where T : IAsakiWindow;
		void Close(IAsakiWindow window);
		void Back();
		void BackTo<T>() where T : IAsakiWindow;
		void BackTo(int uiId);
		UniTask Back(object returnValue);
		void ClearStack(bool includePopup = false);

		UniTask<T> ReplaceAsync<T>(int uiId, object args = null, CancellationToken token = default(CancellationToken)) where T : class, IAsakiWindow;
		// 查询
		bool IsOpened(int uiId);
		T GetWindow<T>() where T : class, IAsakiWindow;
		IAsakiWindow GetWindow(int uiId);
		IReadOnlyList<IAsakiWindow> GetOpenedWindows(AsakiUILayer? layer = null);
		bool HasPopup();
		int GetActiveWindowCount(AsakiUILayer layer);
	}
}
