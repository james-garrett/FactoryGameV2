using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryBuilderTemplate
{
    /// <summary>
    /// ISaveable interface used to handle object save/load operations
    /// Saving and loading logic can be found in FactoryBuilderMaster.cs and machines classes
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Save object data in JSON and return it as a string 
        /// </summary>
        /// <returns>Object data in json format</returns>
        string Save();

        /// <summary>
        /// Load data from string containing data in JSON format
        /// </summary>
        /// <param name="data">data in json format</param>
        /// <returns>True when loading was successful</returns>
        bool Load(string data);
    }
}
