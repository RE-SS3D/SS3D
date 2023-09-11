﻿using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class ObjectReferencePickerItem : RecycledListItem
	{
#pragma warning disable 0649
		[SerializeField]
		private Image background;

		[SerializeField]
		private RawImage texturePreview;
		private LayoutElement texturePreviewLayoutElement;

		[SerializeField]
		private Text referenceNameText;
#pragma warning restore 0649

		public object Reference { get; private set; }

		private int m_skinVersion = 0;
		private UISkin m_skin;
		public UISkin Skin
		{
			get { return m_skin; }
			set
			{
				if( m_skin != value || m_skinVersion != m_skin.Version )
				{
					m_skin = value;

					( (RectTransform) transform ).sizeDelta = new Vector2( 0f, Skin.LineHeight );

					int previewDimensions = Mathf.Max( 5, Skin.LineHeight - 7 );
					texturePreviewLayoutElement.SetWidth( previewDimensions );
					texturePreviewLayoutElement.SetHeight( previewDimensions );

					referenceNameText.SetSkinText( m_skin );

					IsSelected = m_isSelected;
				}
			}
		}

		private bool m_isSelected = false;
		public bool IsSelected
		{
			get { return m_isSelected; }
			set
			{
				m_isSelected = value;

				if( m_isSelected )
				{
					background.color = Skin.SelectedItemBackgroundColor;
					referenceNameText.color = Skin.SelectedItemTextColor;
				}
				else
				{
					background.color = Color.clear;
					referenceNameText.color = Skin.TextColor;
				}
			}
		}

		private void Awake()
		{
			texturePreviewLayoutElement = texturePreview.GetComponent<LayoutElement>();
			GetComponent<PointerEventListener>().PointerClick += ( eventData ) => OnClick();
		}

		public void SetContent( object reference, string displayName )
		{
			Reference = reference;
			referenceNameText.text = displayName;

			Texture previewTex = ( reference as Object ).GetTexture();
			if( previewTex != null )
			{
				texturePreview.gameObject.SetActive( true );
				texturePreview.texture = previewTex;
			}
			else
				texturePreview.gameObject.SetActive( false );
		}
	}
}