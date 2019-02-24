using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace RiotEmoteConsole {
    public enum Mode {
        Insert,
        Clip,
        ClipInsert,
        Substitute
    }

    public class Program : Form {
        public static Dictionary<string, string> EmoteMap;
        public Mode Mode;

        public static string GetLinuxClipboard() {
            using (var proc = new Process()) {
                proc.StartInfo.FileName = "sh";
                proc.StartInfo.Arguments = "-c 'echo -n \"$(xsel -b)\"'";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();

                return proc.StandardOutput.ReadToEnd();
            }
        }

        public static void Main(string[] args) {
            Mode mode = Mode.Insert;

            EmoteMap = new Dictionary<string, string>();
            using (var f = File.OpenRead(Path.Combine(Directory.GetParent(Application.ExecutablePath).FullName, "emotemap.txt")))
            using (var reader = new StreamReader(f))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var split = line.Split(' ');
                    EmoteMap[split[0].ToLowerInvariant()] = $"![](mxc://{split[1]})";
                }
            }

            if (args.Length > 0) {
                var strmode = args[0].ToLowerInvariant();
                switch (strmode) {
                    case "insert": break;
                    case "clip": mode = Mode.Clip; break;
                    case "clip+insert": mode = Mode.ClipInsert; break;
                    case "insert+clip": mode = Mode.ClipInsert; break;
                    case "substitute": mode = Mode.Substitute; break;
                    default: throw new Exception($"Invalid mode: '{strmode}'");
                }
            }

            if (mode == Mode.Substitute) {
                var p = (int)Environment.OSVersion.Platform;
                if (p == 4 || p == 6 || p == 128) {
                    var old_clip_content = GetLinuxClipboard();

                    Console.WriteLine($"OLD CLIPBOARD:\n[{old_clip_content}]\n");
                    var proc = Process.Start("sh", "-c 'xdotool key ctrl+a; sleep 0.5; xdotool key ctrl+c'");
                    proc.WaitForExit();

                    var new_clip_content = GetLinuxClipboard();

                    Console.WriteLine($"NEW CLIPBOARD: {new_clip_content}");
                    if (string.IsNullOrEmpty(new_clip_content)) throw new Exception("New clip content was empty or null");

                    foreach (var kv in EmoteMap) {
                        var emotesub = $":{kv.Key}:";
                        new_clip_content = new_clip_content.Replace(emotesub, kv.Value);
                    }

                    Console.WriteLine($"New clip content: {new_clip_content}");

                    Process.Start("sh", $"-c 'echo -n \"{new_clip_content}\" | xsel -b'").WaitForExit();
                    Process.Start("sh", "-c 'xdotool key ctrl+v'");
                }
                else {
                    throw new Exception("Unsupported platform for substitute mode");
                }
            } else {
                Application.Run(new Program(mode));
            }
        }

        public Program(Mode mode) {
            Mode = mode;

            var box = new TextBox();
            box.Width = 128;
            box.Height = 16;
            Width = 32;
            Height = 16;
            FormBorderStyle = FormBorderStyle.None;
            CenterToScreen();
            Controls.Add(box);

            box.KeyPress += (sender, e) => {
                if (e.KeyChar == (char)Keys.Return) DoEmote(box.Text);
                else if (e.KeyChar == (char)Keys.Escape) Application.Exit();
            };
        }

        public void DoEmote(string value) {
            Application.Exit();

            string emote_code;
            if (!EmoteMap.TryGetValue(value.ToLowerInvariant(), out emote_code)) {
                emote_code = $"Invalid emote code '{value}'";
            }

            var insert = Mode == Mode.Insert || Mode == Mode.ClipInsert;
            var clip = Mode == Mode.Clip || Mode == Mode.ClipInsert;

            var p = (int)Environment.OSVersion.Platform;
            if (p == 4 || p == 6 || p == 128) {
                // linux
                if (clip) Process.Start("sh", $"-c 'echo -n \"{emote_code}\" | xsel -b'");
                if (insert) Process.Start("xdotool", $"type \"{emote_code}\"");
            } else {
                if (clip) Clipboard.SetText(emote_code);
                if (insert) SendKeys.Send(emote_code);
            }
        }
    }
}
