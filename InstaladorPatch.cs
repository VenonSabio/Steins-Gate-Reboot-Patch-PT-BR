// Instalador gráfico do patch PT-BR de STEINS;GATE RE:BOOT (formato SGPT1).
// Usa patch_ptbr.json + patch_ptbr.pak.NNN ao lado do .exe; se não houver, baixa da Release do GitHub.
// Confere SHA-256 dos originais antes e de cada arquivo gerado depois.
// Desinstalar: os "append" guardam o original no começo do arquivo e os "novo" são apagados;
// só os originais dos "full" são copiados, para %LOCALAPPDATA%\SteinsGateRebootPTBR (nunca na pasta do jogo).
// Compilar (.NET Framework 4.x):
//   csc /target:winexe /out:Instalar_Traducao_PTBR.exe /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll InstaladorPatch.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Reflection;
[assembly: AssemblyTitle("Instalador Steins;Gate Reboot PT-BR")]
[assembly: AssemblyDescription("Instalador da traducao PT-BR (fan patch)")]
[assembly: AssemblyCompany("VenonSabio")]
[assembly: AssemblyProduct("Steins;Gate Reboot PT-BR")]
[assembly: AssemblyCopyright("VenonSabio")]
[assembly: AssemblyVersion("1.3.1.0")]
[assembly: AssemblyFileVersion("1.3.1.0")]

class Janela : Form
{
    const string URL = "https://github.com/VenonSabio/Steins-Gate-Reboot-Patch-PT-BR/releases/latest/download/";
    static readonly Color Fundo = Color.White, Painel = Color.FromArgb(246, 246, 246),
        Laranja = Color.FromArgb(214, 96, 0), Texto = Color.FromArgb(35, 35, 40), Cinza = Color.FromArgb(110, 110, 118);
    Image banner, okabe;

    TextBox pasta; Button procurar, instalar, desinstalar; ProgressBar barra; Label status, detalhe; TextBox log;
    volatile bool rodando;

    static Image Recurso(string nome)
    {
        var s = typeof(Janela).Assembly.GetManifestResourceStream(nome);
        return s == null ? null : Image.FromStream(s);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        if (banner != null) g.DrawImage(banner, 0, 0, 860, 300);
        using (var b = new SolidBrush(Laranja)) g.FillRectangle(b, 0, 300, 860, 3);
        if (okabe != null) { int h = 360, w = okabe.Width * h / okabe.Height; g.DrawImage(okabe, 860 - w - 12, 640 - h, w, h); }
    }

    Janela()
    {
        Text = "STEINS;GATE RE:BOOT — Instalador da tradução PT-BR"; ClientSize = new Size(860, 640);
        FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; BackColor = Fundo; ForeColor = Texto;
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10f); DoubleBuffered = true;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        banner = Recurso("banner.png"); okabe = Recurso("okabe.png");

        var titulo = new Label { Text = "Assistente de instalação", Font = new Font("Segoe UI Light", 22f), ForeColor = Texto, AutoSize = true, Location = new Point(28, 312), BackColor = Color.Transparent };
        var desc = new Label { Text = "Este assistente instala a tradução para português brasileiro.\nInforme a pasta onde o STEINS;GATE RE:BOOT está instalado e clique em Instalar.\nO restante do patch (~3,3 GB) é baixado automaticamente.", ForeColor = Cinza, AutoSize = false, Location = new Point(32, 360), Size = new Size(600, 64), BackColor = Color.Transparent };
        var lbl = new Label { Text = "O meu STEINS;GATE RE:BOOT está em:", ForeColor = Texto, AutoSize = true, Location = new Point(30, 434), BackColor = Color.Transparent };
        pasta = new TextBox { Location = new Point(32, 458), Size = new Size(520, 28), BackColor = Painel, ForeColor = Texto, BorderStyle = BorderStyle.FixedSingle };
        procurar = Botao("...", new Point(560, 456), new Size(44, 28), false);
        barra = new ProgressBar { Location = new Point(32, 500), Size = new Size(572, 16), Maximum = 1000 };
        status = new Label { Text = "Escolha a pasta e clique em Instalar.", ForeColor = Texto, AutoSize = false, Location = new Point(30, 522), Size = new Size(580, 22), BackColor = Color.Transparent };
        detalhe = new Label { Text = "", ForeColor = Cinza, AutoSize = false, Location = new Point(30, 544), Size = new Size(580, 20), BackColor = Color.Transparent };
        instalar = Botao("Instalar", new Point(454, 574), new Size(150, 40), true);
        instalar.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        desinstalar = Botao("Desinstalar", new Point(326, 574), new Size(120, 40), false);
        log = new TextBox();
        var rodape = new Label { Text = "Tradução de fãs, não oficial.\nRequer a versão Steam original.", ForeColor = Cinza, Font = new Font("Segoe UI", 8f), AutoSize = false, Location = new Point(30, 576), Size = new Size(290, 40), BackColor = Color.Transparent };
        Controls.AddRange(new Control[] { titulo, desc, lbl, pasta, procurar, barra, status, detalhe, instalar, desinstalar, rodape });
        desinstalar.Click += delegate { IniciarDesinstalar(); };

