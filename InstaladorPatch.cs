// Instalador do patch PT-BR de STEINS;GATE RE:BOOT (formato SGPT1).
// Lê patch_ptbr.json + patch_ptbr.pak.NNN da pasta do .exe e aplica sobre a instalação do jogo
// escolhida pelo usuário. Confere SHA-256 do original antes e do resultado depois.
// Compilar: csc /target:exe /out:Instalar_Traducao_PTBR.exe /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll InstaladorPatch.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows.Forms;

static class Instalador
{
    static string Sha(string path, long limit = -1)
    {
        using (var sha = SHA256.Create())
        using (var f = File.OpenRead(path))
        {
            var buf = new byte[1 << 22]; long left = limit < 0 ? long.MaxValue : limit; int n;
            while (left > 0 && (n = f.Read(buf, 0, (int)Math.Min(buf.Length, left))) > 0) { sha.TransformBlock(buf, 0, n, null, 0); left -= n; }
            sha.TransformFinalBlock(buf, 0, 0);
            return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
        }
    }

    const string URL = "https://github.com/VenonSabio/Steins-Gate-Reboot-Patch-PT-BR/releases/download/v1.0/";

    static void Baixar(string url, string destino, long tamanho)
    {
        System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072; // TLS 1.2
        Console.WriteLine("Baixando " + Path.GetFileName(destino) + (tamanho > 0 ? " (" + (tamanho / 1048576) + " MB)" : "") + "...");
        string tmp = destino + ".part"; long ja = File.Exists(tmp) ? new FileInfo(tmp).Length : 0;
        var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
        req.UserAgent = "SGPTBR-Installer"; req.AllowAutoRedirect = true; req.Timeout = 60000; req.ReadWriteTimeout = 120000;
        if (ja > 0) req.AddRange(ja);
        using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
        {
            if (ja > 0 && resp.StatusCode != System.Net.HttpStatusCode.PartialContent) ja = 0;
            using (var s = resp.GetResponseStream())
            using (var o = new FileStream(tmp, ja > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write))
            {
                var buf = new byte[1 << 20]; int n; long tot = ja; int ultimo = -1;
                while ((n = s.Read(buf, 0, buf.Length)) > 0)
                {
                    o.Write(buf, 0, n); tot += n;
                    if (tamanho > 0) { int pct = (int)(tot * 100 / tamanho); if (pct != ultimo) { Console.Write("\r  " + pct + "%   "); ultimo = pct; } }
                }
                Console.WriteLine();
            }
        }
        if (File.Exists(destino)) File.Delete(destino);
        File.Move(tmp, destino);
    }

