using UnityEngine;

namespace SS3D.Engine.Examine
{
    public abstract class AbstractExamineUIElement : MonoBehaviour
    {
        abstract public void LoadExamineData(IExamineData[] data);
		abstract public void RefreshDisplay();
		abstract public ExamineType GetExamineType();
		
		public virtual void DisableElement()
		{
			gameObject.SetActive(false);
		}
    }
}