        procurar.Click += delegate
        {
            using (var d = new FolderBrowserDialog { Description = "Pasta do STEINS;GATE RE:BOOT (contém sgre_steam.exe)" })
                if (d.ShowDialog(this) == DialogResult.OK) pasta.Text = d.SelectedPath;
        };
        instalar.Click += delegate { Iniciar(); };
        FormClosing += (s, e) =>
        {
            if (rodando && MessageBox.Show(this, "A instalação está em andamento. Cancelar pode deixar a tradução incompleta. Sair mesmo assim?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { e.Cancel = true; return; }
            if (AutoPasta == null) PerguntarLimpeza();
        };
        pasta.Text = Detectar();
        if (AutoPasta != null) { pasta.Text = AutoPasta; Shown += delegate { if (AutoDesinstalar) IniciarDesinstalar(); else Iniciar(); }; }
        Shown += delegate { instalar.Focus(); pasta.SelectionLength = 0; };
    }

    static string PastaTemp { get { return Path.Combine(Path.GetTempPath(), "SteinsGateReboot_PTBR_patch"); } }

    // Se sobrou download na pasta temporária (erro ou saída no meio), pergunta se apaga ou mantém para continuar depois.
    void PerguntarLimpeza()
    {
        if (!Directory.Exists(PastaTemp)) return;
        long bytes = 0; try { bytes = new DirectoryInfo(PastaTemp).GetFiles().Sum(f => f.Length); } catch { }
        if (bytes == 0) { try { Directory.Delete(PastaTemp, true); } catch { } return; }
        var r = MessageBox.Show(this, "Há " + (bytes / 1048576) + " MB de arquivos do patch já baixados em:\n" + PastaTemp +
            "\n\nSim = apagar agora (libera o espaço)\nNão = manter para continuar o download na próxima vez",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (r == DialogResult.Yes) { try { Directory.Delete(PastaTemp, true); } catch (Exception e) { MessageBox.Show(this, "Não foi possível apagar: " + e.Message, Text); } }
    }

    Button Botao(string t, Point p, Size s, bool destaque)
    {
        var b = new Button { Text = t, Location = p, Size = s, FlatStyle = FlatStyle.Flat, BackColor = destaque ? Laranja : Painel, ForeColor = destaque ? Color.White : Texto, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = destaque ? Laranja : Color.FromArgb(190,190,195); b.FlatAppearance.BorderSize = 1; return b;
    }

    static string Detectar()
    {
        string here = AppDomain.CurrentDomain.BaseDirectory;
        var candidatos = new List<string> { here, Path.GetDirectoryName(here.TrimEnd('\\')) ?? "" };
        foreach (var raiz in new[] { @"C:\Program Files (x86)\Steam\steamapps\common", @"D:\SteamLibrary\steamapps\common", @"E:\SteamLibrary\steamapps\common", @"C:\Games" })
            try { if (Directory.Exists(raiz)) candidatos.AddRange(Directory.GetDirectories(raiz).Where(d => d.IndexOf("steins", StringComparison.OrdinalIgnoreCase) >= 0)); } catch { }
        foreach (var c in candidatos) if (c != "" && File.Exists(Path.Combine(c, "sgre_steam.exe"))) return c;
        return "";
    }

    void UI(Action a) { if (InvokeRequired) BeginInvoke(a); else a(); }
    void Status(string s, string d = null) { UI(() => { status.Text = s; if (d != null) detalhe.Text = d; }); }
    void Log(string s) { UI(() => log.AppendText(s + Environment.NewLine)); }
    void Barra(double f) { UI(() => barra.Value = Math.Max(0, Math.Min(1000, (int)(f * 1000)))); }

    void Iniciar()
    {
        string game = pasta.Text.Trim().Trim('"');
        if (!File.Exists(Path.Combine(game, "sgre_steam.exe"))) { MessageBox.Show(this, "Pasta inválida: sgre_steam.exe não encontrado.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        if (System.Diagnostics.Process.GetProcessesByName("sgre_steam").Length > 0) { MessageBox.Show(this, "Feche o jogo antes de instalar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        rodando = true; instalar.Enabled = desinstalar.Enabled = procurar.Enabled = pasta.Enabled = false; log.Clear();
        new Thread(() =>
        {
            try
            {
                string pacote = Instalar(game);
                Barra(1); Status("Tradução instalada e verificada!", "No jogo: AJUSTES → IDIOMA → Português.");
                Log("Concluído: todos os arquivos conferem (" + pacote + ").");
                UI(() => { if (AutoPasta != null) { Close(); return; } MessageBox.Show(this, "Tradução instalada com sucesso!\n\nNo jogo: AJUSTES → IDIOMA → Português.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); });
            }
            catch (Exception e)
            {
                Status("Erro na instalação.", e.Message); Log("ERRO: " + e.Message);
                Saida = 1; UI(() => { if (AutoPasta != null) { System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "erro_instalacao.txt"), e.Message); Close(); return; } MessageBox.Show(this, e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); PerguntarLimpeza(); });
            }
            finally { rodando = false; UI(() => { instalar.Enabled = desinstalar.Enabled = procurar.Enabled = pasta.Enabled = true; }); }
        }) { IsBackground = true }.Start();
    }

    string Sha(string path, long limit = -1, Action<long> prog = null)
    {
        using (var sha = SHA256.Create())
        using (var f = File.OpenRead(path))
        {
            var buf = new byte[1 << 22]; long left = limit < 0 ? long.MaxValue : limit; int n; long tot = 0;
            while (left > 0 && (n = f.Read(buf, 0, (int)Math.Min(buf.Length, left))) > 0) { sha.TransformBlock(buf, 0, n, null, 0); left -= n; tot += n; if (prog != null) prog(tot); }
            sha.TransformFinalBlock(buf, 0, 0);
            return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
        }
    }

    void Baixar(string url, string destino, long tamanho)
    {
        System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;
        string nome = Path.GetFileName(destino), tmp = destino + ".part"; long ja = File.Exists(tmp) ? new FileInfo(tmp).Length : 0;
        Log("Baixando " + nome + (tamanho > 0 ? " (" + (tamanho / 1048576) + " MB)" : "") + "...");
        var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
        req.UserAgent = "SGPTBR-Installer"; req.AllowAutoRedirect = true; req.Timeout = 60000; req.ReadWriteTimeout = 120000;
        if (ja > 0) req.AddRange(ja);
        using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
        {
            if (ja > 0 && resp.StatusCode != System.Net.HttpStatusCode.PartialContent) ja = 0;
            using (var s = resp.GetResponseStream())
            using (var o = new FileStream(tmp, ja > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write))
            {
                var buf = new byte[1 << 20]; int n; long tot = ja; var t0 = DateTime.Now; long base0 = ja; var ult = DateTime.MinValue;
                while ((n = s.Read(buf, 0, buf.Length)) > 0)
                {
                    o.Write(buf, 0, n); tot += n;
                    if (tamanho > 0 && (DateTime.Now - ult).TotalMilliseconds > 250)
                    {
                        ult = DateTime.Now; double seg = Math.Max(0.5, (DateTime.Now - t0).TotalSeconds), vel = (tot - base0) / seg;
                        Barra((double)tot / tamanho);
                        Status("Baixando " + nome + "...", (tot / 1048576) + " / " + (tamanho / 1048576) + " MB  •  " + (vel / 1048576).ToString("0.0") + " MB/s  •  restam ~" + TimeSpan.FromSeconds((tamanho - tot) / Math.Max(1, vel)).ToString(@"hh\:mm\:ss"));
                    }
                }
            }
        }
        Trocar(tmp, destino);
    }

    // Troca atômica: se falhar, o arquivo antigo continua no lugar (nunca fica sem o arquivo).
    static void Trocar(string tmp, string destino)
    {
        if (File.Exists(destino)) File.Replace(tmp, destino, null, true);
        else File.Move(tmp, destino);
    }

    // Impede que o índice grave fora da pasta do jogo (ex.: "..\").
    static string Dentro(string game, string dest)
    {
        string raiz = Path.GetFullPath(game).TrimEnd('\\') + "\\";
        string alvo = Path.GetFullPath(Path.Combine(raiz, dest.Replace('/', '\\')));
        if (!alvo.StartsWith(raiz, StringComparison.OrdinalIgnoreCase)) throw new Exception("Caminho inválido no índice do patch: " + dest);
        return alvo;
    }

    string Instalar(string game)
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory; bool baixado = false;
        if (!File.Exists(Path.Combine(dir, "patch_ptbr.json")))
        {
            dir = PastaTemp; Directory.CreateDirectory(dir); baixado = true; Log("Arquivos temporários em: " + dir + " (apagados ao final)");
            Status("Baixando índice do patch..."); Baixar(URL + "patch_ptbr.json", Path.Combine(dir, "patch_ptbr.json"), -1);
        }
        var js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        var idx = js.Deserialize<Dictionary<string, object>>(File.ReadAllText(Path.Combine(dir, "patch_ptbr.json")));
        var partes = ((System.Collections.ArrayList)idx["partes"]).Cast<Dictionary<string, object>>().ToList();
        var arqs = ((System.Collections.ArrayList)idx["arquivos"]).Cast<Dictionary<string, object>>().ToList();

        Status("Conferindo arquivos do jogo...", "Isso pode levar alguns minutos.");
        var fazer = new List<Dictionary<string, object>>(); int k = 0;
        foreach (var a in arqs)
        {
            k++; Barra((double)k / arqs.Count); string dest = (string)a["destino"];
            UI(() => detalhe.Text = dest);
            string alvo = Dentro(game, dest);
            if (File.Exists(alvo) && new FileInfo(alvo).Length == Convert.ToInt64(a["bytes_resultado"]) && Sha(alvo) == (string)a["sha256_resultado"]) continue;
            if ((string)a["tipo"] == "append")
            {
                // Aceita o arquivo original ou uma versão anterior da tradução (original + dados acrescentados):
                // confere só o trecho original do início.
                long tamOrig = Convert.ToInt64(a["bytes_original"]);
                if (!File.Exists(alvo) || new FileInfo(alvo).Length < tamOrig || Sha(alvo, tamOrig) != (string)a["sha256_original"])
                    throw new Exception("Versão do jogo incompatível ou arquivo modificado:\n" + dest + "\n\nNa Steam: Propriedades → Arquivos instalados → Verificar integridade, e tente de novo.");
            }
            // "full" e "novo": o arquivo é substituído por inteiro e conferido no final.
            fazer.Add(a);
        }
        // Backup para desinstalar: índice + originais dos arquivos "full", fora da pasta do jogo.
        string bk = PastaBackup(game); Directory.CreateDirectory(bk);
        File.WriteAllText(Path.Combine(bk, "patch_ptbr.json"), File.ReadAllText(Path.Combine(dir, "patch_ptbr.json")));
        foreach (var a in arqs.Where(x => (string)x["tipo"] == "full"))
        {
            string dest = (string)a["destino"], alvo = Dentro(game, dest), copia = Path.Combine(bk, dest.Replace('/', '\\'));
            if (File.Exists(copia) && Sha(copia) == (string)a["sha256_original"]) continue;
            if (File.Exists(alvo) && Sha(alvo) == (string)a["sha256_original"])
            {
                Status("Guardando original para desinstalar...", dest);
                Directory.CreateDirectory(Path.GetDirectoryName(copia)); File.Copy(alvo, copia + ".tmp", true); Trocar(copia + ".tmp", copia);
            }
        }
        if (fazer.Count == 0) { Log("A tradução já está instalada e íntegra."); return (string)idx["pacote"]; }
        Log(fazer.Count + " arquivo(s) para instalar.");

        var usadas = new HashSet<int>(fazer.SelectMany(a => ((System.Collections.ArrayList)a["blocos"]).Cast<System.Collections.ArrayList>().Select(b => Convert.ToInt32(b[0]))));
        foreach (int i in usadas.OrderBy(x => x))
        {
            var p = partes[i]; string nome = (string)p["nome"], pp = Path.Combine(dir, nome); long tam = Convert.ToInt64(p["bytes"]);
            if (!File.Exists(pp) || new FileInfo(pp).Length != tam) Baixar(URL + nome, pp, tam);
            Status("Conferindo " + nome + "...", "");
            if (Sha(pp, -1, t => Barra((double)t / tam)) != (string)p["sha256"]) { File.Delete(pp); throw new Exception("Download corrompido: " + nome + ". Execute o instalador de novo."); }
        }

        long total = fazer.Sum(a => ((System.Collections.ArrayList)a["blocos"]).Cast<System.Collections.ArrayList>().Sum(b => Convert.ToInt64(b[2]))), feito = 0;
        var abertas = new Dictionary<int, FileStream>();
        try
        {
            int i = 0;
            foreach (var a in fazer)
            {
                i++; string dest = (string)a["destino"], alvo = Dentro(game, dest);
                Status("Instalando (" + i + "/" + fazer.Count + ")...", dest); Log("Instalando " + dest);
                string tmp = alvo + ".ptbr_tmp";
                if ((string)a["tipo"] == "append")
                {
                    long tamOrig = Convert.ToInt64(a["bytes_original"]);
                    using (var src = File.OpenRead(alvo)) using (var dst = new FileStream(tmp, FileMode.Create, FileAccess.Write))
                    {
                        var b2 = new byte[1 << 22]; long left = tamOrig; int n2;
                        while (left > 0 && (n2 = src.Read(b2, 0, (int)Math.Min(b2.Length, left))) > 0) { dst.Write(b2, 0, n2); left -= n2; }
                    }
                }
                else { Directory.CreateDirectory(Path.GetDirectoryName(alvo)); if (File.Exists(tmp)) File.Delete(tmp); }
                using (var o = new FileStream(tmp, FileMode.Append, FileAccess.Write))
                {
                    var buf = new byte[1 << 22];
                    foreach (System.Collections.ArrayList b in (System.Collections.ArrayList)a["blocos"])
                    {
                        int part = Convert.ToInt32(b[0]); long off = Convert.ToInt64(b[1]), len = Convert.ToInt64(b[2]);
                        if (!abertas.ContainsKey(part)) abertas[part] = File.OpenRead(Path.Combine(dir, (string)partes[part]["nome"]));
                        var s = abertas[part]; s.Position = off;
                        while (len > 0) { int n = s.Read(buf, 0, (int)Math.Min(buf.Length, len)); if (n <= 0) throw new Exception("Patch truncado."); o.Write(buf, 0, n); len -= n; feito += n; Barra((double)feito / Math.Max(1, total)); }
                    }
                }
                if (Sha(tmp) != (string)a["sha256_resultado"]) { File.Delete(tmp); throw new Exception("Falha de verificação ao gerar " + dest); }
                Trocar(tmp, alvo);
            }
        }
        finally { foreach (var s in abertas.Values) s.Dispose(); }
        if (baixado) { try { Directory.Delete(dir, true); } catch { } }
        return (string)idx["pacote"];
    }

    // Backup fica em %LOCALAPPDATA%, uma pasta por instalação do jogo (nunca dentro da pasta do jogo).
    static string PastaBackup(string game)
    {
        string chave;
        using (var s = SHA256.Create()) chave = BitConverter.ToString(s.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(game).TrimEnd('\\').ToLowerInvariant()))).Replace("-", "").Substring(0, 12);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteinsGateRebootPTBR", "backup", chave);
    }

    // Volta o jogo ao original: corta os "append", apaga os "novo" e devolve os "full" do backup. Confere tudo antes de mexer.
    void Desinstalar(string game)
    {
        string bk = PastaBackup(game), ij = Path.Combine(bk, "patch_ptbr.json");
        if (!File.Exists(ij)) throw new Exception("Não há backup desta instalação (ela foi feita com uma versão antiga do instalador).\n\nPara voltar ao original, use na Steam: Propriedades → Arquivos instalados → Verificar integridade.");
        var js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        var arqs = ((System.Collections.ArrayList)js.Deserialize<Dictionary<string, object>>(File.ReadAllText(ij))["arquivos"]).Cast<Dictionary<string, object>>().ToList();
        Status("Conferindo arquivos...", "Nada é alterado antes de tudo conferir.");
        var faltam = new List<string>(); int k = 0;
        foreach (var a in arqs)
        {
            k++; Barra((double)k / arqs.Count); string dest = (string)a["destino"], alvo = Dentro(game, dest), tipo = (string)a["tipo"];
            if (tipo == "append")
            {
                long tam = Convert.ToInt64(a["bytes_original"]);
                if (!File.Exists(alvo) || new FileInfo(alvo).Length < tam || Sha(alvo, tam) != (string)a["sha256_original"]) faltam.Add(dest);
            }
            else if (tipo == "full")
            {
                string copia = Path.Combine(bk, dest.Replace('/', '\\'));
                bool ok = File.Exists(alvo) && Sha(alvo) == (string)a["sha256_original"];
                if (!ok && !(File.Exists(copia) && Sha(copia) == (string)a["sha256_original"])) faltam.Add(dest);
            }
        }
        if (faltam.Count > 0) throw new Exception("Não dá para restaurar com segurança: " + faltam.Count + " arquivo(s) sem original disponível (ex.: " + faltam[0] + ").\nNada foi alterado.\n\nUse na Steam: Propriedades → Arquivos instalados → Verificar integridade.");
        k = 0;
        foreach (var a in arqs)
        {
            k++; Barra((double)k / arqs.Count); string dest = (string)a["destino"], alvo = Dentro(game, dest), tipo = (string)a["tipo"];
            Status("Restaurando (" + k + "/" + arqs.Count + ")...", dest);
            if (tipo == "append")
            {
                long tam = Convert.ToInt64(a["bytes_original"]);
                if (new FileInfo(alvo).Length > tam) using (var f = new FileStream(alvo, FileMode.Open, FileAccess.Write)) f.SetLength(tam);
            }
            else if (tipo == "novo") { if (File.Exists(alvo)) File.Delete(alvo); }
            else if (tipo == "full" && Sha(alvo) != (string)a["sha256_original"])
            {
                string copia = Path.Combine(bk, dest.Replace('/', '\\'));
                File.Copy(copia, alvo + ".ptbr_tmp", true); Trocar(alvo + ".ptbr_tmp", alvo);
            }
        }
        try { Directory.Delete(bk, true); } catch { }
    }

    void IniciarDesinstalar()
    {
        string game = pasta.Text.Trim().Trim('"');
        if (!File.Exists(Path.Combine(game, "sgre_steam.exe"))) { MessageBox.Show(this, "Pasta inválida: sgre_steam.exe não encontrado.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        if (System.Diagnostics.Process.GetProcessesByName("sgre_steam").Length > 0) { MessageBox.Show(this, "Feche o jogo antes de desinstalar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!Auto && MessageBox.Show(this, "Remover a tradução e voltar o jogo ao original?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        rodando = true; instalar.Enabled = desinstalar.Enabled = procurar.Enabled = pasta.Enabled = false;
        new Thread(() =>
        {
            try
            {
                Desinstalar(game); Barra(1); Status("Tradução removida.", "O jogo voltou aos arquivos originais.");
                UI(() => { if (Auto) { Close(); return; } MessageBox.Show(this, "Tradução removida. O jogo voltou aos arquivos originais.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); });
            }
            catch (Exception e)
            {
                Status("Erro ao desinstalar.", e.Message); Saida = 1;
                UI(() => { if (Auto) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "erro_instalacao.txt"), e.Message); Close(); return; } MessageBox.Show(this, e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); });
            }
            finally { rodando = false; UI(() => { instalar.Enabled = desinstalar.Enabled = procurar.Enabled = pasta.Enabled = true; }); }
        }) { IsBackground = true }.Start();
    }

    static bool Auto { get { return AutoPasta != null; } }
    public static string AutoPasta; public static bool AutoDesinstalar; public static int Saida = 0;
    [STAThread]
    static int Main(string[] args)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if (args.Length > 1 && args[0] == "/desinstalar") { AutoDesinstalar = true; AutoPasta = args[1]; }
        else if (args.Length > 0) AutoPasta = args[0];
        Application.Run(new Janela());
        return Saida;
    }
}
