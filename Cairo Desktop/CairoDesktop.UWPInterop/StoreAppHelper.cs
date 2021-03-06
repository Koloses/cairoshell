﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using CairoDesktop.Interop;

namespace CairoDesktop.UWPInterop
{
    public class StoreAppHelper
    {
        const string defaultColor = "#00111111";

        private static string userSID = null;
        private static double scale = 0;

        public static List<string[]> GetStoreApps()
        {
            List<string[]> ret = new List<string[]>();

            try
            {
                Windows.Management.Deployment.PackageManager pman = new Windows.Management.Deployment.PackageManager();
                IEnumerable<Windows.ApplicationModel.Package> packages = getPackages(pman);

                foreach (Windows.ApplicationModel.Package package in packages)
                {
                    string path = "";

                    // need to catch a system-thrown exception...
                    try
                    {
                        path = package.InstalledLocation.Path;
                    }
                    catch
                    {
                        continue;
                    }

                    XmlDocument manifest = getManifest(path);
                    XmlNamespaceManager xmlnsManager = getNamespaceManager(manifest);

                    foreach (XmlNode app in manifest.SelectNodes("/ns:Package/ns:Applications/ns:Application", xmlnsManager))
                    {
                        // packages can contain multiple apps

                        XmlNode showEntry = app.SelectSingleNode("uap:VisualElements/@AppListEntry", xmlnsManager);
                        if (showEntry == null || showEntry.Value == "true")
                        {
                            // App is visible in the applist

                            // return values
                            string appUserModelId = package.Id.FamilyName + "!" + app.SelectSingleNode("@Id", xmlnsManager).Value;
                            string returnName = getDisplayName(package.Id.Name, path, app, xmlnsManager);
                            string returnIcon = getIconPath(path, app, xmlnsManager, 1);
                            string returnColor = "";

                            if (returnIcon.EndsWith("_altform-unplated.png"))
                                returnColor = defaultColor;
                            else
                                returnColor = getPlateColor(app, xmlnsManager);

                            string[] toAdd = new string[] { appUserModelId, returnName, returnIcon, returnColor };
                            bool canAdd = true;
                            foreach (string[] added in ret)
                            {
                                if (added[0] == appUserModelId)
                                {
                                    canAdd = false;
                                    break;
                                }
                            }

                            if (canAdd)
                                ret.Add(toAdd);
                        }
                    }
                }
            }
            catch { }

            return ret;
        }

        private static XmlDocument getManifest(string path)
        {
            XmlDocument manifest = new XmlDocument();
            string manPath = path + "\\AppxManifest.xml";

            if (Shell.Exists(manPath))
                manifest.Load(manPath);

            return manifest;
        }

