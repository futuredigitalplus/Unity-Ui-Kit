// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Extras
{
	public class BlurPanel : Image
	{
#pragma warning disable CS0618 // FutureDigitalPlus: UxmlFactory/UxmlTraits are deprecated in Unity 6 but still work; kept so upstream code stays diffable (see SOURCE.md)
		public new class UxmlFactory : UxmlFactory<BlurPanel, UxmlTraits> { }

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription { get { yield break; } }
		}
#pragma warning restore CS0618

		private VisualElement rootParent;

		// ------------------------------------------------------------------------------------------------------------

		public BlurPanel()
			: base()
		{
			RegisterCallback<GeometryChangedEvent>(UpdateImageRect);
		}

		public void SetImage(Texture img, VisualElement geometryChangedWatch = null)
		{
			// this is to get events when a parent panel is animated.
			// registering to this element alone for that event was not enough.
			geometryChangedWatch?.RegisterCallback<GeometryChangedEvent>(UpdateImageRect);

			image = img;
			scaleMode = ScaleMode.StretchToFill;
			UpdateImageRect(null);
		}

		private void UpdateImageRect(GeometryChangedEvent ev)
		{
			if (image == null)
			{
				return;
			}

			if (rootParent == null)
			{
				rootParent = this.GetDocumentRoot();
			}

			// FutureDigitalPlus: skip while the document root has no size yet (division by zero gave a non finite sourceRect); the next GeometryChangedEvent retries. See SOURCE.md.
			if (!(rootParent.worldBound.width > 0f) || !(rootParent.worldBound.height > 0f))
			{
				return;
			}

			float rw = image.width / rootParent.worldBound.width;
			float rh = image.height / rootParent.worldBound.height;

			float x = worldBound.x * rw;
			float y = worldBound.y * rh;
			float w = worldBound.width * rw;
			float h = worldBound.height * rh;

			sourceRect = new Rect(x, y, w, h);

			MarkDirtyRepaint();
		}

		// ============================================================================================================
	}
}