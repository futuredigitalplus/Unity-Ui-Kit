// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras
{
	public static class UIElementsExtensions
	{
		public static VisualElement GetDocumentRoot(this VisualElement ele)
		{
			return (ele.parent == null ? ele : ele.parent.GetDocumentRoot());
		}

		// ============================================================================================================
	}
}