"""Gera o patch por diferença da tradução PT-BR (formato SGPT1).

Para cada arquivo do pacote:
  - append : o arquivo traduzido = original + sufixo  -> guarda só o sufixo
  - full   : substitui um arquivo existente            -> guarda o arquivo inteiro
  - novo   : arquivo que não existe no jogo (*_sc.webm) -> guarda o arquivo inteiro
Container: patch_ptbr.pak.NNN (partes de até 1,9 GB) = blobs concatenados; o índice vai
em patch_ptbr.json (sha256 do original exigido e sha256 do resultado).
Uso: python gerar_patch.py PACOTE ORIGINAL SAIDA
"""
import json, os, sys, hashlib

PART = 1_900_000_000

def sha(p, limit=None):
    h = hashlib.sha256(); n = 0
    with open(p, 'rb') as f:
        while True:
            want = 1 << 24 if limit is None else min(1 << 24, limit - n)
            if want <= 0: break
            c = f.read(want)
            if not c: break
            h.update(c); n += len(c)
    return h.hexdigest()

def main(pkg, orig, out):
    os.makedirs(out, exist_ok=False)
    man = json.load(open(os.path.join(pkg, 'Manifesto_Pacote.json'), encoding='utf-8-sig'))
    files = man.get('files') or man.get('arquivos')
    entries = []; part = 0; pos = 0
    fo = open(os.path.join(out, f'patch_ptbr.pak.{part:03d}'), 'wb')
    def write_blob(path, start):
        nonlocal fo, part, pos
        chunks = []
        with open(path, 'rb') as f:
            f.seek(start)
            while True:
                c = f.read(1 << 24)
                if not c: break
                while c:
                    room = PART - pos
                    if room == 0:
                        fo.close(); part += 1; pos = 0
                        fo = open(os.path.join(out, f'patch_ptbr.pak.{part:03d}'), 'wb'); room = PART
                    w = c[:room]; fo.write(w)
                    if chunks and chunks[-1][0] == part and chunks[-1][1] + chunks[-1][2] == pos: chunks[-1][2] += len(w)
                    else: chunks.append([part, pos, len(w)])
                    pos += len(w); c = c[room:]
        return chunks
    for f in files:
        dest = f['destino']; new = os.path.join(pkg, 'arquivos', dest); org = os.path.join(orig, dest)
        ns = os.path.getsize(new); e = {'destino': dest, 'sha256_resultado': f['sha256'].lower(), 'bytes_resultado': ns}
        if os.path.exists(org):
            os_ = os.path.getsize(org); e['sha256_original'] = sha(org); e['bytes_original'] = os_
            if ns > os_ and sha(new, os_) == e['sha256_original']:
                e['tipo'] = 'append'; e['blocos'] = write_blob(new, os_)
            else:
                e['tipo'] = 'full'; e['blocos'] = write_blob(new, 0)
        else:
            e['tipo'] = 'novo'; e['blocos'] = write_blob(new, 0)
        entries.append(e); print(e['tipo'], dest, flush=True)
    fo.close()
    parts = [f'patch_ptbr.pak.{i:03d}' for i in range(part + 1)]
    idx = {'formato': 'SGPT1', 'pacote': man.get('pacote'), 'partes': [{'nome': p, 'bytes': os.path.getsize(os.path.join(out, p)), 'sha256': sha(os.path.join(out, p))} for p in parts],
           'arquivos': entries}
    json.dump(idx, open(os.path.join(out, 'patch_ptbr.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print(json.dumps({'pacote': idx['pacote'], 'partes': [(p['nome'], p['bytes']) for p in idx['partes']]}))

if __name__ == '__main__':
    main(*sys.argv[1:4])
