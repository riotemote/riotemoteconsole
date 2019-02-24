using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace RiotEmoteConsole {
    public class Program : Form {
        public static Dictionary<string, string> EmoteMap;

        public static void Main() {
            Application.Run(new Program());
        }

        public Program() {
            EmoteMap = new Dictionary<string, string>();
            using (var f = File.OpenRead(Path.Combine(Directory.GetParent(Application.ExecutablePath).FullName, "emotemap.txt")))
            using (var reader = new StreamReader(f)) {
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    var split = line.Split(' ');
                    EmoteMap[split[0].ToLowerInvariant()] = $"![](mxc://{split[1]})";
                }
            }

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

            Process.Start("xdotool", $"type \"{emote_code}\"");
        }
    }
}
