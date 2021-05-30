using UnityEngine;
using System;

namespace SS3D.Engine.Examine
{
    public class DataIdentificationCard : IExamineData
    {
		private Sprite Mugshot;
		private string FirstName;
		private string Surname;
		private int Age;
		private string Species;
		private DateTime Expiry;
		private string Title;
		private int Classifier;
		private string Gender;
		
		
		
		public DataIdentificationCard()
		{
			Age = UnityEngine.Random.Range(18, 65);
			DateTime expiry = DateTime.Today;
			int daysToAdd = UnityEngine.Random.Range(0,72) * 5;
			Expiry = expiry.AddDays(daysToAdd);

			int randomCharacter = UnityEngine.Random.Range(1,6);
			switch (randomCharacter)
			{
				case 1:
					FirstName = "Tippo";
					Surname = "Felangus";
					Species = "Human";
					Title = "Chemist";
					Gender = "Male";
					Classifier = 4;
					break;
				case 2:
					FirstName = "Reinard";
					Surname = "Parker";
					Species = "Human";
					Title = "Roboticist";
					Gender = "Male";
					Classifier = 4;
					break;
				case 3:
					FirstName = "Robert";
					Surname = "Oppenheimer";
					Species = "Human";
					Title = "Research Director";
					Gender = "Male";
					Classifier = 2;
					break;				
				case 4:
					FirstName = "William";
					Surname = "Harshman";
					Species = "Human";
					Title = "Head of Security";
					Gender = "Male";
					Classifier = 2;
					break;
				case 5:
					FirstName = "George";
					Surname = "Melons";
					Species = "Human";
					Title = "Assistant";
					Gender = "Male";
					Classifier = 6;
					break;
				case 6:
					FirstName = "Ruth";
					Surname = "McVork";
					Species = "Human";
					Title = "Geneticist";
					Gender = "Female";
					Classifier = 4;
					break;					
			}
			
		}
		
        public string GetFirstName()
		{
			return FirstName;
		}

        public string GetSurname()
		{
			return Surname;
		}
		
		public Sprite GetMugshot()
		{
			return Mugshot;
		}
		
		public int GetAge()
		{
			return Age;
		}
		
        public string GetSpecies()
		{
			return Species;
		}

        public DateTime GetExpiry()
		{
			return Expiry;
		}

        public string GetTitle()
		{
			return Title;
		}

        public int GetClassifier()
		{
			return Classifier;
		}		
		
        public string GetGender()
		{
			return Gender;
		}		
		
		public ExamineType GetExamineType()
		{
			return ExamineType.IDENTIFICATION_CARD;
		}
		
    }
}
