using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using SS3D.Engine.Interactions;
using SS3D.Engine.Examine;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Items.Functional.Tools.Generic
{
    
    public class IdentificationCard : Item, IExaminable
    {
		
		// Distance at which the player can visually examine the ID
		private float MaxDistance = 2;
		
		// Requirements for the player to visually examine the ID
		private IExamineRequirement requirements;
		
		// Details contained on the ID card
		private DataIdentificationCard IdDetails;
		
		// Initial random numbers for character details.
		private int initialCharacterID;
		
		// Struct to randomly select character details to display
		[SyncVar(hook = nameof(SyncIDCardDetails))]
		private int characterID;

		// TEST
		[SyncVar(hook = nameof(SyncTest))]
		private bool testSyncVar;
		
		// Data fields required on the ID
		private string FirstName;
		private string Surname;
		private int Age;
		private string Species;
		private DateTime Expiry;
		private string Title;
		private int Classifier;
		private string Gender;				
		private Sprite Mugshot;
		
		public void Start()
		{
			// Ensure random numbers initialised.
			EnsureInit();
			
			// Populate requirements for this item to be examined.
			requirements = new ReqPermitExamine(gameObject);
			requirements = new ReqMaxRange(requirements, MaxDistance);
			requirements = new ReqObstacleCheck(requirements);
			
			// Temporary script to generate a random persons details for the ID card.
			// Obviously, this will be replaced with actual names / roles / details.			
			if (isServer)
			{	
				SyncIDCardDetails(0, initialCharacterID);
				//SyncTest(testSyncVar, !testSyncVar);
			}
			
		}
		
		private void SyncTest(bool oldTest, bool newTest)
		{
			testSyncVar = newTest;
			
			for (int i = 0; i < 100; i++)
			{
				Debug.Log("SyncTest! isServer = " + isServer);
			}
		}
		
		public void OnStartServer()
		{
			EnsureInit();
			base.OnStartServer();
		}
		
		public void OnStartClient()
		{
			EnsureInit();
			base.OnStartClient();
		}	

		public void Awake()
		{
			EnsureInit();
			base.Awake();
		}			

		// Make sure the random numbers have been generated before we try using the SyncVar.
		private void EnsureInit()
		{
			if (initialCharacterID == 0);
			{
				
				// Change the arguments here to generate different people.
				initialCharacterID = UnityEngine.Random.Range(7, 11);
			}
		}

		// This two-argument hook method is called on the client. We don't need both arguments - call overloaded version.
		//private void SyncIDCardDetails (RandomIDNumbers oldDetails, RandomIDNumbers newDetails)
		//{
		//	EnsureInit();
		//	SyncIDCardDetails(newDetails);
		//}	
		
		private void SyncIDCardDetails(int oldCharacter, int newCharacter)
		{
			EnsureInit();

			// This is our synchronised variable, and should trigger our hook method on the client.
			characterID = newCharacter;
			
			// Set all the ID details randomly.
			Expiry = DateTime.Today;    // Need to randomise this.
			switch (newCharacter)
			{
				case 1:
					FirstName = "Tippo";
					Surname = "Felangus";
					Species = "Human";
					Title = "Chemist";
					Gender = "Male";
					Classifier = 4;
					Age = 31;
					break;
				case 2:
					FirstName = "Reinard";
					Surname = "Parker";
					Species = "Human";
					Title = "Roboticist";
					Gender = "Male";
					Classifier = 4;
					Age = 53;

					break;
				case 3:
					FirstName = "Robert";
					Surname = "Oppenheimer";
					Species = "Human";
					Title = "Research Director";
					Gender = "Male";
					Classifier = 2;
					Age = 71;
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
					Age = 18;
					break;
				case 6:
					FirstName = "Ruth";
					Surname = "McVork";
					Species = "Human";
					Title = "Geneticist";
					Gender = "Female";
					Classifier = 4;
					Age = 36;
					break;		
				case 7:
					FirstName = "Stuart";
					Surname = "Maple";
					Species = "Human";
					Title = "Chief Engineer";
					Gender = "Male";
					Classifier = 2;
					Age = 50;
					break;				
				case 8:
					FirstName = "Kaitlyn";
					Surname = "Gill";
					Species = "Human";
					Title = "Station Engineer";
					Gender = "Female";
					Classifier = 4;
					Age = 26;
					break;
				case 9:
					FirstName = "Husain";
					Surname = "Al'Shaqif";
					Species = "Human";
					Title = "Station Engineer";
					Gender = "Male";
					Classifier = 4;
					Age = 25;
					break;
				case 10:
					FirstName = "Laura";
					Surname = "Bowers";
					Species = "Human";
					Title = "Atmospheric Technician";
					Gender = "Female";
					Classifier = 4;
					Age = 31;
					break;								
			}
			
			// Generate the details to be passed to the Examine system.
			IdDetails = new DataIdentificationCard(FirstName, Surname, Age, Species, Expiry, Title, Classifier, Gender, Mugshot);				
		}
		
	
		
		// Provide the requirements to be able to examine the ID user interface.
		public IExamineRequirement GetRequirements()
		{
			return requirements;
		}

		// Provide the ID details to the Examine system
		public IExamineData GetData()
		{
			return IdDetails;
		}
	
    }
	
	/* At the moment this script just selects one from a few
	   different people, so that we have something to test as we
	   work on the interface. Later, it will be designed to take info
	   from the actual players. */
	public struct RandomIDNumbers
	{
		public int RandomCharacter { get; set; }
		public int RandomAge { get; set; }
		public int RandomExpiryOffset { get; set; }
		
		public RandomIDNumbers(int lowIndexInclusive, int highIndexInclusive)
		{
			RandomCharacter = UnityEngine.Random.Range(lowIndexInclusive, highIndexInclusive + 1);
			RandomAge = UnityEngine.Random.Range(18, 65);
			RandomExpiryOffset = UnityEngine.Random.Range(0,72) * 5;
		}
		
	}
	
	
}
