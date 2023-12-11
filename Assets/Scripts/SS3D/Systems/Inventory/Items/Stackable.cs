namespace SS3D.Systems.Inventory.Items
{
  //   [RequireComponent(typeof(Item))]
  //   public class Stackable : SpessBehaviour, IExaminable
  //   {
  //       public int maxStack;
  //       public int amountInStack;
        // private IExamineRequirement requirements;
  //
        // protected override void OnStart()
        // {
        //     base.OnStart();
  //
        //     PopulateRequirements();
        // }
  //
        // private void PopulateRequirements()
        // {
        //     // Populate requirements for this item to be examined.
        //     requirements = new ReqPermitExamine(gameObject);
        //     requirements = new ReqMaxRange(requirements, 2.0f);  // Amount in stack only visible from 2 metres.
        //     requirements = new ReqObstacleCheck(requirements);
        // }
  //
        // private void OnValidate()
  //       {
  //           if (maxStack < 2)
  //           {
  //               maxStack = 2;
  //           }
  //
  //           if (amountInStack < 1)
  //           {
  //               amountInStack = 1;
  //           }
  //           else if (amountInStack > maxStack)
  //           {
  //               amountInStack = maxStack;
  //           }
  //       }
  //
        // public IExamineData GetData()
        // {
        //     return new DataNameDescription("", $"{amountInStack} in stack");
        // }
        //
        // public IExamineRequirement GetRequirements()
        // {
        //     return requirements;
        // }
  //   }
}
