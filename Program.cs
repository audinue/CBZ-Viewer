using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CBZ_Viewer
{
    class Program : Form
    {
        Program(string file)
        {
            HttpListener listener;
            int port = 8000;
            while (true)
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/");
                try
                {
                    listener.Start();
                    break;
                }
                catch (Exception e)
                {
                    port++;
                }
            }
            AsyncCallback onRequest = null;
            onRequest = new AsyncCallback((IAsyncResult result) =>
            {
                if (!listener.IsListening)
                    return;
                var context = listener.EndGetContext(result);
                listener.BeginGetContext(onRequest, null);
                var request = context.Request;
                using (var response = context.Response)
                {
                    if (request.Url.PathAndQuery == "/")
                    {
                        response.Headers["X-UA-Compatible"] = "IE=EDGE";
                        using (var writer = new StreamWriter(response.OutputStream))
                        {
                            writer.Write("<style>body{background:buttonface}img{display:block;max-width:100%;margin:auto}</style>");
                            using (var zip = ZipFile.OpenRead(file))
                                foreach (var entry in zip.Entries.OrderBy(entry => entry.Name, new NaturalStringComparer()))
                                    writer.Write($"<img src=\"/{Uri.EscapeDataString(entry.Name)}\">");
                        }
                    }
                    else
                    {
                        using (var zip = ZipFile.OpenRead(file))
                            zip.GetEntry(request.Url.PathAndQuery.Substring(1))
                                .Open()
                                .CopyTo(response.OutputStream);
                    }
                }
            });
            listener.BeginGetContext(onRequest, null);
            FormClosed += (sender, e) => listener.Stop();
            Text = Path.GetFileNameWithoutExtension(file);
            Width = 800;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            AutoScaleMode = AutoScaleMode.Dpi;
            var browser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                Url = new Uri($"http://localhost:{port}/"),
                AllowNavigation = false,
                AllowWebBrowserDrop = false,
                WebBrowserShortcutsEnabled = false,
                IsWebBrowserContextMenuEnabled = false
            };
            browser.Navigated += (sender, e) => Activate();
            Controls.Add(browser);
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();
            string file;
            if (args.Length != 1)
            {
                var dialog = new OpenFileDialog { Filter = "CBZ Files (*.cbz)|*.cbz" };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    file = dialog.FileName;
                }
                else
                    return;
            }
            else
                file = args[0];
            Application.Run(new Program(file));
        }
        class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                return StrCmpLogicalW(a, b);
            }
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        static extern int StrCmpLogicalW(string psz1, string psz2);

        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();
    }
}
