using System.Text;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using SS3D.Engine.Examine;

namespace SS3D.Content.Systems.Examine.UI
{
    public class UIIdentificationCard : AbstractExamineUIElement
    {
        public Transform Panel;
        public TMP_Text FirstName;
        public TMP_Text Surname;
        public TMP_Text Age;
        public TMP_Text Species;
        public TMP_Text Classifier;
        public TMP_Text Title;
        public TMP_Text Gender;
        public TMP_Text Expiry;
		public Image Mugshot;

		private RectTransform rectTransform;
        private string firstName;
        private string surname;
        private string age;
        private string species;
        private string classifier;
        private string title;
        private string gender;
        private string expiry;
		private Sprite mugshot;
		
		public void Start()
		{
			rectTransform = GetComponent<RectTransform>();
		}
		
        public override void RefreshDisplay()
        {
            FirstName.text = firstName;
            Surname.text = surname;
            Age.text = age;
            Species.text = species;
            Classifier.text = classifier;
            Title.text = title;
            Gender.text = gender;
            Expiry.text = expiry;
			Mugshot.sprite = mugshot;
			rectTransform = GetComponent<RectTransform>();
			Panel.position = new Vector3(Input.mousePosition.x + 5f, Input.mousePosition.y + 5f, 0);
        }
		
		public override void LoadExamineData(IExamineData[] data)
		{
			DataIdentificationCard currentExaminable;

			foreach (IExamineData examineData in data)
            {
				currentExaminable = examineData as DataIdentificationCard;
				if (currentExaminable != null)
				{
					gameObject.SetActive(true);
					firstName = currentExaminable.GetFirstName();
					surname = currentExaminable.GetSurname();
					age = currentExaminable.GetAge().ToString();
					species = currentExaminable.GetSpecies();
					classifier = currentExaminable.GetClassifier().ToString();
					title = currentExaminable.GetTitle();
					gender = currentExaminable.GetGender();
					expiry = currentExaminable.GetExpiry().ToString("m");
					mugshot = currentExaminable.GetMugshot();
				}
            }	
			RefreshDisplay();
		}
		
		public override ExamineType GetExamineType()
		{
			return ExamineType.IDENTIFICATION_CARD;
		}
		
    }
}