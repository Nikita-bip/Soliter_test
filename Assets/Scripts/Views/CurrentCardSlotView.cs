using TestTask.Solitaire.Models;
using UnityEngine;
using UnityEngine.UI;

namespace TestTask.Solitaire.Views
{
    public sealed class CurrentCardSlotView : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private CardSpriteLibrary spriteLibrary;

        public void SetSpriteLibrary(CardSpriteLibrary library)
        {
            spriteLibrary = library;
        }

        public void Show(CardDescriptor descriptor)
        {
            if (image == null || spriteLibrary == null)
            {
                return;
            }

            image.sprite = spriteLibrary.GetFace(descriptor);
            image.gameObject.SetActive(true);
        }
    }
}