    static string PedirPasta()
    {
        string here = AppDomain.CurrentDomain.BaseDirectory;
        foreach (var c in new[] { here, Path.GetDirectoryName(here.TrimEnd('\\')) ?? "" })
            if (c != "" && File.Exists(Path.Combine(c, "sgre_steam.exe"))) return c;
        Console.WriteLine("Selecione a pasta do jogo (a que contém sgre_steam.exe)...");
        string escolhida = null;
        var t = new System.Threading.Thread(() =>
        {
            using (var d = new FolderBrowserDialog { Description = "Pasta do STEINS;GATE RE:BOOT (contém sgre_steam.exe)" })
                if (d.ShowDialog() == DialogResult.OK) escolhida = d.SelectedPath;
        });
        t.SetApartmentState(System.Threading.ApartmentState.STA); t.Start(); t.Join();
        if (escolhida == null) { Console.Write("Ou digite o caminho: "); escolhida = Console.ReadLine(); }
        return escolhida == null ? null : escolhida.Trim('"', ' ');
    }

    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("STEINS;GATE RE:BOOT — Tradução PT-BR (patch)\n");
        try
        {
            string game = args.Length > 0 ? args[0] : PedirPasta();
            if (string.IsNullOrEmpty(game) || !File.Exists(Path.Combine(game, "sgre_steam.exe"))) throw new Exception("Pasta inválida: sgre_steam.exe não encontrado.");
            if (System.Diagnostics.Process.GetProcessesByName("sgre_steam").Length > 0) throw new Exception("Feche o jogo antes de instalar.");

            // Dados do patch: usa os arquivos ao lado do .exe; se faltarem, baixa da Release do GitHub.
            string dir = AppDomain.CurrentDomain.BaseDirectory; bool baixado = false;
            if (!File.Exists(Path.Combine(dir, "patch_ptbr.json")))
            {
                dir = Path.Combine(Path.GetTempPath(), "SteinsGateReboot_PTBR_patch"); Directory.CreateDirectory(dir); baixado = true;
                Baixar(URL + "patch_ptbr.json", Path.Combine(dir, "patch_ptbr.json"), -1);
            }
            var js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var idx = js.Deserialize<Dictionary<string, object>>(File.ReadAllText(Path.Combine(dir, "patch_ptbr.json")));
            var partes = ((System.Collections.ArrayList)idx["partes"]).Cast<Dictionary<string, object>>().ToList();
            foreach (var p in partes)
            {
                string nome = (string)p["nome"], pp = Path.Combine(dir, nome); long tam = Convert.ToInt64(p["bytes"]);
                if (!File.Exists(pp) || new FileInfo(pp).Length != tam)
                {
                    if (!baixado && !File.Exists(Path.Combine(dir, "patch_ptbr.json"))) throw new Exception("Parte do patch ausente: " + nome);
                    Baixar(URL + nome, pp, tam);
                }
                Console.WriteLine("Conferindo " + nome + "...");
                if (Sha(pp) != (string)p["sha256"]) { File.Delete(pp); throw new Exception("Download corrompido: " + nome + ". Execute o instalador de novo."); }
            }
            var arqs = ((System.Collections.ArrayList)idx["arquivos"]).Cast<Dictionary<string, object>>().ToList();

            Console.WriteLine("Conferindo arquivos do jogo (pode levar alguns minutos)...");
            var fazer = new List<Dictionary<string, object>>();
            foreach (var a in arqs)
            {
                string alvo = Path.Combine(game, ((string)a["destino"]).Replace('/', '\\'));
                if (File.Exists(alvo) && new FileInfo(alvo).Length == Convert.ToInt64(a["bytes_resultado"]) && Sha(alvo) == (string)a["sha256_resultado"]) continue; // já aplicado
                string tipo = (string)a["tipo"];
                if (tipo != "novo")
                {
                    if (!File.Exists(alvo)) throw new Exception("Arquivo do jogo ausente: " + a["destino"]);
                    if (Sha(alvo) != (string)a["sha256_original"]) throw new Exception("Versão do jogo incompatível ou arquivo modificado: " + a["destino"] + "\nUse a versão Steam original (verifique a integridade dos arquivos na Steam).");
                }
                fazer.Add(a);
            }
            if (fazer.Count == 0) { Console.WriteLine("A tradução já está instalada e íntegra."); Pausa(); return 0; }
            Console.WriteLine("Aplicando " + fazer.Count + " arquivo(s)...");
            var abertas = new Dictionary<int, FileStream>();
            try
            {
                int i = 0;
                foreach (var a in fazer)
                {
                    i++; string dest = (string)a["destino"]; string alvo = Path.Combine(game, dest.Replace('/', '\\'));
                    Console.WriteLine("  [" + i + "/" + fazer.Count + "] " + dest);
                    string tmp = alvo + ".ptbr_tmp";
                    if ((string)a["tipo"] == "append") File.Copy(alvo, tmp, true);
                    else { Directory.CreateDirectory(Path.GetDirectoryName(alvo)); if (File.Exists(tmp)) File.Delete(tmp); }
                    using (var o = new FileStream(tmp, FileMode.Append, FileAccess.Write))
                    {
                        var buf = new byte[1 << 22];
                        foreach (System.Collections.ArrayList b in (System.Collections.ArrayList)a["blocos"])
                        {
                            int part = Convert.ToInt32(b[0]); long off = Convert.ToInt64(b[1]); long len = Convert.ToInt64(b[2]);
                            if (!abertas.ContainsKey(part)) abertas[part] = File.OpenRead(Path.Combine(dir, (string)partes[part]["nome"]));
                            var s = abertas[part]; s.Position = off;
                            while (len > 0) { int n = s.Read(buf, 0, (int)Math.Min(buf.Length, len)); if (n <= 0) throw new Exception("Patch truncado."); o.Write(buf, 0, n); len -= n; }
                        }
                    }
                    if (Sha(tmp) != (string)a["sha256_resultado"]) { File.Delete(tmp); throw new Exception("Falha de verificação ao gerar " + dest); }
                    if (File.Exists(alvo)) File.Delete(alvo);
                    File.Move(tmp, alvo);
                }
            }
            finally { foreach (var s in abertas.Values) s.Dispose(); }
            if (baixado) { try { Directory.Delete(dir, true); } catch { } }
            Console.WriteLine("\nTradução instalada e verificada (SHA-256 idêntico ao pacote " + idx["pacote"] + ").");
            Console.WriteLine("No jogo: AJUSTES > IDIOMA > Português.");
            Pausa(); return 0;
        }
        catch (Exception e) { Console.WriteLine("\nERRO: " + e.Message); Pausa(); return 1; }
    }
    static void Pausa() { if (!Console.IsInputRedirected) { Console.WriteLine("\nPressione Enter para sair."); Console.ReadLine(); } }
}
