using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace System.Electricity
{
    public class Circuit
    {
        private List<IPowerConsumer> _consumers;
        private List<IPowerProducer> _producer;




        // when removing one element of the circuit, start a tree search for all connected elements to that element,
        // accordingly updating circuits ?

        /// <summary>
        /// Merge two small circuits into a big one.
        /// </summary>
        /// <param name="First"></param>
        /// <param name="Second"></param>
        private static void Merge(Circuit First, Circuit Second)
        {

        }

        /// <summary>
        /// Separate a big circuit into smaller ones.
        /// </summary>
        /// <param name="circuit"></param>
        /// <param name="circuits"></param>
        private static void Split(Circuit circuit, out List<Circuit> circuits)
        {
            circuits = new List<Circuit>();
        }


    }
}
