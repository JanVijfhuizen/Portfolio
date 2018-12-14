using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace Jext
{
    public static class Methods
    {
        #region Game Specific

        /// <summary>
        /// Uses the Tahn Function.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float Activation(float value)
        {
            return (float)Math.Tanh(value);
        }

        public enum FadeType {FadeIn, FadeOut, TotalFade }
        public static IEnumerator FadeToBlack(Image image, float speed, FadeType type)
        {
            if(type != FadeType.FadeIn)
                while (image.color.a < 0.9)
                {
                    image.FadeToBlack(true, speed);
                    yield return null;
                }

            if(type != FadeType.FadeOut)
                while (image.color.a > 0)
                {
                    image.FadeToBlack(false, speed);
                    yield return null;
                }
        }

        private static Image FadeToBlack(this Image image, bool add, float fadeSpeed)
        {
            Color color = image.color;
            color.a += Time.deltaTime * fadeSpeed * (add ? 1 : -1);
            image.color = color;
            return image;
        }

        public static void Reset(this Rigidbody rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.isKinematic = false;
        }

        public static int RandomIndex<T>(this List<T> self)
        {
            return UnityEngine.Random.Range(0, self.Count - 1);
        }

        public static List<T> AddList<T>(this List<T> self, List<T> other, bool duplicatesAllowed)
        {
            foreach (T t in other)
                if(!self.Contains(t) || duplicatesAllowed)
                    self.Add(t);
            return self;
        }

        public static List<T> RemoveList<T>(this List<T> self, List<T> other)
        {
            self.RemoveAll(x => other.Contains(x));
            return self;
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

        public static List<T> CloneList<T>(this List<T> source)
        {
            List<T> ret = new List<T>();
            foreach (T t in source)
                ret.Add(t);
            return ret;
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