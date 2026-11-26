#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace FruitTree.Editor
{
    /// <summary>
    /// The Google Mobile Ads SDK adds com.google.android.gms.permission.AD_ID.
    /// Fruit Tree targets all ages, including children (Google Play Families), and
    /// requests only non-personalised, child-directed ads, so it must not declare
    /// AD_ID. A tools:node="remove" entry in the launcher manifest, which outranks
    /// every library manifest, strips it during the Gradle manifest merge.
    /// </summary>
    public class RemoveAdIdPermission : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNs = "http://schemas.android.com/apk/res/android";
        private const string ToolsNs = "http://schemas.android.com/tools";
        private const string AdIdPermission = "com.google.android.gms.permission.AD_ID";

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            string manifest = Path.Combine(unityLibraryPath, "..", "launcher",
                "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest))
                throw new FileNotFoundException("Fruit Tree: launcher manifest not found", manifest);

            var doc = new XmlDocument();
            doc.Load(manifest);
            XmlElement root = doc.DocumentElement;
            if (!root.HasAttribute("xmlns:tools"))
                root.SetAttribute("xmlns:tools", ToolsNs);

            foreach (XmlElement e in root.GetElementsByTagName("uses-permission"))
                if (e.GetAttribute("name", AndroidNs) == AdIdPermission)
                    return;

            XmlElement remove = doc.CreateElement("uses-permission");
            remove.SetAttribute("name", AndroidNs, AdIdPermission);
            remove.SetAttribute("node", ToolsNs, "remove");
            root.PrependChild(remove);
            doc.Save(manifest);
            Debug.Log("Fruit Tree: AD_ID permission removed from the Android manifest.");
        }
    }
}
#endif
