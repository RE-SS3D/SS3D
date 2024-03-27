// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2023 Kybernetik //

using UnityEngine;

namespace InspectorGadgets
{
    /// <summary>Holds a text comment as a <see cref="ScriptableObject"/> asset which can be viewed and edited in the inspector.</summary>
    /// <remarks>By default, this script sets itself to be excluded from the build.</remarks>
    [CreateAssetMenu(menuName = "Comment", fileName = "Comment", order = 26)]// Group with "Folder".
    [HelpURL(Strings.APIDocumentationURL + "/" + nameof(CommentAsset))]
    public sealed class CommentAsset : ScriptableObject, IComment
    {
        /************************************************************************************************************************/

        [SerializeField, TextArea]
        private string _Text;

        string IComment.TextFieldName
            => nameof(_Text);

        /// <summary>[<see cref="SerializeField"/>] [<see cref="IComment"/>] The text of this comment.</summary>
        public string Text
        {
            get => _Text;
            set => _Text = value;
        }

        /************************************************************************************************************************/

        /// <summary>False if this script is set to <see cref="HideFlags.DontSaveInBuild"/>.</summary>
        public bool IncludeInBuild
        {
            get => (hideFlags &= HideFlags.DontSaveInBuild) == 0;
            set
            {
                if (value)
                    hideFlags &= ~HideFlags.DontSaveInBuild;
                else
                    hideFlags |= HideFlags.DontSaveInBuild;
            }
        }

        /************************************************************************************************************************/

        private void Reset()
        {
            IncludeInBuild = false;
        }

        /************************************************************************************************************************/
    }
}