        private static XmlNamespaceManager getNamespaceManager(XmlDocument manifest)
        {
            XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(manifest.NameTable);
            xmlnsManager.AddNamespace("ns", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
            xmlnsManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

            return xmlnsManager;
        }

        private static string getDisplayName(string packageName, string packagePath, XmlNode app, XmlNamespaceManager xmlnsManager)
        {
            Uri nameUri;
            string nameKey = app.SelectSingleNode("uap:VisualElements/@DisplayName", xmlnsManager).Value;

            if (!Uri.TryCreate(nameKey, UriKind.Absolute, out nameUri))
                return nameKey;
            else
            {
                var resourceKey = string.Format("ms-resource://{0}/resources/{1}", packageName, nameUri.Segments.Last());
                string name = ExtractStringFromPRIFile(packagePath + "\\resources.pri", resourceKey);
                if (!string.IsNullOrEmpty(name))
                    return name;
                else
                {
                    resourceKey = string.Format("ms-resource://{0}/{1}", packageName, nameUri.Segments.Last());
                    name = ExtractStringFromPRIFile(packagePath + "\\resources.pri", resourceKey);
                    if (!string.IsNullOrEmpty(name))
                        return name;
                    else
                    {
                        return ExtractStringFromPRIFile(packagePath + "\\resources.pri", nameUri.ToString());
                    }
                }
            }
        }

        private static string getPlateColor(XmlNode app, XmlNamespaceManager xmlnsManager)
        {
            XmlNode colorKey = app.SelectSingleNode("uap:VisualElements/@BackgroundColor", xmlnsManager);

            if (colorKey != null && !string.IsNullOrEmpty(colorKey.Value) && colorKey.Value.ToLower() != "transparent")
                return colorKey.Value;
            else
                return defaultColor;
        }

        private static string getIconPath(string path, XmlNode app, XmlNamespaceManager xmlnsManager, int size)
        {
            string iconPath = path + "\\" + (app.SelectSingleNode("uap:VisualElements/@Square44x44Logo", xmlnsManager).Value).Replace(".png", "");

            List<string> iconAssets = new List<string> {
                ".png",
                ".targetsize-32_altform-unplated.png",
                ".targetsize-36_altform-unplated.png",
                ".targetsize-40_altform-unplated.png",
                ".targetsize-48_altform-unplated.png",
                ".targetsize-32.png",
                ".targetsize-36.png",
                ".targetsize-40.png",
                ".targetsize-48.png",
                ".targetsize-256_altform-unplated.png",
                ".scale-200.png",
                ".targetsize-24_altform-unplated.png",
                ".targetsize-16_altform-unplated.png",
                ".targetsize-24.png",
                ".targetsize-16.png",
                ".scale-100.png",
                ".targetsize-256.png"
            };

            // do some sorting based on DPI for prettiness
            if (scale == 0)
                scale = Shell.GetDpiScale();

            int numMoved = 0;
            for (int i = 0; i < iconAssets.Count; i++)
            {
                if (((scale < 1.25 && size == 1) && iconAssets[i].Contains("16")) || ((((scale >= 1.25 && scale < 1.75) && size == 1) || (scale < 1.25 && size == 10)) && iconAssets[i].Contains("24")) || (((scale >= 1.5 && size == 10) || ((scale >= 1.25 && scale <= 1.75) && size == 0)) && iconAssets[i].Contains("48")) || (((scale >= 1.5 && size != 1) || (scale >= 1.25 && size == 0)) && (iconAssets[i].Contains("200") || iconAssets[i].Contains("100") || iconAssets[i].Contains("256"))))
                {
                    string copy = iconAssets[i];
                    iconAssets.RemoveAt(i);
                    iconAssets.Insert(1 + numMoved, copy);
                    numMoved++;
                }
            }

            foreach (string iconName in iconAssets)
            {
                if (File.Exists(iconPath + iconName))
                {
                    return iconPath + iconName;
                }
            }

            return "";
        }

        private static IEnumerable<Windows.ApplicationModel.Package> getPackages(Windows.Management.Deployment.PackageManager pman)
        {
            if (userSID == null)
                userSID = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();

            try
            {
                return pman.FindPackagesForUser(userSID);
            }
            catch
            {
                return Enumerable.Empty<Windows.ApplicationModel.Package>();
            }
        }

        private static IEnumerable<Windows.ApplicationModel.Package> getPackages(Windows.Management.Deployment.PackageManager pman, string packageFamilyName)
        {
            if (userSID == null)
                userSID = System.Security.Principal.WindowsIdentity.GetCurrent().User.ToString();

            try
            {
                return pman.FindPackagesForUser(userSID, packageFamilyName);
            }
            catch
            {
                return Enumerable.Empty<Windows.ApplicationModel.Package>();
            }
        }

        // returns [icon, color]
        public static string[] GetAppIcon(string appUserModelId, int size)
        {
            string[] pkgAppId = appUserModelId.Split('!');
            string packageFamilyName = pkgAppId[0];
            string appId = pkgAppId[1];
            string returnIcon = "";
            string returnColor = "";

            Windows.Management.Deployment.PackageManager pman = new Windows.Management.Deployment.PackageManager();
            IEnumerable<Windows.ApplicationModel.Package> packages = getPackages(pman, packageFamilyName);

            foreach (Windows.ApplicationModel.Package package in packages)
            {
                string path = "";

                // need to catch a system-thrown exception...
                try
                {
                    path = package.InstalledLocation.Path;
                }
                catch
                {
                    continue;
                }

                XmlDocument manifest = getManifest(path);
                XmlNamespaceManager xmlnsManager = getNamespaceManager(manifest);

                bool found = false;

                foreach (XmlNode app in manifest.SelectNodes("/ns:Package/ns:Applications/ns:Application", xmlnsManager))
                {
                    // get specific app in package
                    
                    if (app.SelectSingleNode("@Id", xmlnsManager).Value == appId)
                    {
                        // return values
                        returnIcon = getIconPath(path, app, xmlnsManager, size);

                        if (returnIcon.EndsWith("_altform-unplated.png"))
                            returnColor = defaultColor;
                        else
                            returnColor = getPlateColor(app, xmlnsManager);

                        found = true;

                        break;
                    }
                }

                if (found)
                    break;
            }

            return new string[] { returnIcon, returnColor };
        }

        [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
        private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

        static internal string ExtractStringFromPRIFile(string pathToPRI, string resourceKey)
        {
            string sWin8ManifestString = string.Format("@{{{0}? {1}}}", pathToPRI, resourceKey);
            var outBuff = new StringBuilder(256);
            int result = SHLoadIndirectString(sWin8ManifestString, outBuff, outBuff.Capacity, IntPtr.Zero);
            return outBuff.ToString();
        }
    }
}
