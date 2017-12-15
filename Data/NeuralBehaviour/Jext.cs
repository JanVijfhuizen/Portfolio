using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;

namespace Jext
{
    public static class Methods
    {
        #region Game Specific

        public static void Reset(this Rigidbody rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.isKinematic = false;
        }

        #endregion

        #region Sorting

        public delegate float GetSortValue<T>(T sortable);
        public static List<T> SuperSort<T>(this List<T> sortableList, GetSortValue<T> sortFunct)
        {
            return sortableList.OrderBy(t => sortFunct(t)).ToList();
        }

        public static List<GameObject> SortByClosest(this List<GameObject> sortableList, Vector3 pos)
        {
            return sortableList.OrderBy(t => Vector3.Distance(t.transform.position, pos)).ToList();
        }

        public static List<T> SortByClosest<T>(this List<T> sortableList, Vector3 pos) where T : MonoBehaviour
        {
            return sortableList.OrderBy(t => Vector3.Distance(t.transform.position, pos)).ToList();
        }

        #endregion

        #region Get From List

        public static List<U> GetTypeFromListAsU<T, U>(this List<U> list) where T : class
        {
            List<U> ret = new List<U>();
            foreach (U t in list)
                if (t as T != null)
                    ret.Add(t);
            return ret;
        }

        public static List<T> GetTypeFromListAsT<T, U>(this List<U> list) where T : class
        {
            List<T> ret = new List<T>();
            foreach (U t in list)
                if (t as T != null)
                    ret.Add(t as T);
            return ret;
        }

        #endregion

        #region Converting

        public static List<T> ConvertListToNew<T>(this List<T> convertable)
        {
            List<T> ret = new List<T>();
            foreach (T t in convertable)
                ret.Add(t);
            return ret;
        }

        public static T[] ConvertListToNew<T>(this T[] list)
        {
            T[] ret = new T[list.Length];
            for (int i = 0; i < list.Length; i++)
                ret[i] = list[i];
            return ret;
        }

        public static T Clone<T>(this T source)
        {
            if (!typeof(T).IsSerializable)
                throw new ArgumentException("The type must be serializable.", "source");

            if (ReferenceEquals(source, null))
                return default(T);

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        #endregion

        #region XML

        public static T Load<T>(string path) where T : class
        {
            if (!File.Exists(path))
            {
                Debug.LogError("No Save Data has been found!");
                return null;
            }
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            FileStream stream = new FileStream(path, FileMode.Open);
            T ret = (T)serializer.Deserialize(stream) as T;
            stream.Close();
            return ret;
        }

        public static void Save<T>(this T saveData, string path) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            FileStream stream = new FileStream(path, FileMode.Create);
            serializer.Serialize(stream, saveData);
            stream.Close();
        }

        /// <param name="folderPath">Where folderPath[folderPath.Count - 1] = fileName</param>
        /// <returns></returns>
        public static string GenerateFilePath(this List<string> folderPath)
        {
            char s = Path.DirectorySeparatorChar;
            string path;

            #if UNITY_STANDALONE
            path = Application.dataPath;
            #endif

            #if UNITY_ANDROID || UNITY_IOS
            path = Application.persistentDataPath;
            #endif

            foreach (string f in folderPath)
                path += s + f;
            path += ".xml";
            return path;
        }
        #endregion
    }
}