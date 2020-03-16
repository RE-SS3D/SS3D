using UnityEngine;

namespace SS3D.Engine.Server.Helpers
{
    /// <summary>
    /// Helper class used to deserialize a json array
    /// Usage:
    ///     MySerializableClass[] newArray = JsonArray.Deserialize<MySerializableClass>(jsonString, nameOfArrayElement);
    /// </summary>
    public static class JsonArray
    {
        public static T[] Deserialize<T>(string json, string arrayName)
        {
            string replacedJson = json.Replace(arrayName, "array");
            Wrapper<T> wrappedArray = JsonUtility.FromJson<Wrapper<T>>(replacedJson);
            return wrappedArray.array ?? new T[] { };
        }
 
        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] array = null;
        }
    }
}