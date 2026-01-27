using System;
using UnityEngine;

namespace Asaki.Core.Configs
{
	[Serializable]
	public class AsakiLocalizationConfig
	{
		// =========================================================
		// 运行时设置 (Runtime)
		// =========================================================
		[field: Header("Runtime Settings")]
		[field: SerializeField] public string FallbackLanguage { get; private set; } = "en";

		// =========================================================
		// 编辑器管线设置 (Editor Pipeline)
		// =========================================================
		[field: Header("Editor Build Pipeline")]
		[Tooltip("源文件路径，相对于项目根目录 (Assets/...)")]
		[field: SerializeField] public string RawCsvPath { get; private set; } = "Assets/Editor/RawData/RawLocalization.csv";

		[Tooltip("生成的配置表类名/文件名 (不带.csv后缀)")]
		[field: SerializeField] public string OutputTableName { get; private set; } = "LocalizationTable";

		[Tooltip("生成文件存放的相对路径 (通常在StreamingAssets下)")]
		[field: SerializeField] public string OutputRelativePath { get; private set; } = "Configs";
	}
}
