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
		private string MugshotPath;
		
		
		
		public DataIdentificationCard(string firstName, string surname, int age, string species, DateTime expiry, string title, int classifier, string gender, string mugshotPath)
		{
			FirstName = firstName;
			Surname = surname;
			Age = age;
			Species = species;
			Expiry = expiry;
			Title = title;
			Classifier = classifier;
			Gender = gender;
			MugshotPath = mugshotPath;
			Mugshot = Resources.Load<Sprite>(MugshotPath);
		}

		// Returns the name of the object (i.e. Identification Card)
		// It does NOT return the name of the person identified by the card!
		public string GetName()
		{
			return "Identification Card";
		}

		// Returns the character first name
		public string GetFirstName()
		{
			return FirstName;
		}

		// Returns the character surname
        public string GetSurname()
		{
			return Surname;
		}
		
		// Returns the ID photo of the character
		public Sprite GetMugshot()
		{
			return Mugshot;
		}
		
		// Returns the character age
		public int GetAge()
		{
			return Age;
		}
		
		// Returns the species of the character
        public string GetSpecies()
		{
			return Species;
		}

		// Returns the expiry date of the ID card
        public DateTime GetExpiry()
		{
			return Expiry;
		}

		// Returns the character's job title
        public string GetTitle()
		{
			return Title;
		}

		// Returns the character classification in the station hierarchy
        public int GetClassifier()
		{
			return Classifier;
		}		
		
		// Returns the character gender
        public string GetGender()
		{
			return Gender;
		}		
		
		// Lets the Examine system know to treat this like an ID card. 
		public ExamineType GetExamineType()
		{
			return ExamineType.IDENTIFICATION_CARD;
		}
		
    }
}
