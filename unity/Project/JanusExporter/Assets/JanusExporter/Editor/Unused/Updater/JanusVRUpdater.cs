#if UPDATER

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVRUpdater
{
    /// <summary>
    /// Class shown when the user asks to update to the latest version
    /// </summary>
    public class JanusVRUpdater : EditorWindow
    {
        public const string UpdateUrl = @"https://raw.githubusercontent.com/JamesMcCrae/janusvr_utils/master/unity/Release/version.txt";
        public const string UnityPkgUrl = @"https://github.com/JamesMcCrae/janusvr_utils/raw/master/unity/Release/JanusVRExporter.unitypackage";

        [NonSerialized]
        private Rect border = new Rect(10, 5, 20, 15);
        [NonSerialized]
        private int serverVersion = 0;
        [NonSerialized]
        private string status;
        [NonSerialized]
        private string description;
        private WebClient client;
        private Stopwatch timer;
        private string error;

        private bool downloaded;
        private string tempFile;
        private bool refresh;
        private bool open;

        //[MenuItem("Window/JanusVR Updater")]
        public static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            JanusVRUpdater window = EditorWindow.GetWindow<JanusVRUpdater>();
            window.Show();
        }

        private void OnInspectorUpdate()
        {
            if (refresh)
            {
                refresh = false;
                this.Repaint();
            }

            if (downloaded && !open)
            {
                open = true;
                AssetDatabase.ImportPackage(tempFile, true);
                this.Close();
            }
        }

        private readonly int version;

        public JanusVRUpdater()
        {
            // extract the version from the other namespace, because this class never changes
            Type t = this.GetType().Assembly.GetType("JanusVR.JanusGlobals");
            if (t == null)
            {
                version = 0; // nothing installed, just the updater
            }
            else
            {
                FieldInfo field = t.GetField("Version", BindingFlags.Static | BindingFlags.Public);
                version = (int)field.GetValue(null);
            }
        }

        private void OnEnable()
        {
            // search for the icon file
            Texture2D icon = Resources.Load<Texture2D>("janusvricon");
            this.titleContent = new GUIContent("Update", icon);

            status = "Server version unknown";
            description = "";
        }

        private void CleanUp()
        {
            downloaded = false;
            open = false;

            if (!string.IsNullOrEmpty(tempFile))
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        private void CheckForUpdates()
        {
            if (client != null &&
                timer.Elapsed.TotalSeconds < 30)
            {
                return;
            }
            CleanUp();

            timer = Stopwatch.StartNew();

            status = "Waiting...";
            description = "";

            // Mono and HTTPS dont combine, so we need to check the certificate
            // to make sure it works
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

            client = new WebClient();
            client.DownloadDataCompleted += Client_DownloadDataCompleted;
            client.DownloadDataAsync(new Uri(UpdateUrl));
        }

        private void DownloadUpdate()
        {
            if (client != null)
            {
                return;
            }
            CleanUp();

            status = "Downloading";
            description = "0%";

            timer = Stopwatch.StartNew();
            tempFile = Path.GetTempFileName();

            client = new WebClient();
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileAsync(new Uri(UnityPkgUrl), tempFile);
        }

        public static bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;
            // If there are errors in the certificate chain, look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                        bool chainIsValid = chain.Build((X509Certificate2)certificate);
                        if (!chainIsValid)
                        {
                            isOk = false;
                        }
                    }
                }
            }
            return isOk;
        }

        private void Client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            client.Dispose();
            client = null;
            timer.Stop();
            timer = null;

            if (e.Error == null)
            {
                error = null;
                string value = Encoding.UTF8.GetString(e.Result);
                int.TryParse(value, out serverVersion);

                status = "Server version " + (serverVersion / 100.0).ToString("F2");
                if (serverVersion > version)
                {
                    description = "Server version is newer than the importer version.";
                }
                else
                {
                    description = "You're up to date";
                }
            }
            else
            {
                error = e.Error.Message;

                status = "Error";
                description = error;
            }
        }

        #region Package

        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            client.Dispose();
            client = null;
            timer.Stop();
            timer = null;            

            if (e.Error == null && !e.Cancelled)
            {
                error = null;
                downloaded = true;
            }
            else
            {
                error = e.Error.Message;
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            description = e.ProgressPercentage + "%";
            refresh = true;
        }

        #endregion


        private void OnGUI()
        {
            Rect rect = this.position;
            GUILayout.BeginArea(new Rect(border.x, border.y, rect.width - border.width, rect.height - border.height));

            GUILayout.Label("JanusVR Unity Exporter Version " + (version / 100.0).ToString("F2"), EditorStyles.boldLabel);

            GUILayout.Label("Status: " + status);
            GUILayout.Label(description);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Check for Updates"))
            {
                CheckForUpdates();
            }

            if (serverVersion > version)
            {
                if (GUILayout.Button("Download Update"))
                {
                    DownloadUpdate();
                }
            }

            GUILayout.EndArea();
        }
    }
}
#endif
